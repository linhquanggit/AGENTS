# Learnings Index (Unity)

Grep this file by task keywords; open only matching entries. Big multi-section captures → [snapshots/INDEX.md](../snapshots/INDEX.md).
Format: `- [title](slug.md) — hook ≤120 chars (scope)` · newest first.

- [WB6: prefab _A + Node test từ scene MakeAll](worldboss-prefab-node-from-scene.md) — ref scene Fighter → null, null-safe LauncherShotE:63; Node test enemyPrefab trực tiếp, Test-WB index 3–5 (WB6)

- [StepRotation quét nòng ±N° (WB2 P3 G5)](worldboss-steprotation-sweep.md) — ueReachMin/stopAtInitAfterMax/initLocal; rotate=None; waitFireRate cần Init; test qua Manager.LoadTestScene
- [FX artist: holder + nested prefab](fx-prefab-artist-holder-nested.md) — không gắn script lên prefab FX artist; holder con + script tự bật Warning/mask (WB2 G6)
- [ENEMY_TYPE WB đổi số quên Enemies.asset + prefab](worldboss-enemy-type-renumber-data-loss.md) — đổi số enum enemy phải grep Enemies.asset `Type:` + prefab `type:` (Enemies/WorldBoss)
- [Sync StarDataInit PROMOTED: giữ StarLevelUpgradeFactor](stardatainit-promoted-sync-dataconvert-to-data.md) — DataConvert zero field này, chỉ copy các field base sang Data (Fighters/Wingmans)
- [Abyss timeout đua với die/boss-die](abyss-timeout-race-die-boss.md) — die ưu tiên; 4 fix freeze/AllEnemiesCleared/pendingWinCheck/IsLevelRepairBlocked (LevelAbyssManager)
- [BossLevelData: BaseDamage(level) × Multiplier(phase)](worldboss-bosslevel-basedamage-multiplier.md) — GetPhaseDamageDeal; milestone#1=0 phải fallback (WorldBossSO.cs)
- [UbhBulletStopAndFall](ubh-bullet-stopandfall.md) — move → freeze → rơi thẳng; rotation set 1 lần lúc chuyển fall (Bullets)
- [UbhBullet: không gắn Rigidbody2D Dynamic/Gravity](ubh-bullet-no-rigidbody2d-dynamic.md) — đấu với script set position → quỹ đạo cong; chỉ cần Collider2D trigger (bullet prefabs)
- [Bullet subclass return sớm trong UpdateMove kẹt pool](ubh-bullet-custom-updatemove-skip-onupdatedestroy.md) — bỏ base.UpdateMove() → không release, Shot() sau no-op (UbhBullet)
- [`event_index` ≠ season id](event-index-not-season-id.md) — id boss rotate trong list đã release; detect mùa mới bằng `start_time` (Events/WorldBoss)
- [Parse date server: AdjustToUniversal](date-parse-adjusttouniversal-convention.md) — event-connect dùng AdjustToUniversal; chỗ khác MIXED, không quét-đổi toàn project (Events)
- [JsonUtility: base DTO chiếm field → parse qua view](jsonutility-parse-view-pattern.md) — không đa hình/không shadow; mẫu LeaderboardView (DTO/JSON)
- [WorldBoss: 2 biến damage = 2 hợp đồng server](worldboss-damage-two-vars.md) — UserTotal = leaderboard score; UserReal = total_damage_all; không merge (WorldBoss)
- [Popup leaderboard root ở EventLeaderboardEntry](leaderboard-popup-event-entry-root.md) — module mới kế thừa entry; EventConnectResponse.cs cấm sửa (Leaderboard/Events)
- [LeaderboardPopupBase.OnSuccess sort theo score int](leaderboard-popup-score-sort-trap.md) — rank bằng giá trị khác (BigDouble) phải override OnSuccess (LeaderboardPopupBase.cs)
- [Moveset lao ra ngoài màn rồi về thẳng](charge-return-moveset-pattern.md) — lùi OutQuad → Linear ra ngoài CameraBouns → về thẳng; tắt collider ngoài rìa (Enemies moveset)
- [QLMonitor — hub config editor tool](ql-monitor-editor-config-hub.md) — tool expose `public static bool Enabled` + 1 dòng DrawToggleRow (QL_Tools/Editor)
- [Menu.SetChecked → CS0117](unity-menu-setchecked-cs0117.md) — không resolve ở 2021.3.45f2; vẽ toggle trong EditorWindow (Editor scripts)
- [Editor SIGBUS khi recompile/Play = UDP socket VS integration](vs-integration-udp-socket-domain-reload-crash.md) — fix VsMessagerReloadGuard.cs (beforeAssemblyReload) (Editor crash)
- [Module prefs-riêng → Player.Data](prefs-module-to-player-data.md) — cached process + EnsureProcess + migrate-then-DeleteKey, mẫu LuckyWheel (save system)
- [int.ShortToString() rút gọn K/M/B/T](reward-count-short-format.md) — dùng thay FormatString()/ToReadableFormat() (DP.Utilities)
- [WorldCup gold = hệ số nhân GoldByChapter](worldcup-gold-by-chapter.md) — Count trong WorldCup.asset là multiplier qua EventGold (Events/WorldCup)
- [Networking Core không được sửa](networking-core-no-edit.md) — ApiResponse/HttpClient; bóc JSON lỗi qua Error string (Networking/Core)
