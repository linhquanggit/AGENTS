# Merge main → /main/WorldBoss: những thứ cần wire lại (2026-10-28)

Base merge lần trước: cs:9003 → 9007. Main mới: 9020, 9030, 9032. Workspace: cs:9010. Chưa merge gì cả.

## 3 file conflict
| File | Main đổi (9003→9032) | Nhánh WorldBoss đổi | Cách resolve |
|---|---|---|---|
| `Scripts/Manager.cs` | thêm `BattleTeams.EnsureRegistered();` (1 dòng, ~L2337) | local chỉ khác line-ending/dòng trống | Lấy bản main, giữ nguyên line-ending của main |
| `Scripts/Player.cs` | thêm `BattleTeams.EnsureRegistered();` ×2 (~L751, ~L863) | thêm `WorldBossProcess` (2 chỗ init `new WorldBoss.Process()`, property + setter + field trong data class) | Giữ cả hai, không mất field `WorldBossProcess` (mất = mất save data WB) |
| `BaseShot/DirectionShotE.cs` | enum `BossShotPattern`, field `bossShotPattern/bossPatternAngle/bossPatternSteps`, override `Init`, `GetBossPatternOffset`, xoay bullet theo offset | `linkedRotation`, `onBeforeSpawn`, `spawnPositionOverride`, `sweep` (SweepAimShotWB2), `ExpectedBullets`, `UseSweep`, `IE_SweepSpawn`, `OnAnimPerWave` | Trộn tay: giữ đủ 2 bộ field (Unity serialize theo TÊN field). SpawnBullet: dùng `_spawnPos` (spawnPositionOverride) VÀ `offsetRotation * fireDir.Rotation/Direction` (main). Gọi thêm `patternDirectionIndex = j` trước SpawnBullet ở cả vòng thường lẫn `IE_SweepSpawn` nếu muốn pattern chạy cả nhánh sweep |

Rủi ro nếu chọn "lấy bản main" cho DirectionShotE: field `linkedRotation` / `sweep` / `spawnPositionOverride` mất → ref serialized bị Unity xoá âm thầm.
Ref đang wire thật: `EnemyWB2_P2_A.prefab` có 2 `linkedRotation` ≠ 0 (L290, L3888). `sweep` / `spawnPositionOverride` hiện toàn `{fileID: 0}` (WB2_P3_A, Bullet_WB6_P3_G7, MakeAll) → mất cũng không hỏng gì ngay, nhưng vẫn nên giữ.

## Cần re-wire / check sau merge
1. Mở Unity, đợi compile, `read_console`: lỗi `BattleTeams` (nếu main thêm class mới mà thiếu .meta/asmdef) và lỗi DirectionShotE.
2. `EnemyWB2_P2_A.prefab`: xác nhận 2 `linkedRotation` còn trỏ đúng (RotationAimFighter).
3. WB6 (MakeAll.unity: `EnemyWB6_P1_A/P2_A/P3_A` dưới WB-Shooter): main KHÔNG đụng MakeAll, nhưng MakeAll của bạn đang checked-out + changed → checkin/shelve TRƯỚC khi merge. Class WB6 mới (PolygonLaserShot, PylonLinkShot, RingPulseSpiralShot, RotatingBurstShot, RandomExclusiveSequenceShot, BulletDelayedCurveToTarget/GrowOverTime/StopShakeBurst) không phụ thuộc Manager/Player; chỉ comment nhắc DirectionShotE → không cần sửa code.
4. Main sửa `Enemy_Boss6.prefab`, `EnemyBoss12.prefab`, `Boss9_WP1.prefab`: boss khác, không liên quan WB6 (Boss6 ≠ World_Boss_6), chỉ cần chấp nhận bản main.
5. Main sửa Events.asset, BattlePassMissions.asset, UIHome.prefab, PVP*, Addressable settings: nhận bản main; Addressable: sau merge mở Addressables Groups kiểm WB6 (`World_Boss_6` Spine + `EnemyWB6_P*_A`) vẫn còn nếu đã gán.
6. `Packages/packages-lock.json`: cả hai bên đổi → resolve bằng bản main rồi để Unity tự regenerate.
7. Pending local KHÔNG nên đi vào checkin merge: `Assets/FR2_Cache.asset`, `GoogleMobileAds/.../768x1024.prefab`, `ProjectSettings/EditorSettings.asset`, `mono_crash.mem.*.blob`, `Packages/com.coplaydev.unity-mcp` (private), `.claude/settings.local.json`.
8. Khác: `World_Boss_6/*.atlas` bị Removed locally (đã thay bằng `.atlas.txt`) → đúng ý thì giữ, đừng undo.

## Trước khi merge (Plastic)
`cm merge` từ chối khi còn pending changes. Checkin phần WB6 (script/prefab/bullet/Spine/Path + MakeAll nếu đồng ý lưu scene) hoặc shelve, rồi merge từ main.

Đã dọn: Không tạo file tạm trong project (file fetch nằm ở scratchpad phiên).
