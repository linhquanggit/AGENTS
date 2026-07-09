# Learnings Index

Project-specific facts discovered during work — patterns, code locations, undocumented conventions, corrected behaviors. Read this index first; open only the entries relevant to the current task. Each entry is one tiny file under `learnings/`.

Entry line format: `- [title](slug.md) — hook (scope)`

<!-- entries below, one line each, newest first -->
- [int.ShortToString() - helper rút gọn K/M/B/T reward count](reward-count-short-format.md) — ưu tiên ShortToString() thay vì FormatString()/ToReadableFormat() (DP.Utilities)
- [WorldCup gold reward = hệ số nhân (GoldByChapter)](worldcup-gold-by-chapter.md) — Count trong WorldCup.asset là multiplier, quy đổi qua WorldCup.EventGold (Events/WorldCup)
- [Networking Core (ApiResponse/HttpClient) không được sửa](networking-core-no-edit.md) — bóc JSON lỗi qua Error string thay vì sửa Core (Networking/Core)
