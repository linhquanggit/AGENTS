#!/usr/bin/env python3
"""Chỉ số hiệu quả cho mỗi plan Musude. Chỉ parse file, không gọi LLM.
Cache: plans/<id>.metrics.json (transcript chỉ parse lại khi mtime/size đổi; events.jsonl cũng vậy).
Usage: python3 metrics.py <plan_id>"""
import glob, json, os, re, sys
from datetime import datetime

HERE = os.path.dirname(os.path.abspath(__file__))
PLANS = os.path.join(HERE, "plans")
MCP_PREFIX = ("mcp__unityMCP__", "mcp__remote-devices__unityMCP__")
MANUAL = ("loi_lot_review", "loi_linh_phat_hien")
REPORT = os.path.expanduser("~/AGENTS/Reports/WB/metrics.md")
LOG_RULE_START = datetime(2026, 10, 23)
ID_RE = re.compile(r"^[A-Za-z0-9_.-]+$")


def _agent_dir():
    try:
        cfg = json.load(open(os.path.join(HERE, "config.json"), encoding="utf-8"))
    except Exception:
        cfg = {}
    if cfg.get("agent_dir"):
        return os.path.expanduser(cfg["agent_dir"])
    if cfg.get("repo_path"):
        return os.path.join(os.path.expanduser(cfg["repo_path"]), ".agent")
    return os.path.join(HERE, ".agent")


def _jload(path, default):
    try:
        return json.load(open(path, encoding="utf-8"))
    except Exception:
        return default


def _sig(path):
    try:
        s = os.stat(path)
        return [int(s.st_mtime), s.st_size]
    except OSError:
        return None


def _minutes(status):
    ts = []
    for v in status.values():
        try:
            ts.append(datetime.fromisoformat(v["at"]))
        except Exception:
            pass
    if len(ts) < 2:
        return None, (ts[0] if ts else None), None
    return round((max(ts) - min(ts)).total_seconds() / 60, 1), min(ts), max(ts)


def _find_transcript(sid):
    hits = glob.glob(os.path.expanduser("~/.claude/projects/*/%s.jsonl" % sid))
    return hits[0] if hits else None


def _parse_transcript(path):
    mcp, models = 0, set()
    usage = {}  # message.id -> usage (mỗi message có thể lặp qua nhiều dòng; lấy bản cuối)
    seen_tool = set()
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if '"assistant"' not in line:
                continue
            try:
                d = json.loads(line)
            except Exception:
                continue
            if d.get("type") != "assistant":
                continue
            m = d.get("message") or {}
            if m.get("model") and not m["model"].startswith("<"):
                models.add(m["model"])
            u = m.get("usage")
            if u:
                usage[m.get("id") or d.get("uuid") or len(usage)] = u
            for c in m.get("content") or []:
                if isinstance(c, dict) and c.get("type") == "tool_use":
                    tid = c.get("id")
                    if tid in seen_tool:
                        continue
                    seen_tool.add(tid)
                    if str(c.get("name", "")).startswith(MCP_PREFIX):
                        mcp += 1
    tok = {"input": 0, "output": 0, "cache_creation": 0, "cache_read": 0}
    for u in usage.values():
        tok["input"] += u.get("input_tokens") or 0
        tok["output"] += u.get("output_tokens") or 0
        tok["cache_creation"] += u.get("cache_creation_input_tokens") or 0
        tok["cache_read"] += u.get("cache_read_input_tokens") or 0
    return {"mcp": mcp, "tokens": tok, "models": sorted(models)}


def _all_windows():
    out = {}
    for p in glob.glob(os.path.join(PLANS, "*.status.json")):
        pid = os.path.basename(p)[:-len(".status.json")]
        _, a, b = _minutes(_jload(p, {}))
        if a and b:
            out[pid] = (a, b)
    return out


def _parse_events(pid, start, end, windows):
    play = selfcode = bug = 0
    ev_path = os.path.join(_agent_dir(), "events.jsonl")
    if not os.path.exists(ev_path):
        return None
    others = {k: v for k, v in windows.items() if k != pid}
    with open(ev_path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            try:
                e = json.loads(line)
            except Exception:
                continue
            kind, text, task = e.get("kind", ""), e.get("text", ""), str(e.get("task", ""))
            if not (kind == "claude_bug" or text.startswith(("[play]", "[self-code]"))):
                continue
            explicit = task.startswith(pid + "/") or ("plan " + pid) in text[:200]
            if not explicit:
                if not (start and end):
                    continue
                try:
                    at = datetime.fromisoformat(e["at"])
                except Exception:
                    continue
                if not (start <= at <= end) or any(a <= at <= b for a, b in others.values()):
                    continue  # ngoài cửa sổ plan, hoặc mơ hồ vì trùng cửa sổ plan khác
            if text.startswith("[play]"):
                play += 1
            elif text.startswith("[self-code]"):
                selfcode += 1
            elif kind == "claude_bug":
                bug += 1
    return {"play": play, "self_code": selfcode, "review_rounds": bug}


def _title(pid):
    try:
        for ln in open(os.path.join(PLANS, pid + ".md"), encoding="utf-8"):
            if ln.startswith("#"):
                return ln.lstrip("# ").strip()
    except Exception:
        pass
    return pid


def compute(pid, force=False):
    if not ID_RE.match(pid) or not os.path.exists(os.path.join(PLANS, pid + ".md")):
        return {"error": "không có plan " + pid}
    mpath = os.path.join(PLANS, pid + ".metrics.json")
    cache = _jload(mpath, {})
    manual = {k: cache.get(k) for k in MANUAL}
    spath = os.path.join(PLANS, pid + ".status.json")
    status = _jload(spath, {})
    minutes, start, end = _minutes(status)
    notes = {}
    res = {"id": pid, "title": _title(pid), "date": start.strftime("%Y-%m-%d") if start else None,
           "minutes": minutes, "mcp": None, "tokens": None, "models": None,
           "play": None, "self_code": None, "review_rounds": None, "notes": notes}
    if minutes is None:
        notes["minutes"] = "status.json thiếu mốc `at` (cần ≥2 mốc)"

    # transcript — chỉ parse lại khi mtime/size đổi
    sess = _jload(os.path.join(PLANS, pid + ".session.json"), {})
    sid = sess.get("session_id")
    tpath = _find_transcript(sid) if sid else None
    if not sid:
        notes["transcript"] = "không có plans/%s.session.json" % pid
    elif not tpath:
        notes["transcript"] = "không thấy ~/.claude/projects/*/%s.jsonl" % sid
    else:
        sig = _sig(tpath)
        c = cache.get("transcript")
        if not force and c and c.get("sig") == sig:
            tr = c["data"]
        else:
            try:
                tr = _parse_transcript(tpath)
            except Exception as ex:
                tr, notes["transcript"] = None, "lỗi parse: %s" % ex
        if tr:
            res.update(mcp=tr["mcp"], tokens=tr["tokens"], models=tr["models"])
            cache["transcript"] = {"sig": sig, "data": tr}

    # log muse-ask — parse lại khi events.jsonl / status đổi
    esig = [_sig(os.path.join(_agent_dir(), "events.jsonl")), _sig(spath)]
    c = cache.get("events")
    if not force and c and c.get("sig") == esig:
        ev = c["data"]
    else:
        try:
            ev = _parse_events(pid, start, end, _all_windows())
        except Exception as ex:
            ev, notes["events"] = None, "lỗi parse log: %s" % ex
    if ev is not None:
        res.update(ev)
        # [play]/[self-code] chỉ được ghi từ khi có luật mới -> plan cũ mà đếm 0 = chưa ghi, không phải 0
        if not end or end < LOG_RULE_START:
            for k in ("play", "self_code"):
                if res[k] == 0:
                    res[k] = None
                    notes[k] = "plan trước %s, chưa có quy ước log [play]/[self-code]" % LOG_RULE_START.date()
        cache["events"] = {"sig": esig, "data": ev}
        if not (start and end):
            notes["events"] = "plan chưa có cửa sổ thời gian; chỉ đếm event ghi rõ tên plan"
    elif "events" not in notes:
        notes["events"] = "không thấy events.jsonl"

    res.update(manual)
    out = dict(cache)
    out.update(manual)
    try:
        tmp = mpath + ".tmp"
        json.dump(out, open(tmp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
        os.replace(tmp, mpath)
    except Exception as ex:
        notes["cache"] = "không ghi được cache: %s" % ex
    res["exported"] = _exported(pid)
    return res


def set_manual(pid, field, value):
    if field not in MANUAL or not ID_RE.match(pid):
        return {"error": "trường không hợp lệ"}
    if value in ("", None):
        v = None
    else:
        try:
            v = int(value)
            if v < 0:
                raise ValueError
        except (TypeError, ValueError):
            return {"error": "cần số nguyên ≥ 0 hoặc để trống"}
    mpath = os.path.join(PLANS, pid + ".metrics.json")
    cache = _jload(mpath, {})
    cache[field] = v
    json.dump(cache, open(mpath, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    return {"ok": True}


def _exported(pid):
    try:
        for ln in open(REPORT, encoding="utf-8"):
            if ln.startswith("|") and (ln.split("|")[1].strip().split() or [""])[0] == pid:
                return True
    except Exception:
        pass
    return False


def _cell(v):
    return "không ghi" if v is None else str(v)


def export(pid):
    m = compute(pid)
    if "error" in m:
        return m
    if not os.path.exists(REPORT):
        return {"error": "chưa có " + REPORT}
    if m["exported"]:
        return {"error": "Mission %s đã có trong metrics.md" % pid}
    tk = m["tokens"]
    tok = "không ghi" if not tk else "%d (in %d / out %d / cache-ghi %d / cache-đọc %d)" % (
        sum(tk.values()), tk["input"], tk["output"], tk["cache_creation"], tk["cache_read"])
    row = [pid, m["date"], ", ".join(m["models"]) if m["models"] else None, m["title"], m["minutes"],
           m["mcp"], m["play"], m["review_rounds"], m["loi_lot_review"], m["loi_linh_phat_hien"],
           m["self_code"], None]
    cells = [_cell(x).replace("|", "/") for x in row]
    cells[-1] = tok
    lines = open(REPORT, encoding="utf-8").read().split("\n")
    last = max((i for i, ln in enumerate(lines) if ln.startswith("|")), default=None)
    if last is None:
        return {"error": "metrics.md không có bảng"}
    lines.insert(last + 1, "| " + " | ".join(cells) + " |")  # chèn ngay sau dòng cuối của bảng, trước phần ghi chú
    tmp = REPORT + ".tmp"
    open(tmp, "w", encoding="utf-8").write("\n".join(lines))
    os.replace(tmp, REPORT)
    return {"ok": True}


def compute_all():
    out = []
    for p in sorted(glob.glob(os.path.join(PLANS, "*.md")), key=os.path.getmtime):
        pid = os.path.basename(p)[:-3]
        try:
            out.append(compute(pid))
        except Exception as ex:
            out.append({"id": pid, "error": str(ex)})
    return out


if __name__ == "__main__":
    try:
        print(json.dumps(compute(sys.argv[1]) if len(sys.argv) > 1 else compute_all(), ensure_ascii=False, indent=1))
    except Exception as ex:
        print(json.dumps({"error": str(ex)}, ensure_ascii=False))
