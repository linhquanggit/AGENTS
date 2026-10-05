# Snapshots Index (Unity)

Large captures (plans, merge caches, catalogs). Open only when the task touches that system. Format như learnings.

- [WorldBoss merge main lần 3 — 54 conflict + cách resolve](worldboss-merge-3-conflicts.md) — bẫy IAP 157-161 trùng, WB_TICKET chen trước CAMPAIGN_*; bỏ CRLF còn 21 conflict thật (2026-09-28)
- [WorldBoss điểm chạm file dùng chung + checklist merge](worldboss-merge-touchpoints.md) — ~28 file; Odin còn trong branch; AddressableAssetSettings mất group worldboss-* (2026-09-28)
- [WorldBoss cache 4 file enum + vị trí dùng](worldboss-premerge-enum-cache.md) — PopupId/IAP/REWARD_TYPE; nhớ quét cả `*.unity` (bảng giá idProductIAP trong Menu.unity)
- [WorldBoss conflict cache 2](worldboss-premerge-conflict-cache-2.md) — cache resolve lần merge trước
- [WorldBoss P1 checklist](worldboss-p1-checklist.md) — checklist triển khai P1
- [WorldBoss UI pre-merge (PopupBase)](worldboss-ui-premerge.md) — 7 popup PopupId 200–207, chuẩn bị convert sang hệ UI mới
- [WorldBoss Addressable audit + plan (CHỜ)](worldboss-addressable-plan.md) — thiếu cả 5 mắt xích; plan P0–P5 ~48MB; chưa chốt Remote/Local
- [Catalog RewardByProgression](reward-by-progression-catalog.md) — `BP_` = By Progress; config dùng chung toàn game
- [Shoot/Bullet catalog](shoot-templates/00-overview.md) — UniBulletHell vs BaseShotE; quét 01–05 trước khi làm đạn mới
- [EnhancedScroller architecture + recipe](enhancedscroller-architecture.md) — dùng base `LeaderboardScrollerBase<T,TCell>`
- [Addressables verify + bẫy double-wire](../addressables.md) — tích Addressable không loại asset khỏi build; Resources/ ship vô điều kiện
