#!/usr/bin/env python3
"""Muse Bridge — điều phối Claude Code ↔ Muse ↔ Unity từ một web app local.

Chạy:  python3 server.py   →  mở http://127.0.0.1:8765
Chỉ dùng thư viện chuẩn. Gọi muse-cli / git / claude bằng subprocess (không qua shell).
"""
import base64
import json
import os
import re
import shutil
import subprocess
import sys
import threading
import time
from datetime import datetime
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

HERE = os.path.dirname(os.path.abspath(__file__))
CONFIG_PATH = os.path.join(HERE, "config.json")

DEFAULT_CONFIG = {
    "port": 8765,
    "muse_cli": "muse-cli",
    "thread_id": "",
    "repo_path": "",
    "agent_dir": "",
    "poll_seconds": 30,
    "send_wait_seconds": 20,
    "auto_mode": False,
    "max_fix_attempts": 3,
    "claude_bin": "claude",
    "claude_allowed_tools": ["Read", "Grep", "Glob", "Bash(git:*)", "mcp__UnityMCP"],
    # Plan chạy trong terminal của app: tool được tự cho phép (không hỏi) + chế độ quyền
    "plan_allowed_tools": ["Read", "Grep", "Glob", "Edit", "Write", "MultiEdit", "TodoWrite",
                           "Bash(muse-ask:*)", "Bash(git status:*)", "Bash(git diff:*)", "Bash(git log:*)",
                           "Bash(ls:*)", "Bash(cat:*)", "Bash(grep:*)", "Bash(find:*)",
                           "mcp__unityMCP", "mcp__UnityMCP"],
    "plan_permission_mode": "acceptEdits",
    "plan_autonomous": True,
    "idle_alert_seconds": 8,
    "claude_timeout_seconds": 900,
    "claude_workdir": "~/wkspaces/WB",
    "trash_days": 3,
    "thread_per_plan": True,
    "task_footer": (
        "---\nProject: {project}\nSửa/tạo file TRỰC TIẾP trong project trên (không dán code vào reply).\n"
        "Kiến thức: /Users/mobione/AGENTS/AI/Unity/knowledge/ (INDEX.md trước).\n"
        "Xong reply ĐÚNG format dòng đầu: [DONE-{id}] <tóm tắt> | files: <đường dẫn>\n"
        "Nếu vướng: [BLOCKED-{id}] <lý do>"
    ),
    "review_prompt": (
        "Bạn là QA cho Unity project ở thư mục hiện tại.\n"
        "Task {id}: {body}\n\n"
        "Coding agent (Muse) báo xong: {summary}\nCommit: {sha}\n\n"
        "Việc cần làm:\n"
        "1. Xem thay đổi: git show --stat {sha} rồi git show {sha}.\n"
        "2. Dùng Unity MCP: refresh_unity, đợi compile xong, read_console lấy lỗi/cảnh báo.\n"
        "3. Nếu project có test liên quan thì run_tests (EditMode).\n"
        "4. Đối chiếu với yêu cầu task.\n"
        "Không sửa code. Kết thúc bằng đúng 1 dòng: VERDICT: PASS hoặc VERDICT: FAIL.\n"
        "Nếu FAIL, thêm sau đó một mục bắt đầu bằng 'BUG:' mô tả lỗi + log ngắn gọn để gửi cho coding agent."
    ),
}

LOCK = threading.RLock()
CFG = {}
STATE = {"tasks": {}, "next_num": 1, "last_seq": 0}
RUNTIME = {"messages": [], "poll_ok": None, "poll_error": "", "poll_at": 0,
           "status": None, "status_error": "", "jobs": {}}
WAKE = threading.Event()

TAG_RE = re.compile(r"(?m)^\s*\[(DONE|BLOCKED)-([A-Za-z0-9_.-]+)\]\s*(.*)", re.S)
SHA_RE = re.compile(r"commit\s+([0-9a-f]{7,40})", re.I)
ACTIVE = ("sent", "bug_sent")


# ---------------------------------------------------------------- utils
def now():
    return datetime.now().isoformat(timespec="seconds")


def load_config():
    cfg = dict(DEFAULT_CONFIG)
    if os.path.exists(CONFIG_PATH):
        with open(CONFIG_PATH, encoding="utf-8") as fh:
            cfg.update(json.load(fh))
    CFG.clear()
    CFG.update(cfg)


def save_config():
    with open(CONFIG_PATH, "w", encoding="utf-8") as fh:
        json.dump(CFG, fh, indent=2, ensure_ascii=False)


def default_project():
    return os.path.abspath(os.path.expanduser(CFG.get("claude_workdir") or "~"))


def agent_dir():
    if CFG.get("agent_dir"):
        return os.path.expanduser(CFG["agent_dir"])
    if CFG.get("repo_path"):
        return os.path.join(os.path.expanduser(CFG["repo_path"]), ".agent")
    return os.path.join(HERE, ".agent")


def repo():
    return os.path.expanduser(CFG.get("repo_path") or "")


def write_file(rel, text):
    path = os.path.join(agent_dir(), rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(text)


def save_state():
    os.makedirs(agent_dir(), exist_ok=True)
    tmp = os.path.join(agent_dir(), "state.json.tmp")
    with open(tmp, "w", encoding="utf-8") as fh:
        json.dump(STATE, fh, indent=2, ensure_ascii=False)
    os.replace(tmp, os.path.join(agent_dir(), "state.json"))


def load_state():
    path = os.path.join(agent_dir(), "state.json")
    STATE.clear()
    STATE.update({"tasks": {}, "next_num": 1, "last_seq": 0})
    if os.path.exists(path):
        with open(path, encoding="utf-8") as fh:
            STATE.update(json.load(fh))


def log_event(task_id, kind, text, **extra):
    ev = {"at": now(), "task": task_id, "kind": kind, "text": text}
    ev.update(extra)
    os.makedirs(agent_dir(), exist_ok=True)
    with open(os.path.join(agent_dir(), "events.jsonl"), "a", encoding="utf-8") as fh:
        fh.write(json.dumps(ev, ensure_ascii=False) + "\n")
    t = STATE["tasks"].get(task_id)
    if t is not None:
        t.setdefault("timeline", []).append(ev)
        t["updated"] = ev["at"]


def run(argv, cwd=None, timeout=120):
    try:
        r = subprocess.run(argv, cwd=cwd or None, capture_output=True, text=True, timeout=timeout)
        return r.returncode, r.stdout, r.stderr
    except FileNotFoundError:
        return 127, "", "không tìm thấy lệnh: " + argv[0]
    except subprocess.TimeoutExpired:
        return 124, "", "timeout sau %ss" % timeout


def muse(args, timeout=120):
    env_argv = [os.path.expanduser(CFG["muse_cli"])] + args
    code, out, err = run(env_argv, timeout=timeout)
    if code != 0:
        raise RuntimeError((err or out or "exit %s" % code).strip()[:800])
    try:
        return json.loads(out) if out.strip() else {}
    except json.JSONDecodeError:
        return {"raw": out}


def find_key(obj, key):
    if isinstance(obj, dict):
        if obj.get(key):
            return obj[key]
        for v in obj.values():
            hit = find_key(v, key)
            if hit:
                return hit
    elif isinstance(obj, list):
        for v in obj:
            hit = find_key(v, key)
            if hit:
                return hit
    return None


# ---------------------------------------------------------------- muse ops
def muse_send(text):
    if not CFG.get("thread_id"):
        raise RuntimeError("chưa có thread_id — tạo side chat ở mục Cài đặt")
    # --wait 0 đóng kết nối ngay sau khi gửi → tin có thể chưa tới Muse. Giữ kết nối vài giây.
    wait = max(5, int(CFG.get("send_wait_seconds", 20)))
    head = text.split("\n", 1)[0][:120]
    try:
        res = muse(["send", text, "--thread", CFG["thread_id"], "--wait", str(wait)], timeout=wait + 60)
    except Exception as e:  # noqa: BLE001
        with LOCK:
            log_event("-", "send_error", "%s → %s" % (head, e))
        raise
    with LOCK:
        log_event("-", "send_ok", head, reply=bool(isinstance(res, dict) and res.get("reply")))
    WAKE.set()
    return res


def poll_once():
    if not CFG.get("thread_id"):
        return
    try:
        msgs = muse(["history", "--thread", CFG["thread_id"], "--limit", "30"], timeout=90)
        if not isinstance(msgs, list):
            msgs = []
        msgs.sort(key=lambda m: m.get("seq") or 0)
        with LOCK:
            RUNTIME["messages"] = msgs
            RUNTIME["poll_ok"], RUNTIME["poll_error"], RUNTIME["poll_at"] = True, "", time.time()
            for m in msgs:
                seq = m.get("seq") or 0
                if seq <= STATE.get("last_seq", 0):
                    continue
                STATE["last_seq"] = seq
                if "user" in str(m.get("role") or "").lower():
                    continue
                handle_reply(m)
            relay_inbox(msgs)
            save_state()
    except Exception as e:  # noqa: BLE001
        with LOCK:
            RUNTIME["poll_ok"], RUNTIME["poll_error"], RUNTIME["poll_at"] = False, str(e), time.time()


def handle_reply(m):
    text = m.get("text") or ""
    for kind, tid, rest in TAG_RE.findall(text):
        key = tid if tid.upper().startswith("TASK-") else "TASK-" + tid
        t = STATE["tasks"].get(key)
        if t is None:
            continue
        rest = rest.strip()
        if kind == "DONE":
            sha = SHA_RE.search(rest)
            t["status"] = "done"
            t["summary"] = rest.split("\n")[0][:500]
            t["sha"] = sha.group(1) if sha else ""
            log_event(key, "done", rest[:2000], sha=t["sha"])
            n = len([e for e in t["timeline"] if e["kind"] == "done"])
            write_file("results/%s-%d.md" % (key, n), "# %s — DONE #%d\n\n%s\n\n%s\n" % (key, n, now(), rest))
            if CFG.get("auto_mode"):
                start_job(key, "pull_review")
        else:
            t["status"] = "blocked"
            t["blocked_reason"] = rest[:2000]
            log_event(key, "blocked", rest[:2000])
            write_file("results/%s.md" % key, "# %s — BLOCKED\n\n%s\n\n%s\n" % (key, now(), rest))


# ---------------------------------------------------------------- relay (Cowork ↔ Muse qua file)
RELAY_TAG = re.compile(r"\[(DONE|BLOCKED|ASK-CLAUDE)-([A-Za-z0-9_.-]+)\]")
RELAY_FOOTER = (
    "---\n(Tin từ Claude — đồng nghiệp điều phối, không phải Linh.)\n"
    "Project: {project} — sửa/tạo file TRỰC TIẾP trong đó. Kiến thức: /Users/mobione/AGENTS/AI/Unity/knowledge/.\n"
    "Reply dòng đầu ĐÚNG 1 trong các format:\n"
    "[DONE-{id}] <tóm tắt> | files: <đường dẫn>\n"
    "[BLOCKED-{id}] <lý do / cần gì>\n"
    "[ASK-CLAUDE-{id}] <việc cần Claude làm trong Unity trước>"
)
PREFIX = {"task": "TASK", "bug": "BUG", "reply": "CLAUDE"}


def relay_dirs():
    base = agent_dir()
    out, inbox = os.path.join(base, "outbox"), os.path.join(base, "inbox")
    for d in (out, os.path.join(out, "sent"), os.path.join(out, "failed"), inbox):
        os.makedirs(d, exist_ok=True)
    return out, inbox


def relay_outbox():
    out, _ = relay_dirs()
    for name in sorted(os.listdir(out)):
        if not name.endswith(".json"):
            continue
        path = os.path.join(out, name)
        try:
            with open(path, encoding="utf-8") as fh:
                job = json.load(fh)
            rid, kind, text = job["id"], job.get("kind", "task"), job["text"].strip()
            if kind == "raw":
                msg = text
            else:
                msg = "[%s-%s] %s\n\n%s" % (PREFIX.get(kind, "TASK"), rid, text, RELAY_FOOTER.replace("{id}", rid).replace("{project}", default_project()))
            muse_send(msg)
            RUNTIME["relay_at"] = time.time()
            with LOCK:
                STATE.setdefault("relay_ids", [])
                if rid not in STATE["relay_ids"]:
                    STATE["relay_ids"].append(rid)
                log_event(rid, "claude_" + kind, text, by="claude", via="cowork")
                save_state()
            os.replace(path, os.path.join(out, "sent", "%s-%d.json" % (name[:-5], int(time.time()))))
            WAKE.set()
        except Exception as e:  # noqa: BLE001
            with LOCK:
                log_event("-", "error", "relay %s: %s" % (name, e))
                save_state()
            os.replace(path, os.path.join(out, "failed", name))


def relay_inbox(msgs):
    _, inbox = relay_dirs()
    with open(os.path.join(agent_dir(), "thread.json"), "w", encoding="utf-8") as fh:
        json.dump(msgs[-30:], fh, ensure_ascii=False, indent=1)
    ids = set(STATE.get("relay_ids", []))
    for m in msgs:
        if "user" in str(m.get("role") or "").lower():
            continue
        text = m.get("text") or ""
        for kind, rid in RELAY_TAG.findall(text):
            if rid not in ids:
                continue
            seq = m.get("seq") or 0
            path = os.path.join(inbox, "%s-%s.json" % (rid, seq))
            if os.path.exists(path):
                continue
            status = {"DONE": "DONE", "BLOCKED": "BLOCKED", "ASK-CLAUDE": "ASK_CLAUDE"}[kind]
            sha = SHA_RE.search(text)
            with open(path, "w", encoding="utf-8") as fh:
                json.dump({"id": rid, "status": status, "seq": seq, "commit": sha.group(1) if sha else "",
                           "text": text, "at": now()}, fh, ensure_ascii=False, indent=1)
            log_event(rid, "muse_" + status.lower(), text, seq=seq)
            break


def relay_loop():
    while True:
        try:
            relay_outbox()
        except Exception:  # noqa: BLE001
            pass
        time.sleep(3)


def poller():
    while True:
        need = bool(CFG.get("thread_id"))
        if need:
            poll_once()
        with LOCK:
            active = any(t.get("status") in ACTIVE for t in STATE["tasks"].values())
            recent = time.time() - RUNTIME.get("relay_at", 0) < 3600
        if recent:
            wait = 10
        else:
            wait = CFG.get("poll_seconds", 30) if active else max(120, CFG.get("poll_seconds", 30))
        WAKE.wait(wait)
        WAKE.clear()


def bg(fn, *args, task_id="-"):
    def work():
        try:
            fn(*args)
        except Exception as e:  # noqa: BLE001
            with LOCK:
                t = STATE["tasks"].get(task_id)
                if t is not None and t.get("status") == "sending":
                    t["status"] = "send_error"
                log_event(task_id, "error", str(e))
                save_state()
    threading.Thread(target=work, daemon=True).start()


# ---------------------------------------------------------------- tasks
def create_task(title, body, send=True):
    with LOCK:
        num = STATE["next_num"]
        STATE["next_num"] = num + 1
        tid = "TASK-%03d" % num
        STATE["tasks"][tid] = {"id": tid, "title": title.strip() or body.strip()[:60],
                               "body": body.strip(), "status": "draft", "attempts": 0,
                               "created": now(), "updated": now(), "timeline": []}
        write_file("tasks/%s.md" % tid, "# %s — %s\n\n%s\n\n%s\n" % (tid, title, now(), body))
        log_event(tid, "created", title or body[:80])
        save_state()
    if send:
        with LOCK:
            STATE["tasks"][tid]["status"] = "sending"
            save_state()
        bg(send_task, tid, task_id=tid)
    return tid


def send_task(tid):
    with LOCK:
        t = STATE["tasks"][tid]
        t["status"] = "sending"
        short = tid.split("-", 1)[1]
        footer = CFG.get("task_footer", "").replace("{id}", short).replace("{project}", default_project())
        msg = "[%s] %s\n\n%s" % (tid, t["body"], footer)
    muse_send(msg)
    with LOCK:
        t["status"] = "sent"
        log_event(tid, "sent", "Đã gửi task cho Muse")
        save_state()
    WAKE.set()


def send_bug(tid, text):
    with LOCK:
        t = STATE["tasks"][tid]
        t["status"] = "sending"
        t["attempts"] = t.get("attempts", 0) + 1
        short = tid.split("-", 1)[1]
        msg = ("[BUG-%s] (lần fix %d)\n%s\n\n---\nSửa xong reply: [DONE-%s] <tóm tắt> | commit <sha>"
               % (short, t["attempts"], text.strip(), short))
    muse_send(msg)
    with LOCK:
        t["status"] = "bug_sent"
        write_file("bugs/BUG-%s-%d.md" % (short, t["attempts"]), "# BUG-%s (lần %d)\n\n%s\n\n%s\n"
                   % (short, t["attempts"], now(), text))
        log_event(tid, "bug_sent", text.strip()[:2000], attempt=t["attempts"])
        save_state()
    WAKE.set()


def git_pull():
    if not repo():
        raise RuntimeError("chưa cấu hình repo_path")
    code, out, err = run(["git", "-C", repo(), "pull", "--ff-only"], timeout=120)
    if code != 0:
        raise RuntimeError((err or out).strip()[:1500])
    return out.strip()


def claude_review(tid):
    with LOCK:
        t = dict(STATE["tasks"][tid])
        t["status_before"] = t["status"]
        STATE["tasks"][tid]["status"] = "reviewing"
        save_state()
    prompt = (CFG["review_prompt"].replace("{id}", tid).replace("{body}", t.get("body", ""))
              .replace("{summary}", t.get("summary", "")).replace("{sha}", t.get("sha") or "HEAD"))
    argv = [os.path.expanduser(CFG["claude_bin"]), "-p", prompt, "--output-format", "json"]
    tools = CFG.get("claude_allowed_tools") or []
    if tools:
        argv += ["--allowedTools"] + list(tools)
    code, out, err = run(argv, cwd=repo() or None, timeout=int(CFG.get("claude_timeout_seconds", 900)))
    result = out
    try:
        result = json.loads(out).get("result", out)
    except (json.JSONDecodeError, AttributeError):
        pass
    if code != 0 and not result.strip():
        result = "Lỗi chạy claude (exit %s): %s" % (code, err.strip()[:1500])
    verdict_m = re.findall(r"VERDICT:\s*(PASS|FAIL)", result or "", re.I)
    verdict = verdict_m[-1].upper() if verdict_m else "UNKNOWN"
    bug_m = re.search(r"BUG:\s*(.*)", result or "", re.S)
    bug_text = bug_m.group(1).strip() if bug_m else ""
    n = len([e for e in t.get("timeline", []) if e["kind"] == "review"]) + 1
    write_file("reviews/%s-%d.md" % (tid, n), "# Review %s #%d — %s\n\n%s\n\n%s\n" % (tid, n, verdict, now(), result))
    with LOCK:
        tt = STATE["tasks"][tid]
        tt["last_review"] = result
        tt["verdict"] = verdict
        tt["bug_draft"] = bug_text if verdict == "FAIL" else ""
        tt["status"] = {"PASS": "pass", "FAIL": "fail"}.get(verdict, "review_unknown")
        log_event(tid, "review", "VERDICT: " + verdict, verdict=verdict)
        save_state()
        auto = CFG.get("auto_mode")
        attempts = tt.get("attempts", 0)
        max_fix = int(CFG.get("max_fix_attempts", 3))
    if auto and verdict == "FAIL":
        if attempts < max_fix:
            send_bug(tid, bug_text or result[-3000:])
        else:
            with LOCK:
                STATE["tasks"][tid]["status"] = "needs_human"
                log_event(tid, "needs_human", "Đã fix %d lần vẫn FAIL — cần Linh xem" % attempts)
                save_state()


def start_job(tid, kind):
    def work():
        try:
            if kind in ("pull", "pull_review"):
                out = git_pull()
                with LOCK:
                    log_event(tid, "git_pull", out or "Already up to date.")
                    save_state()
            if kind in ("review", "pull_review"):
                claude_review(tid)
        except Exception as e:  # noqa: BLE001
            with LOCK:
                if STATE["tasks"].get(tid, {}).get("status") == "reviewing":
                    STATE["tasks"][tid]["status"] = "done"
                log_event(tid, "error", "%s: %s" % (kind, e))
                save_state()
        finally:
            with LOCK:
                RUNTIME["jobs"].pop(tid, None)
    with LOCK:
        if tid in RUNTIME["jobs"]:
            raise RuntimeError("task này đang chạy job khác")
        RUNTIME["jobs"][tid] = kind
    threading.Thread(target=work, daemon=True).start()


def git_show(sha):
    if not repo():
        return "chưa cấu hình repo_path"
    if not re.fullmatch(r"[0-9a-fA-F]{4,40}|HEAD", sha or ""):
        return "sha không hợp lệ"
    code, out, err = run(["git", "-C", repo(), "show", "--stat", "--patch", "--no-color", sha], timeout=30)
    text = out if code == 0 else err
    return text[:200000]


def git_log():
    if not repo():
        return ""
    code, out, err = run(["git", "-C", repo(), "log", "--oneline", "-n", "25", "--no-color"], timeout=20)
    return out if code == 0 else err


# ---------------------------------------------------------------- embedded terminal (pty)
TERMS = {}
TERM_MAX = 4 * 1024 * 1024


def term_start(cmd, cwd, title, plan=None, extra_env=None):
    import pty
    pid, fd = pty.fork()
    if pid == 0:
        try:
            os.chdir(cwd)
        except OSError:
            pass
        env = dict(os.environ, TERM="xterm-256color", COLORTERM="truecolor", LANG=os.environ.get("LANG", "en_US.UTF-8"))
        env.update(extra_env or {})
        shell = "/bin/zsh" if os.path.exists("/bin/zsh") else "/bin/bash"
        os.execvpe(shell, [shell, "-l", "-c", cmd], env)
    tid = "t%d" % (int(time.time() * 1000) % 10000000)
    t = {"id": tid, "pid": pid, "fd": fd, "buf": bytearray(), "base": 0, "alive": True, "title": title, "plan": plan,
         "started": now(), "cond": threading.Condition(), "exit": None, "last_out": time.time(), "need": False, "need_at": 0}
    TERMS[tid] = t

    def reader():
        while True:
            try:
                data = os.read(fd, 65536)
            except OSError:
                data = b""
            with t["cond"]:
                if not data:
                    t["alive"] = False
                    try:
                        _, st = os.waitpid(pid, 0)
                        t["exit"] = os.waitstatus_to_exitcode(st) if hasattr(os, "waitstatus_to_exitcode") else st
                    except ChildProcessError:
                        pass
                    t["cond"].notify_all()
                    break
                t["buf"] += data
                t["last_out"] = time.time()
                t["need"] = False
                if len(t["buf"]) > TERM_MAX:
                    cut = len(t["buf"]) - TERM_MAX
                    del t["buf"][:cut]
                    t["base"] += cut
                t["cond"].notify_all()
        try:
            os.close(fd)
        except OSError:
            pass
    threading.Thread(target=reader, daemon=True).start()
    return tid


def term_read(tid, frm, wait=15):
    t = TERMS[tid]
    with t["cond"]:
        end = t["base"] + len(t["buf"])
        if frm >= end and t["alive"]:
            t["cond"].wait(wait)
            end = t["base"] + len(t["buf"])
        start = max(frm, t["base"])
        chunk = bytes(t["buf"][start - t["base"]:])
        return {"data": base64.b64encode(chunk).decode(), "next": end, "alive": t["alive"], "exit": t["exit"],
                "truncated": frm < t["base"]}


def term_resize(tid, cols, rows):
    import fcntl
    import struct
    import termios
    t = TERMS[tid]
    if t["alive"]:
        fcntl.ioctl(t["fd"], termios.TIOCSWINSZ, struct.pack("HHHH", int(rows), int(cols), 0, 0))


def term_kill(tid):
    import signal
    t = TERMS.get(tid)
    if t and t["alive"]:
        try:
            os.kill(t["pid"], signal.SIGHUP)
        except ProcessLookupError:
            pass


# ---------------------------------------------------------------- plans
PLANS_DIR = os.path.join(HERE, "plans")
PLAN_ID_RE = re.compile(r"^[A-Za-z0-9_.-]+$")
TASK_HEAD = re.compile(r"^##\s+(T[0-9A-Za-z_.-]*)\s*[—\-:]+\s*(.*)$")


def parse_plan(pid):
    path = os.path.join(PLANS_DIR, pid + ".md")
    with open(path, encoding="utf-8") as fh:
        text = fh.read()
    title, tasks, cur, project = pid, [], None, ""
    for line in text.splitlines():
        if line.startswith("# ") and title == pid:
            title = line[2:].strip()
            continue
        if cur is None and line.lower().startswith("project:"):
            project = line.split(":", 1)[1].strip()
            continue
        m = TASK_HEAD.match(line)
        if m:
            cur = {"id": m.group(1), "title": m.group(2).strip(), "owner": "any", "depends": "", "body": ""}
            tasks.append(cur)
            continue
        if cur is not None:
            low = line.strip().lower()
            if low.startswith("owner:"):
                cur["owner"] = line.split(":", 1)[1].strip()
            elif low.startswith("depends:"):
                cur["depends"] = line.split(":", 1)[1].strip()
            cur["body"] += line + "\n"
    status = {}
    spath = os.path.join(PLANS_DIR, pid + ".status.json")
    if os.path.exists(spath):
        try:
            status = json.load(open(spath, encoding="utf-8"))
        except json.JSONDecodeError:
            status = {}
    for t in tasks:
        st = status.get(t["id"], {})
        t["status"], t["note"], t["at"] = st.get("status", "todo"), st.get("note", ""), st.get("at", "")
    muse_wait = [w for w in waiting_list() if w.get("plan") == pid]
    terms = [t for t in TERMS.values() if t.get("plan") == pid]
    last = terms[-1] if terms else None
    proj = os.path.abspath(os.path.expanduser(project)) if project else default_project()
    return {"id": pid, "title": title, "tasks": tasks, "path": path, "mtime": os.path.getmtime(path),
            "project": proj, "project_ok": os.path.isdir(proj), "thread": plan_thread(pid, create=False),
            "muse_wait": muse_wait,
            "has_session": os.path.exists(os.path.join(PLANS_DIR, pid + ".session.json")),
            "term": {"id": last["id"], "alive": last["alive"], "need": bool(last.get("need")),
                     "idle": int(time.time() - last.get("last_out", time.time()))} if last else None}



# ---------- Claude plan usage (5h / tuần) — đọc OAuth token của Claude Code trong Keychain
_USAGE = {"at": 0, "data": None, "good": None, "until": 0}
_USAGE_TTL = 300  # API usage bị rate limit gắt -> cache 5 phút


def _claude_bin():
    import shutil
    for c in [shutil.which("claude"), os.path.expanduser("~/.local/bin/claude"), os.path.expanduser("~/.claude/local/claude"),
              "/opt/homebrew/bin/claude", "/usr/local/bin/claude"]:
        if c and os.path.exists(c):
            return c
    return None


def _refresh_claude_token():
    """Token OAuth chỉ được làm mới khi Claude Code chạy -> gọi 1 lệnh rất nhẹ để nó tự refresh."""
    b = _claude_bin()
    if not b:
        return False
    try:
        subprocess.run([b, "-p", "ok", "--model", "haiku"], capture_output=True, text=True, timeout=60,
                       stdin=subprocess.DEVNULL)
        return True
    except Exception:  # noqa: BLE001
        return False


def claude_usage(force=False):
    import urllib.request, urllib.error
    now = time.time()
    if now < _USAGE["until"] and _USAGE["data"]:  # đang back-off sau 429: không gọi API, kể cả force
        return _USAGE["data"]
    if not force and now - _USAGE["at"] < _USAGE_TTL and _USAGE["data"]:
        return _USAGE["data"]
    out = {"ok": False}

    def fetch():
        r = subprocess.run(["security", "find-generic-password", "-s", "Claude Code-credentials", "-w"],
                           capture_output=True, text=True, timeout=10)
        tok = (json.loads(r.stdout.strip() or "{}").get("claudeAiOauth") or {}).get("accessToken")
        if not tok:
            return None
        req = urllib.request.Request("https://api.anthropic.com/api/oauth/usage", headers={
            "Authorization": "Bearer " + tok, "anthropic-beta": "oauth-2025-04-20",
            "Content-Type": "application/json", "User-Agent": "musude"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode())

    try:
        try:
            d = fetch()
        except urllib.error.HTTPError as e:
            if e.code != 401 or not _refresh_claude_token():
                raise
            d = fetch()  # thử lại sau khi refresh token
        if d is None:
            out["error"] = "Không thấy đăng nhập Claude Code trong Keychain"
        else:
            out = {"ok": True, "five_hour": d.get("five_hour"), "seven_day": d.get("seven_day"),
                   "seven_day_opus": d.get("seven_day_opus"), "seven_day_sonnet": d.get("seven_day_sonnet")}
    except urllib.error.HTTPError as e:
        if e.code == 429:
            try:
                wait = int(e.headers.get("retry-after") or 0)
            except ValueError:
                wait = 0
            wait = max(wait, 300)
            _USAGE["until"] = time.time() + wait
            if _USAGE["good"]:  # giữ số liệu cũ thay vì hiện "—"
                out = dict(_USAGE["good"], stale=True)
                _USAGE.update(at=time.time(), data=out)
                return out
            out["error"] = "HTTP 429 — bị giới hạn, tự thử lại sau %d phút" % (wait // 60)
            _USAGE.update(at=time.time(), data=out)
            return out
        out["error"] = "HTTP %s%s" % (e.code, " — token hết hạn, tự refresh không được: chạy `claude` rồi /login" if e.code == 401 else "")
    except Exception as e:  # noqa: BLE001
        out["error"] = str(e)[:200]
    if out.get("ok"):
        _USAGE["good"] = out
    elif _USAGE["good"]:
        out = dict(_USAGE["good"], stale=True, error=out.get("error"))
    _USAGE.update(at=time.time(), data=out)
    return out


def waiting_list():
    d = os.path.join(agent_dir(), "waiting")
    out = []
    if not os.path.isdir(d):
        return out
    for name in os.listdir(d):
        fp = os.path.join(d, name)
        try:
            w = json.load(open(fp, encoding="utf-8"))
            os.kill(int(w.get("pid", 0)), 0)
        except (OSError, ValueError, json.JSONDecodeError):
            try:
                os.remove(fp)
            except OSError:
                pass
            continue
        w["elapsed"] = time.time() - w.get("since", time.time())
        out.append(w)
    return out


def list_plans():
    os.makedirs(PLANS_DIR, exist_ok=True)
    out = []
    for name in os.listdir(PLANS_DIR):
        if name.endswith(".md") and PLAN_ID_RE.match(name[:-3]):
            try:
                out.append(parse_plan(name[:-3]))
            except Exception as e:  # noqa: BLE001
                out.append({"id": name[:-3], "title": name, "tasks": [], "error": str(e), "mtime": 0})
    return sorted(out, key=lambda p: -p.get("mtime", 0))


def plan_prompt(pid):
    path = os.path.join(PLANS_DIR, pid + ".md")
    return (
        "Dùng skill muse-collab. Thực hiện plan trong file %s (plan id: %s). Project: thư mục hiện tại.\n"
        "1. Đọc plan + trạng thái hiện tại: muse-ask --plan %s --show\n"
        "2. Làm các task chưa done theo thứ tự phụ thuộc. Tự quyết việc nào tự làm qua Unity MCP, việc nào nhờ Muse (muse-ask) — "
        "owner trong plan chỉ là gợi ý. Khi giao Muse, đưa đủ ngữ cảnh từ plan vào task.\n"
        "3. Mỗi task: bắt đầu → muse-ask --plan %s --task <Tn> --status doing \"...\"; xong và đã verify → --status done \"kết quả\"; "
        "vướng → --status blocked \"lý do\".\n"
        "%s"
        "5. Kết thúc: báo ngắn task nào xong, Claude làm gì, Muse làm gì, còn gì chưa verify."
    ) % (path, pid, pid, pid, autonomy_rule())


def autonomy_rule():
    if not CFG.get("plan_autonomous", True):
        return "4. Tuân thủ điểm dừng của skill (xoá file, scene chính, Play/Build… thì hỏi Linh).\n"
    return ("4. CHẠY TỰ ĐỘNG — Linh không ngồi canh terminal. ĐƯỢC PHÉP không cần hỏi: vào/thoát Play mode trong Unity Editor để test/verify "
            "(nhớ thoát Play, không lưu scene khi đang Play), đọc console, chạy test, sửa file trong phạm vi task, giao/nhắc Muse, "
            "chuyển sang task kế tiếp. KHÔNG hỏi kiểu 'làm tiếp không?', 'có muốn tôi…?' — cứ làm tiếp tới hết plan. "
            "Chỉ DỪNG hỏi Linh khi: xoá file/asset, lưu scene chính hoặc prefab dùng chung ngoài phạm vi, Build, git push/reset, "
            "hoặc cần quyết định thiết kế không suy ra được từ plan — khi đó đánh task --status blocked \"câu hỏi cụ thể\" "
            "rồi LÀM TIẾP các task khác không phụ thuộc, hỏi gộp một lần ở cuối.\n")


def applescript_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def new_session(title):
    res = muse(["session-start", "--title", title], timeout=60)
    sid = find_key(res, "session_id") or find_key(res, "id")
    if not sid:
        raise RuntimeError("không tạo được side chat: " + json.dumps(res)[:300])
    return sid


def plan_thread(pid, create=True):
    """Side chat riêng của plan (plans/<pid>.thread.json). Tắt thread_per_plan → dùng chat chính."""
    if not CFG.get("thread_per_plan", True):
        return CFG.get("thread_id", "")
    path = os.path.join(PLANS_DIR, pid + ".thread.json")
    if os.path.exists(path):
        try:
            return json.load(open(path, encoding="utf-8"))["thread_id"]
        except (json.JSONDecodeError, KeyError):
            pass
    if not create:
        return ""
    sid = new_session("Bridge · " + pid)
    with open(path, "w", encoding="utf-8") as fh:
        json.dump({"thread_id": sid, "created": now()}, fh)
    with LOCK:
        log_event(pid, "thread_new", "Tạo side chat Muse riêng cho plan", by="app", thread=sid)
        save_state()
    return sid


def plan_session(pid, new=False):
    """Session id Claude Code của plan (plans/<pid>.session.json) để 'Tiếp tục' đúng cuộc trò chuyện."""
    import uuid
    path = os.path.join(PLANS_DIR, pid + ".session.json")
    if not new and os.path.exists(path):
        try:
            return json.load(open(path, encoding="utf-8"))["session_id"], False
        except (json.JSONDecodeError, KeyError):
            pass
    sid = str(uuid.uuid4())
    with open(path, "w", encoding="utf-8") as fh:
        json.dump({"session_id": sid, "created": now()}, fh)
    return sid, True


def resume_prompt(pid):
    return ("Tiếp tục plan %s (phiên trước bị dừng giữa chừng). Chạy lại: muse-ask --plan %s --show. "
            "Task đang 'doing': kiểm tra trước (muse-ask --history 10 trong chat của plan, file đã tạo/sửa) — "
            "Muse đã trả lời DONE thì dùng kết quả, KHÔNG gửi lại; chưa có thì --wait hoặc làm tiếp. "
            "Rồi làm các task còn lại theo skill muse-collab.") % (pid, pid)


def plan_flags():
    f = ""
    if CFG.get("plan_permission_mode"):
        f += " --permission-mode " + shq(CFG["plan_permission_mode"])
    tools = CFG.get("plan_allowed_tools") or []
    if tools:
        f += " --allowedTools " + shq(",".join(tools))
    return f


def run_plan_embedded(pid, resume=False):
    if not PLAN_ID_RE.match(pid) or not os.path.exists(os.path.join(PLANS_DIR, pid + ".md")):
        raise RuntimeError("không có plan " + pid)
    wd = parse_plan(pid)["project"]
    if not os.path.isdir(wd):
        raise RuntimeError("không thấy thư mục project: " + wd)
    pfile = os.path.join(PLANS_DIR, pid + ".prompt.txt")
    claude = os.path.expanduser(CFG.get("claude_bin", "claude"))
    thread = plan_thread(pid)
    if resume and os.path.exists(os.path.join(PLANS_DIR, pid + ".session.json")):
        sid, _ = plan_session(pid)
        with open(pfile, "w", encoding="utf-8") as fh:
            fh.write(resume_prompt(pid))
        cmd = "%s --resume %s \"$(cat %s)\"%s" % (shq(claude), shq(sid), shq(pfile), plan_flags())
        note = "Tiếp tục phiên Claude cũ của plan"
    else:
        sid, _ = plan_session(pid, new=True)
        with open(pfile, "w", encoding="utf-8") as fh:
            fh.write(plan_prompt(pid))
        cmd = "%s --session-id %s \"$(cat %s)\"%s" % (shq(claude), shq(sid), shq(pfile), plan_flags())
        note = "Chạy plan trong terminal của app (phiên mới)"
    tid = term_start(cmd, wd, "plan " + pid, plan=pid,
                     extra_env=dict({"MUSE_PLAN": pid, "MUSE_PROJECT": wd}, **({"MUSE_THREAD": thread} if thread else {})))
    with LOCK:
        log_event(pid, "plan_run", note, by="app")
        save_state()
    return tid


def run_plan(pid):
    if not PLAN_ID_RE.match(pid) or not os.path.exists(os.path.join(PLANS_DIR, pid + ".md")):
        raise RuntimeError("không có plan " + pid)
    wd = parse_plan(pid)["project"]
    if not os.path.isdir(wd):
        raise RuntimeError("không thấy thư mục project: " + wd)
    pfile = os.path.join(PLANS_DIR, pid + ".prompt.txt")
    with open(pfile, "w", encoding="utf-8") as fh:
        fh.write(plan_prompt(pid))
    claude = os.path.expanduser(CFG.get("claude_bin", "claude"))
    thread = plan_thread(pid)
    cmd = "cd %s && MUSE_PLAN=%s MUSE_PROJECT=%s MUSE_THREAD=%s %s \"$(cat %s)\"" % (shq(wd), shq(pid), shq(wd), shq(thread), shq(claude), shq(pfile))
    script = 'tell application "Terminal"\nactivate\ndo script %s\nend tell' % applescript_str(cmd)
    code, out, err = run(["osascript", "-e", script], timeout=20)
    if code != 0:
        raise RuntimeError("không mở được Terminal: " + (err or out)[:400])
    with LOCK:
        log_event(pid, "plan_run", "Mở Terminal chạy plan (Claude Code tương tác)", by="app")
        save_state()


# ---------------------------------------------------------------- recycle bin
TRASH_DIR = os.path.join(HERE, "trash")
TRASH_ID_RE = re.compile(r"^[A-Za-z0-9_.-]+$")


def trash_put(kind, title, meta, files=()):
    os.makedirs(TRASH_DIR, exist_ok=True)
    tid = "%s-%s-%d" % (datetime.now().strftime("%Y%m%d%H%M%S"), kind, int(time.time() * 1000) % 1000)
    d = os.path.join(TRASH_DIR, tid)
    os.makedirs(os.path.join(d, "files"))
    for src in files:
        os.replace(src, os.path.join(d, "files", os.path.basename(src)))
    meta = dict(meta, kind=kind, title=title, deleted_at=time.time(), id=tid)
    with open(os.path.join(d, "meta.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, ensure_ascii=False, indent=1)
    return tid


def trash_list():
    out = []
    if not os.path.isdir(TRASH_DIR):
        return out
    days = float(CFG.get("trash_days", 3))
    for name in os.listdir(TRASH_DIR):
        mp = os.path.join(TRASH_DIR, name, "meta.json")
        if not os.path.exists(mp):
            continue
        try:
            m = json.load(open(mp, encoding="utf-8"))
        except json.JSONDecodeError:
            continue
        m["expires_in"] = max(0, m.get("deleted_at", 0) + days * 86400 - time.time())
        m.pop("section", None)
        m.pop("task", None)
        out.append(m)
    return sorted(out, key=lambda m: -m.get("deleted_at", 0))


def trash_purge(tid=None, expired_only=False):
    if not os.path.isdir(TRASH_DIR):
        return 0
    n, days = 0, float(CFG.get("trash_days", 3))
    for name in os.listdir(TRASH_DIR):
        if tid and name != tid:
            continue
        d = os.path.join(TRASH_DIR, name)
        mp = os.path.join(d, "meta.json")
        if expired_only:
            try:
                at = json.load(open(mp, encoding="utf-8")).get("deleted_at", 0)
            except Exception:  # noqa: BLE001
                at = os.path.getmtime(d)
            if time.time() - at < days * 86400:
                continue
        if os.path.realpath(d).startswith(os.path.realpath(TRASH_DIR) + os.sep):
            shutil.rmtree(d, ignore_errors=True)
            n += 1
    return n


def _tnum(tn):
    m = re.search(r"\d+", tn or "")
    return int(m.group()) if m else 10 ** 9


def insert_section(md_lines, section_lines):
    head = TASK_HEAD.match(section_lines[0]) if section_lines else None
    me = _tnum(head.group(1)) if head else 10 ** 9
    for i, ln in enumerate(md_lines):
        m = TASK_HEAD.match(ln)
        if (m and _tnum(m.group(1)) > me) or (ln.startswith("## ") and not m):
            return md_lines[:i] + section_lines + md_lines[i:]
    return md_lines + [""] + section_lines


def trash_restore(tid):
    if not TRASH_ID_RE.match(tid):
        raise RuntimeError("id lạ")
    d = os.path.join(TRASH_DIR, tid)
    m = json.load(open(os.path.join(d, "meta.json"), encoding="utf-8"))
    kind = m["kind"]
    if kind in ("plan", "legacy"):
        os.makedirs(PLANS_DIR, exist_ok=True)
        for name in os.listdir(os.path.join(d, "files")):
            dest = os.path.join(PLANS_DIR, name)
            if os.path.exists(dest):
                raise RuntimeError("đã có %s trong plans — đổi tên hoặc xoá bản hiện tại trước" % name)
        for name in os.listdir(os.path.join(d, "files")):
            os.replace(os.path.join(d, "files", name), os.path.join(PLANS_DIR, name))
    elif kind == "plan_task":
        path = os.path.join(PLANS_DIR, m["plan"] + ".md")
        if not os.path.exists(path):
            raise RuntimeError("plan %s không còn — khôi phục plan trước" % m["plan"])
        lines = open(path, encoding="utf-8").read().split("\n")
        if any((TASK_HEAD.match(ln) or [None, None])[1] == m["task_id"] for ln in lines if TASK_HEAD.match(ln)):
            raise RuntimeError("plan đã có task %s" % m["task_id"])
        with open(path, "w", encoding="utf-8") as fh:
            fh.write("\n".join(insert_section(lines, m["section"])))
        if m.get("status"):
            spath = os.path.join(PLANS_DIR, m["plan"] + ".status.json")
            st = json.load(open(spath, encoding="utf-8")) if os.path.exists(spath) else {}
            st[m["task_id"]] = m["status"]
            json.dump(st, open(spath, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    elif kind == "task":
        with LOCK:
            if m["task"]["id"] in STATE["tasks"]:
                raise RuntimeError("đã có task " + m["task"]["id"])
            STATE["tasks"][m["task"]["id"]] = m["task"]
            save_state()
    shutil.rmtree(d, ignore_errors=True)
    with LOCK:
        log_event("-", "restored", "Khôi phục: " + m.get("title", tid), by="app")
        save_state()


def migrate_old_archives():
    adir = os.path.join(PLANS_DIR, "_archive")
    if os.path.isdir(adir):
        for name in os.listdir(adir):
            src = os.path.join(adir, name)
            files = [os.path.join(src, f) for f in os.listdir(src)] if os.path.isdir(src) else [src]
            tid = trash_put("legacy", "Bản lưu cũ: " + name, {})
            mp = os.path.join(TRASH_DIR, tid, "meta.json")
            m = json.load(open(mp, encoding="utf-8"))
            m["deleted_at"] = os.path.getmtime(src)
            json.dump(m, open(mp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
            for f in files:
                os.replace(f, os.path.join(TRASH_DIR, tid, "files", os.path.basename(f)))
            if os.path.isdir(src):
                shutil.rmtree(src, ignore_errors=True)
        shutil.rmtree(adir, ignore_errors=True)
    with LOCK:
        old = STATE.pop("deleted_tasks", None)
        if old:
            for t in old.values():
                trash_put("task", "%s — %s" % (t["id"], t.get("title", "")), {"task": t})
            save_state()


def idle_watch_loop():
    """Terminal plan im lặng quá idle_alert_seconds khi còn sống = Claude đang chờ Linh → đánh dấu + thông báo macOS 1 lần."""
    while True:
        time.sleep(2)
        lim = max(4, int(CFG.get("idle_alert_seconds", 8)))
        for t in list(TERMS.values()):
            if not t.get("alive") or t.get("need") or not t.get("plan"):
                continue
            if time.time() - t.get("last_out", time.time()) < lim:
                continue
            try:
                p = parse_plan(t["plan"])
                if p["tasks"] and all(x["status"] == "done" for x in p["tasks"]):
                    continue
                if [w for w in waiting_list() if w.get("plan") == t["plan"]]:
                    continue
                title = p.get("title") or t["plan"]
            except Exception:  # noqa: BLE001
                title = t["plan"]
            t["need"], t["need_at"] = True, time.time()
            msg = "Claude đang chờ anh ở plan: " + title
            run(["osascript", "-e", 'display notification %s with title "Musude" sound name "Glass"' % applescript_str(msg[:180])], timeout=10)
            with LOCK:
                log_event(t["plan"], "needs_human", "Terminal dừng chờ xác nhận/trả lời", by="app")


def trash_loop():
    while True:
        try:
            trash_purge(expired_only=True)
        except Exception:  # noqa: BLE001
            pass
        time.sleep(3600)


def archive_plan(pid):
    if not PLAN_ID_RE.match(pid) or not os.path.exists(os.path.join(PLANS_DIR, pid + ".md")):
        raise RuntimeError("không có plan " + pid)
    title = parse_plan(pid)["title"]
    files = [os.path.join(PLANS_DIR, n) for n in os.listdir(PLANS_DIR)
             if n == pid + ".md" or (n.startswith(pid + ".") and os.path.isfile(os.path.join(PLANS_DIR, n)))]
    trash_put("plan", "Plan: " + title, {"plan": pid}, files)
    with LOCK:
        log_event(pid, "plan_archived", "Đã chuyển plan vào thùng rác", by="app")
        save_state()


def delete_plan_task(pid, tn):
    path = os.path.join(PLANS_DIR, pid + ".md")
    if not PLAN_ID_RE.match(pid) or not os.path.exists(path):
        raise RuntimeError("không có plan " + pid)
    with open(path, encoding="utf-8") as fh:
        lines = fh.read().split("\n")
    out, section, skip, title = [], [], False, tn
    for ln in lines:
        m = TASK_HEAD.match(ln)
        if m or ln.startswith("## "):
            skip = bool(m and m.group(1) == tn)
            if skip:
                title = m.group(2).strip()
        (section if skip else out).append(ln)
    if not section:
        raise RuntimeError("không có task %s trong plan" % tn)
    status = None
    spath = os.path.join(PLANS_DIR, pid + ".status.json")
    if os.path.exists(spath):
        try:
            st = json.load(open(spath, encoding="utf-8"))
            status = st.pop(tn, None)
            json.dump(st, open(spath, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        except json.JSONDecodeError:
            pass
    while section and not section[-1].strip():
        section.pop()
    trash_put("plan_task", "%s / %s — %s" % (pid, tn, title),
              {"plan": pid, "task_id": tn, "section": section + [""], "status": status})
    with open(path, "w", encoding="utf-8") as fh:
        fh.write("\n".join(out))
    with LOCK:
        log_event("%s/%s" % (pid, tn), "plan_task_deleted", "Đã chuyển task vào thùng rác", by="app")
        save_state()


def shq(s):
    return "'" + s.replace("'", "'\\''") + "'"


# ---------------------------------------------------------------- http
class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, code, payload, ctype="application/json"):
        body = payload if isinstance(payload, bytes) else json.dumps(payload, ensure_ascii=False).encode()
        self.send_response(code)
        self.send_header("Content-Type", ctype + "; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def _body(self):
        n = int(self.headers.get("Content-Length") or 0)
        return json.loads(self.rfile.read(n) or b"{}") if n else {}

    def do_GET(self):
        u = urlparse(self.path)
        q = parse_qs(u.query)
        if u.path in ("/", "/index.html"):
            with open(os.path.join(HERE, "index.html"), "rb") as fh:
                return self._send(200, fh.read(), "text/html")
        if u.path == "/api/state":
            with LOCK:
                cfg = {k: CFG[k] for k in CFG}
                return self._send(200, {"config": cfg, "state": STATE, "agent_dir": agent_dir(),
                                        "runtime": {k: RUNTIME[k] for k in RUNTIME}})
        if u.path == "/api/git/show":
            return self._send(200, {"text": git_show((q.get("sha") or [""])[0])})
        if u.path.startswith("/vendor/"):
            name = os.path.basename(u.path)
            fp = os.path.join(HERE, "vendor", name)
            if os.path.exists(fp):
                ctype = "text/css" if name.endswith(".css") else "application/javascript"
                with open(fp, "rb") as fh:
                    return self._send(200, fh.read(), ctype)
            return self._send(404, {"error": "not found"})
        if u.path == "/api/threads":
            items = [{"id": CFG.get("thread_id", ""), "title": "Chat chính", "main": True}]
            for pl in list_plans():
                if pl.get("thread") and pl["thread"] != CFG.get("thread_id"):
                    items.append({"id": pl["thread"], "title": "Plan · " + pl["id"], "main": False})
            return self._send(200, {"threads": items})
        if u.path == "/api/thread":
            th = (q.get("id") or [""])[0]
            if not re.fullmatch(r"[A-Za-z0-9-]{8,64}", th):
                return self._send(400, {"error": "thread id lạ"})
            if th == CFG.get("thread_id"):
                return self._send(200, {"messages": RUNTIME["messages"]})
            try:
                msgs = muse(["history", "--thread", th, "--limit", "30"], timeout=60)
            except Exception as e:  # noqa: BLE001
                return self._send(200, {"messages": [], "error": str(e)})
            msgs = sorted(msgs if isinstance(msgs, list) else [], key=lambda m: m.get("seq") or 0)
            return self._send(200, {"messages": msgs})
        if u.path == "/api/usage":
            return self._send(200, claude_usage(force="force" in u.query))
        if u.path == "/api/terms":
            return self._send(200, {"terms": [{"id": t["id"], "title": t["title"], "alive": t["alive"], "started": t["started"],
                                               "exit": t["exit"], "plan": t.get("plan")} for t in TERMS.values()]})
        m = re.fullmatch(r"/api/term/(t\d+)/read", u.path)
        if m:
            if m.group(1) not in TERMS:
                return self._send(404, {"error": "không có terminal"})
            return self._send(200, term_read(m.group(1), int((q.get("from") or ["0"])[0])))
        if u.path == "/api/trash":
            return self._send(200, {"items": trash_list(), "days": CFG.get("trash_days", 3)})
        if u.path == "/api/plans":
            return self._send(200, {"plans": list_plans(), "dir": PLANS_DIR})
        if u.path == "/api/activity":
            limit = int((q.get("limit") or ["300"])[0])
            path = os.path.join(agent_dir(), "events.jsonl")
            rows = []
            if os.path.exists(path):
                with open(path, encoding="utf-8") as fh:
                    lines = fh.readlines()[-limit:]
                for ln in lines:
                    try:
                        rows.append(json.loads(ln))
                    except json.JSONDecodeError:
                        pass
            return self._send(200, {"events": rows[::-1]})
        if u.path == "/api/git/log":
            return self._send(200, {"text": git_log()})
        return self._send(404, {"error": "not found"})

    def do_POST(self):
        if self.headers.get("X-Bridge") != "1":
            return self._send(403, {"error": "missing X-Bridge header"})
        u = urlparse(self.path)
        try:
            b = self._body()
            p = u.path
            if p == "/api/status":
                try:
                    st = muse(["status"], timeout=60)
                    with LOCK:
                        RUNTIME["status"], RUNTIME["status_error"] = st, ""
                except Exception as e:  # noqa: BLE001
                    with LOCK:
                        RUNTIME["status"], RUNTIME["status_error"] = None, str(e)
                return self._send(200, {"ok": True})
            if p == "/api/chat/new":
                sid = new_session(b.get("title") or ("Bridge · " + datetime.now().strftime("%d/%m %H:%M")))
                with LOCK:
                    CFG["thread_id"] = sid
                    STATE["last_seq"] = 0
                    save_config()
                    log_event("-", "thread_new", "Tạo chat chính mới", by="app", thread=sid)
                    save_state()
                WAKE.set()
                return self._send(200, {"thread_id": sid})
            if p == "/api/session/start":
                res = muse(["session-start", "--title", b.get("title") or "Unity collab"], timeout=60)
                sid = find_key(res, "session_id") or find_key(res, "id")
                if not sid:
                    raise RuntimeError("không đọc được session_id: " + json.dumps(res)[:400])
                with LOCK:
                    CFG["thread_id"] = sid
                    save_config()
                WAKE.set()
                return self._send(200, {"thread_id": sid, "raw": res})
            if p == "/api/config":
                allowed = set(DEFAULT_CONFIG) - {"port"}
                with LOCK:
                    old_dir = agent_dir()
                    for k, v in b.items():
                        if k in allowed:
                            CFG[k] = v
                    save_config()
                    if agent_dir() != old_dir:
                        load_state()
                WAKE.set()
                return self._send(200, {"ok": True})
            if p == "/api/ping":
                bg(muse_send, "[TASK-PING] Hãy reply đúng nguyên văn: [DONE-PING] PONG")
                RUNTIME["ping_at"] = time.time()
                WAKE.set()
                return self._send(200, {"ok": True})
            if p == "/api/message":
                bg(muse_send, b.get("text", ""))
                return self._send(200, {"ok": True})
            if p == "/api/poll":
                poll_once()
                return self._send(200, {"ok": True})
            if p == "/api/task":
                tid = create_task(b.get("title", ""), b.get("body", ""), send=b.get("send", True))
                return self._send(200, {"id": tid})
            m = re.fullmatch(r"/api/trash/([A-Za-z0-9_.-]+)/(restore|purge)", p)
            if m:
                if m.group(2) == "restore":
                    trash_restore(m.group(1))
                else:
                    trash_purge(m.group(1))
                return self._send(200, {"ok": True})
            if p == "/api/trash/empty":
                return self._send(200, {"purged": trash_purge()})
            m = re.fullmatch(r"/api/plan/([A-Za-z0-9_.-]+)/task/(T[0-9A-Za-z_.-]*)/delete", p)
            if m:
                delete_plan_task(m.group(1), m.group(2))
                return self._send(200, {"ok": True})
            m = re.fullmatch(r"/api/plan/([A-Za-z0-9_.-]+)/(run|delete)", p)
            if m:
                if m.group(2) == "run":
                    if b.get("mode") == "external":
                        run_plan(m.group(1))
                    else:
                        return self._send(200, {"ok": True, "term": run_plan_embedded(m.group(1), resume=b.get("mode") == "resume")})
                else:
                    archive_plan(m.group(1))
                return self._send(200, {"ok": True})
            if p == "/api/notify":
                msg = str(b.get("text") or "")[:200]
                script = 'display notification %s with title "Musude" sound name "Glass"' % applescript_str(msg)
                threading.Thread(target=run, args=(["osascript", "-e", script],), kwargs={"timeout": 10}, daemon=True).start()
                return self._send(200, {"ok": True})
            if p == "/api/term/start":
                wd = os.path.expanduser(b.get("cwd") or CFG.get("claude_workdir") or "~")
                cmd = os.path.expanduser(CFG.get("claude_bin", "claude")) if b.get("kind", "claude") == "claude" else "exec $SHELL -l"
                if b.get("kind") == "claude":
                    cmd = shq(cmd)
                return self._send(200, {"term": term_start(cmd, wd, b.get("title") or b.get("kind", "claude"))})
            if p == "/api/terms/clear":
                for k in [k for k, t in TERMS.items() if not t["alive"]]:
                    TERMS.pop(k, None)
                return self._send(200, {"ok": True})
            m = re.fullmatch(r"/api/term/(t\d+)/delete", p)
            if m:
                tid = m.group(1)
                if tid in TERMS:
                    term_kill(tid)
                    TERMS.pop(tid, None)
                return self._send(200, {"ok": True})
            m = re.fullmatch(r"/api/term/(t\d+)/(write|resize|kill)", p)
            if m:
                tid, action = m.groups()
                if tid not in TERMS:
                    return self._send(404, {"error": "không có terminal"})
                if action == "write":
                    if TERMS[tid]["alive"]:
                        os.write(TERMS[tid]["fd"], b.get("data", "").encode("utf-8"))
                elif action == "resize":
                    term_resize(tid, b.get("cols", 80), b.get("rows", 24))
                else:
                    term_kill(tid)
                return self._send(200, {"ok": True})
            if p == "/api/plans/open":
                os.makedirs(PLANS_DIR, exist_ok=True)
                run(["open", PLANS_DIR], timeout=10)
                return self._send(200, {"ok": True})
            m = re.fullmatch(r"/api/task/(TASK-\d+)/(\w+)", p)
            if m:
                tid, action = m.groups()
                if tid not in STATE["tasks"]:
                    return self._send(404, {"error": "không có task"})
                if action == "send":
                    bg(send_task, tid, task_id=tid)
                elif action == "bug":
                    bg(send_bug, tid, b.get("text", ""), task_id=tid)
                elif action in ("pull", "review", "pull_review"):
                    start_job(tid, action)
                elif action == "set_status":
                    with LOCK:
                        STATE["tasks"][tid]["status"] = b.get("status", "pass")
                        log_event(tid, "manual", "Linh đặt trạng thái: " + b.get("status", ""))
                        save_state()
                elif action == "delete":
                    with LOCK:
                        t = STATE["tasks"].pop(tid)
                        trash_put("task", "%s — %s" % (tid, t.get("title", "")), {"task": t})
                        log_event(tid, "task_deleted", "Đã chuyển task vào thùng rác", by="app")
                        save_state()
                elif action == "note":
                    with LOCK:
                        log_event(tid, "note", b.get("text", ""))
                        save_state()
                else:
                    return self._send(400, {"error": "action lạ"})
                return self._send(200, {"ok": True})
            return self._send(404, {"error": "not found"})
        except Exception as e:  # noqa: BLE001
            return self._send(500, {"error": str(e)})


def main():
    load_config()
    if not os.path.exists(CONFIG_PATH):
        save_config()
    load_state()
    threading.Thread(target=poller, daemon=True).start()
    threading.Thread(target=relay_loop, daemon=True).start()
    migrate_old_archives()
    threading.Thread(target=trash_loop, daemon=True).start()
    threading.Thread(target=idle_watch_loop, daemon=True).start()
    port = int(CFG.get("port", 8765))
    srv = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    print("Musude: http://127.0.0.1:%d  (Ctrl+C để dừng)" % port)
    print("Lịch sử lưu ở:", agent_dir())
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\nĐã dừng.")
        sys.exit(0)


if __name__ == "__main__":
    main()
