#!/bin/bash
cd "$(dirname "$0")"
PORT=$(python3 -c "import json;print(json.load(open('config.json')).get('port',8765))" 2>/dev/null || echo 8765)
(sleep 1.5; open "http://127.0.0.1:$PORT") &
exec python3 server.py
