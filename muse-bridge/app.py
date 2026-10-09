#!/usr/bin/env python3
"""Muse Bridge dạng app cửa sổ (pywebview / WKWebView). Đóng cửa sổ = thoát."""
import os
import sys
import threading
from http.server import ThreadingHTTPServer

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import server  # noqa: E402


def set_dock_icon():
    from AppKit import NSApplication, NSImage
    icon = os.path.join(HERE, "assets", "Musude.icns")
    if os.path.exists(icon):
        NSApplication.sharedApplication().setApplicationIconImage_(NSImage.alloc().initWithContentsOfFile_(icon))


def main():
    import webview

    server.load_config()
    if not os.path.exists(server.CONFIG_PATH):
        server.save_config()
    server.load_state()
    threading.Thread(target=server.poller, daemon=True).start()
    threading.Thread(target=server.relay_loop, daemon=True).start()
    server.migrate_old_archives()
    threading.Thread(target=server.trash_loop, daemon=True).start()
    threading.Thread(target=server.idle_watch_loop, daemon=True).start()
    srv = ThreadingHTTPServer(("127.0.0.1", 0), server.Handler)
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    url = "http://127.0.0.1:%d" % srv.server_address[1]
    print("Musude app:", url, "| lịch sử:", server.agent_dir(), flush=True)
    webview.create_window("Musude", url, width=1440, height=900, min_size=(900, 600))
    webview.start(set_dock_icon)
    srv.shutdown()


if __name__ == "__main__":
    main()
