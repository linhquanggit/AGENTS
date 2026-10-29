#!/usr/bin/env python3
"""Muse Win — app cửa sổ để dùng muse-cli trên Windows (Cài đặt + đăng nhập + chat với Muse).

Chạy:  python musewin.py          (cửa sổ pywebview)
       python musewin.py --browser (mở bằng trình duyệt, để debug)
Chỉ dùng thư viện chuẩn + pywebview. Server chỉ nghe 127.0.0.1, cổng ngẫu nhiên.
"""
import json
import os
import re
import shutil
import subprocess
import sys
import threading
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

IS_WIN = os.name == "nt"
if getattr(sys, "frozen", False):
    RES = getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
else:
    RES = os.path.dirname(os.path.abspath(__file__))

APP_DIR = os.environ.get("MUSEWIN_HOME") or os.path.join(
    os.environ.get("APPDATA") or os.path.expanduser("~/.config"), "MuseWin")
CONFIG_PATH = os.path.join(APP_DIR, "config.json")
THREADS_PATH = os.path.join(APP_DIR, "threads.json")

DEFAULT_CONFIG = {
    "muse_cli": "",           # trống = tự dò
    "muse_cfg_dir": "",       # trống = tự dò thư mục cấu hình của muse-cli
    "thread_id": "",          # chat đang chọn
    "send_wait_seconds": 20,
    "poll_seconds": 5,
}
CFG = dict(DEFAULT_CONFIG)
LOCK = threading.RLock()
RUNTIME = {"status": None, "status_error": "", "busy": {}, "login": "", "last_error": ""}
WEBVIEW = {"mod": None}


# ------------------------------------------------------------------ config / threads
def load_json(path, default):
    try:
        with open(path, encoding="utf-8") as fh:
            return json.load(fh)
    except (OSError, json.JSONDecodeError):
        return default


def save_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2, ensure_ascii=False)
    os.replace(tmp, path)


def load_all():
    CFG.update(DEFAULT_CONFIG)
    CFG.update(load_json(CONFIG_PATH, {}))


def threads():
    d = load_json(THREADS_PATH, [])
    return d if isinstance(d, list) else []


def add_thread(tid, title):
    with LOCK:
        ts = [t for t in threads() if t.get("id") != tid]
        ts.insert(0, {"id": tid, "title": title or tid[:8], "created": time.strftime("%Y-%m-%dT%H:%M:%S")})
        save_json(THREADS_PATH, ts)
        CFG["thread_id"] = tid
        save_json(CONFIG_PATH, CFG)


# ------------------------------------------------------------------ thư mục local gắn với chat
SKIP_DIRS = {".git", ".svn", ".plastic", "node_modules", "Library", "Temp", "Logs", "obj", "Builds", "Build",
             ".vs", ".idea", "__pycache__", ".venv", "venv", "dist", "UserSettings", "MemoryCaptures"}
SECRET_NAMES = {".env", ".npmrc", ".pypirc", "id_rsa", "id_ed25519", "credentials", "auth.json", "cookies.json"}
SECRET_EXT = {".pem", ".key", ".pfx", ".p12", ".keystore", ".jks"}
BIN_EXT = {".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".psd", ".mp4", ".mov", ".webm", ".mp3", ".wav", ".ogg",
           ".zip", ".7z", ".rar", ".dll", ".exe", ".so", ".fbx", ".obj", ".asset", ".unity", ".prefab", ".meta",
           ".pdf", ".ttf", ".otf", ".bin", ".pyc", ".class", ".apk", ".aab"}
MAX_FILE = 200 * 1024
CHUNK = 24000  # giới hạn dòng lệnh Windows ~32k ký tự


def is_secret(name):
    n = name.lower()
    return n in SECRET_NAMES or n.startswith(".env") or os.path.splitext(n)[1] in SECRET_EXT


def thread_folder(tid):
    for t in threads():
        if t.get("id") == tid:
            return t.get("folder") or ""
    return ""


def set_thread_folder(tid, path):
    path = os.path.abspath(os.path.expanduser((path or "").strip().strip('"')))
    if path and not os.path.isdir(path):
        raise RuntimeError("không thấy thư mục: " + path)
    with LOCK:
        ts = threads()
        for t in ts:
            if t.get("id") == tid:
                t["folder"] = path
        save_json(THREADS_PATH, ts)
    return path


def folder_tree(root, depth=4, limit=600):
    out = []
    root = os.path.realpath(root)
    for cur, dirs, files in os.walk(root):
        rel = os.path.relpath(cur, root)
        lvl = 0 if rel == "." else rel.count(os.sep) + 1
        dirs[:] = sorted(d for d in dirs if d not in SKIP_DIRS and not d.startswith("."))
        if lvl >= depth:
            dirs[:] = []
        for f in sorted(files):
            fp = os.path.join(cur, f)
            ext = os.path.splitext(f)[1].lower()
            try:
                size = os.path.getsize(fp)
            except OSError:
                continue
            out.append({"p": os.path.relpath(fp, root).replace(os.sep, "/"), "size": size,
                        "ok": not is_secret(f) and ext not in BIN_EXT and size <= MAX_FILE})
            if len(out) >= limit:
                return out, True
    return out, False


def safe_path(root, rel):
    root = os.path.realpath(root)
    full = os.path.realpath(os.path.join(root, rel.replace("/", os.sep)))
    if full != root and not full.startswith(root + os.sep):
        raise RuntimeError("đường dẫn ra ngoài thư mục đã gắn")
    return full


def read_local(root, rel):
    full = safe_path(root, rel)
    name = os.path.basename(full)
    if not os.path.isfile(full):
        raise RuntimeError("không phải file: " + rel)
    if is_secret(name):
        raise RuntimeError("file nhạy cảm (khoá/cookie/.env), app không gửi: " + rel)
    if os.path.splitext(name)[1].lower() in BIN_EXT:
        raise RuntimeError("file nhị phân/ảnh/asset Unity, không gửi dạng chữ: " + rel)
    size = os.path.getsize(full)
    if size > MAX_FILE:
        raise RuntimeError("file quá lớn (%d KB > %d KB): %s" % (size // 1024, MAX_FILE // 1024, rel))
    with open(full, "rb") as fh:
        raw = fh.read()
    if b"\0" in raw[:4096]:
        raise RuntimeError("file nhị phân: " + rel)
    return raw.decode("utf-8", errors="replace")


# ------------------------------------------------------------------ muse-cli
def find_muse_cli():
    c = (CFG.get("muse_cli") or "").strip().strip('"')
    if c:
        return os.path.expanduser(c)
    hit = shutil.which("muse-cli")
    if hit:
        return hit
    home = os.path.expanduser("~")
    cands = [
        os.path.join(home, ".local", "bin", "muse-cli.exe"),
        os.path.join(home, ".local", "bin", "muse-cli"),
        os.path.join(os.environ.get("APPDATA", ""), "Python", "Scripts", "muse-cli.exe"),
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "Programs", "Python", "Scripts", "muse-cli.exe"),
    ]
    for p in cands:
        if p and os.path.isfile(p):
            return p
    return "muse-cli"


def run(argv, timeout=120):
    kw = {}
    if IS_WIN:
        kw["creationflags"] = 0x08000000  # CREATE_NO_WINDOW
    try:
        r = subprocess.run(argv, capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=timeout, **kw)
        return r.returncode, r.stdout, r.stderr
    except FileNotFoundError:
        return 127, "", "không tìm thấy muse-cli (%s) — cài bằng: uv tool install muse-cli==0.3.2" % argv[0]
    except subprocess.TimeoutExpired:
        return 124, "", "timeout sau %ss" % timeout


def muse(args, timeout=120):
    code, out, err = run([find_muse_cli()] + args, timeout=timeout)
    if code != 0:
        raise RuntimeError((err or out or "exit %s" % code).strip()[:800])
    try:
        return json.loads(out) if out.strip() else {}
    except json.JSONDecodeError:
        return {"raw": out.strip()}


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


def new_session(title):
    res = muse(["session-start", "--title", title], timeout=60)
    sid = find_key(res, "session_id") or find_key(res, "id")
    if not sid:
        raise RuntimeError("không đọc được session_id: " + json.dumps(res, ensure_ascii=False)[:300])
    return str(sid)


def history(tid, limit=40):
    msgs = muse(["history", "--thread", tid, "--limit", str(limit)], timeout=60)
    if not isinstance(msgs, list):
        msgs = []
    msgs.sort(key=lambda m: m.get("seq") or 0)
    return msgs


# ------------------------------------------------------------------ cookie hatch_sess
def cfg_dirs():
    """Các thư mục có thể chứa cấu hình/cookie của muse-cli (Mac, Linux, Windows)."""
    forced = (CFG.get("muse_cfg_dir") or "").strip().strip('"')
    if forced:
        return [os.path.expanduser(forced)]
    home = os.path.expanduser("~")
    cands = [
        os.path.join(home, ".config", "muse-cli"),
        os.path.join(os.environ.get("APPDATA", ""), "muse-cli"),
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "muse-cli"),
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "muse-cli", "muse-cli"),
        os.path.join(home, ".muse-cli"),
        os.path.join(home, ".muse"),
    ]
    seen, out = set(), []
    for p in cands:
        if p and p not in seen and os.path.isdir(p):
            seen.add(p)
            out.append(p)
    return out


def clean_cookie(value):
    value = (value or "").strip()
    if value.lower().startswith("hatch_sess="):
        value = value.split("=", 1)[1]
    value = value.split(";", 1)[0].strip().strip('"')
    if not value or re.search(r"\s", value):
        raise RuntimeError("giá trị cookie không hợp lệ")
    return value


def set_hatch_cookie(value):
    """Ghi đè hatch_sess trong các file của muse-cli (backup .bak trước)."""
    value = clean_cookie(value)
    esc = value.replace("\\", "\\\\")
    pats = [
        (r'("name"\s*:\s*"hatch_sess"[^{}]*?"value"\s*:\s*")[^"]*(")', r"\g<1>" + esc + r"\g<2>"),
        (r'("value"\s*:\s*")[^"]*("[^{}]*?"name"\s*:\s*"hatch_sess")', r"\g<1>" + esc + r"\g<2>"),
        (r'("hatch_sess"\s*:\s*")[^"]*(")', r"\g<1>" + esc + r"\g<2>"),
        (r"(hatch_sess=)[^;\s\"']+", r"\g<1>" + esc),
        (r"(\thatch_sess\t)[^\t\r\n]+", r"\g<1>" + esc),
    ]
    dirs = cfg_dirs()
    if not dirs:
        raise RuntimeError("không thấy thư mục cấu hình muse-cli — bấm \"Dò cấu hình\" hoặc điền đường dẫn ở Cài đặt")
    changed = []
    for base in dirs:
        for root, _d, files in os.walk(base):
            for fn in files:
                if fn.endswith(".bak"):
                    continue
                fp = os.path.join(root, fn)
                try:
                    txt = open(fp, encoding="utf-8").read()
                except (UnicodeDecodeError, OSError):
                    continue
                if "hatch_sess" not in txt:
                    continue
                new = txt
                for pat, rep in pats:
                    new = re.sub(pat, rep, new)
                if new != txt:
                    shutil.copy2(fp, fp + ".bak")
                    with open(fp, "w", encoding="utf-8") as f:
                        f.write(new)
                    changed.append(fp)
    if not changed:
        raise RuntimeError("không tìm thấy hatch_sess trong " + ", ".join(dirs)
                           + " — nếu muse-cli chưa từng đăng nhập, chạy `muse-cli auth export` một lần rồi thử lại")
    return changed


def diagnose():
    """Liệt kê tên file (không đọc giá trị cookie) để biết muse-cli lưu ở đâu."""
    info = {"muse_cli": find_muse_cli(), "found_cli": bool(shutil.which(find_muse_cli()) or os.path.isfile(find_muse_cli())),
            "app_dir": APP_DIR, "dirs": []}
    for base in cfg_dirs():
        files = []
        for root, _d, fs in os.walk(base):
            for fn in fs:
                fp = os.path.join(root, fn)
                has = False
                try:
                    has = "hatch_sess" in open(fp, encoding="utf-8").read()
                except (UnicodeDecodeError, OSError):
                    pass
                files.append({"path": os.path.relpath(fp, base), "has_hatch_sess": has})
        info["dirs"].append({"dir": base, "files": files[:60]})
    return info


def refresh_status():
    try:
        st = muse(["status"], timeout=60)
        with LOCK:
            RUNTIME["status"], RUNTIME["status_error"] = st, ""
    except Exception as e:  # noqa: BLE001
        with LOCK:
            RUNTIME["status"], RUNTIME["status_error"] = None, str(e)


# ------------------------------------------------------------------ đăng nhập bằng cửa sổ WebView2
def login_window():
    """Mở muse.ai trong cửa sổ riêng, thấy cookie hatch_sess thì lưu vào muse-cli rồi đóng."""
    webview = WEBVIEW["mod"]
    if webview is None:
        raise RuntimeError("đăng nhập tự động chỉ chạy trong app cửa sổ (không dùng với --browser) — hãy dán cookie tay")
    with LOCK:
        if RUNTIME["login"] == "running":
            return
        RUNTIME["login"] = "running"
    win = webview.create_window("Đăng nhập muse.ai", "https://muse.ai", width=1000, height=760)

    def watch():
        deadline = time.time() + 600
        got = None
        try:
            while time.time() < deadline:
                time.sleep(2)
                try:
                    jar = win.get_cookies() or []
                except Exception:  # noqa: BLE001
                    break  # cửa sổ đã đóng
                for c in jar:
                    for name, morsel in c.items():
                        if name == "hatch_sess" and morsel.value:
                            got = morsel.value
                if got:
                    break
            if got:
                set_hatch_cookie(got)
                refresh_status()
                try:
                    win.destroy()
                except Exception:  # noqa: BLE001
                    pass
                with LOCK:
                    RUNTIME["login"] = "ok" if RUNTIME["status"] else "saved_status_error"
            else:
                with LOCK:
                    RUNTIME["login"] = "closed"
        except Exception as e:  # noqa: BLE001
            with LOCK:
                RUNTIME["login"] = "error: " + str(e)

    threading.Thread(target=watch, daemon=True).start()


# ------------------------------------------------------------------ gửi tin
def send_bg(tid, text):
    key = uuid.uuid4().hex[:6]
    with LOCK:
        RUNTIME["busy"][key] = {"thread": tid, "since": time.time()}

    def work():
        try:
            wait = max(5, int(CFG.get("send_wait_seconds", 20)))
            parts = [text[i:i + CHUNK] for i in range(0, len(text), CHUNK)] or [text]
            for i, part in enumerate(parts, 1):
                if len(parts) > 1:
                    part = "[phần %d/%d — chưa trả lời, đợi đủ phần]\n%s" % (i, len(parts), part)
                muse(["send", part, "--thread", tid, "--wait", str(wait)], timeout=wait + 60)
            with LOCK:
                RUNTIME["last_error"] = ""
        except Exception as e:  # noqa: BLE001
            with LOCK:
                RUNTIME["last_error"] = str(e)
        finally:
            with LOCK:
                RUNTIME["busy"].pop(key, None)

    threading.Thread(target=work, daemon=True).start()


# ------------------------------------------------------------------ HTTP
class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, code, payload, ctype="application/json"):
        data = payload if isinstance(payload, bytes) else json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", ctype + ("; charset=utf-8" if ctype.startswith("text") else ""))
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def _host_ok(self):
        return (self.headers.get("Host") or "").split(":")[0] in ("127.0.0.1", "localhost")

    def do_GET(self):
        if not self._host_ok():
            return self._send(403, {"error": "host"})
        u = urlparse(self.path)
        q = parse_qs(u.query)
        if u.path in ("/", "/index.html"):
            with open(os.path.join(RES, "index.html"), "rb") as fh:
                return self._send(200, fh.read(), "text/html")
        if u.path == "/api/state":
            with LOCK:
                return self._send(200, {
                    "config": {k: CFG.get(k) for k in DEFAULT_CONFIG},
                    "threads": threads(),
                    "status": RUNTIME["status"], "status_error": RUNTIME["status_error"],
                    "busy": list(RUNTIME["busy"].values()), "login": RUNTIME["login"],
                    "last_error": RUNTIME["last_error"], "app_dir": APP_DIR,
                    "can_login_window": WEBVIEW["mod"] is not None,
                })
        if u.path == "/api/history":
            tid = (q.get("id") or [""])[0]
            if not re.fullmatch(r"[A-Za-z0-9-]{8,64}", tid):
                return self._send(400, {"error": "thread id lạ"})
            try:
                return self._send(200, {"messages": history(tid, int((q.get("limit") or ["40"])[0]))})
            except Exception as e:  # noqa: BLE001
                return self._send(200, {"messages": [], "error": str(e)})
        if u.path == "/api/diagnose":
            return self._send(200, diagnose())
        if u.path in ("/api/folder/tree", "/api/folder/read"):
            root = thread_folder((q.get("id") or [""])[0])
            if not root:
                return self._send(200, {"error": "chat này chưa gắn thư mục"})
            try:
                if u.path.endswith("tree"):
                    entries, cut = folder_tree(root, int((q.get("depth") or ["4"])[0]))
                    return self._send(200, {"root": root, "entries": entries, "truncated": cut})
                return self._send(200, {"text": read_local(root, (q.get("p") or [""])[0])})
            except Exception as e:  # noqa: BLE001
                return self._send(200, {"error": str(e)})
        return self._send(404, {"error": "not found"})

    def do_POST(self):
        if not self._host_ok() or self.headers.get("X-Bridge") != "1":
            return self._send(403, {"error": "forbidden"})
        try:
            n = int(self.headers.get("Content-Length") or 0)
            b = json.loads(self.rfile.read(n) or b"{}") if n else {}
            p = urlparse(self.path).path
            if p == "/api/status":
                refresh_status()
                return self._send(200, {"ok": True})
            if p == "/api/cookie":
                files = set_hatch_cookie(b.get("value", ""))
                refresh_status()
                with LOCK:
                    err = RUNTIME["status_error"]
                return self._send(200, {"ok": not err, "files": files, "error": err})
            if p == "/api/login":
                login_window()
                return self._send(200, {"ok": True})
            if p == "/api/chat/new":
                title = (b.get("title") or "").strip() or ("Muse Win · " + time.strftime("%d/%m %H:%M"))
                sid = new_session(title)
                add_thread(sid, title)
                return self._send(200, {"thread_id": sid})
            if p == "/api/chat/add":
                tid = (b.get("id") or "").strip()
                if not re.fullmatch(r"[A-Za-z0-9-]{8,64}", tid):
                    raise RuntimeError("thread ID không hợp lệ")
                add_thread(tid, (b.get("title") or "").strip() or tid[:8])
                return self._send(200, {"thread_id": tid})
            if p == "/api/chat/folder":
                return self._send(200, {"folder": set_thread_folder(b.get("id", ""), b.get("path", ""))})
            if p == "/api/chat/select":
                with LOCK:
                    CFG["thread_id"] = b.get("id", "")
                    save_json(CONFIG_PATH, CFG)
                return self._send(200, {"ok": True})
            if p == "/api/chat/remove":
                with LOCK:
                    ts = [t for t in threads() if t.get("id") != b.get("id")]
                    save_json(THREADS_PATH, ts)
                    if CFG.get("thread_id") == b.get("id"):
                        CFG["thread_id"] = ts[0]["id"] if ts else ""
                        save_json(CONFIG_PATH, CFG)
                return self._send(200, {"ok": True})
            if p == "/api/chat/rename":
                with LOCK:
                    ts = threads()
                    for t in ts:
                        if t.get("id") == b.get("id"):
                            t["title"] = (b.get("title") or "").strip() or t["title"]
                    save_json(THREADS_PATH, ts)
                return self._send(200, {"ok": True})
            if p == "/api/send":
                tid, text = b.get("id", ""), (b.get("text") or "").strip()
                if not re.fullmatch(r"[A-Za-z0-9-]{8,64}", tid) or not text:
                    raise RuntimeError("thiếu thread hoặc nội dung")
                send_bg(tid, text)
                return self._send(200, {"ok": True})
            if p == "/api/config":
                with LOCK:
                    for k, v in b.items():
                        if k in DEFAULT_CONFIG and k != "thread_id":
                            CFG[k] = v
                    save_json(CONFIG_PATH, CFG)
                return self._send(200, {"ok": True})
            return self._send(404, {"error": "not found"})
        except Exception as e:  # noqa: BLE001
            return self._send(200, {"error": str(e)})


def start_server():
    load_all()
    if not os.path.exists(CONFIG_PATH):
        save_json(CONFIG_PATH, CFG)
    srv = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    return srv, "http://127.0.0.1:%d" % srv.server_address[1]


def main():
    srv, url = start_server()
    if "--browser" in sys.argv:
        print("Muse Win:", url, "(Ctrl+C để dừng)")
        try:
            while True:
                time.sleep(3600)
        except KeyboardInterrupt:
            return
    import webview
    WEBVIEW["mod"] = webview
    icon = os.path.join(RES, "assets", "musewin.ico")
    webview.create_window("Muse Win", url, width=1200, height=800, min_size=(820, 560))
    webview.start(icon=icon if os.path.exists(icon) else None)
    srv.shutdown()


if __name__ == "__main__":
    main()
