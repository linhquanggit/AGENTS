# Bẫy: LeaderboardPopupBase.OnSuccess sort theo score (int) và ghi đè rank
Scope: Assets/Project/Scripts/LeaderboardPopupBase.cs  |  Evidence: LeaderboardPopupBase.cs:64-68

`OnSuccess` luôn `OrderByDescending(x => x.score)` (int) rồi ghi đè `rank` server trả về. Module xếp hạng bằng giá trị ngoài `score` (WorldBoss: `total_damage` BigDouble ~1e23, tràn int) sẽ bị xáo trộn thứ hạng khi nối `DHS.GetLeaderboardEvent` thật → phải override `OnSuccess` sort theo giá trị riêng, hoặc server fill `score` tương ứng. Liên quan [worldboss-damage-two-vars](worldboss-damage-two-vars.md).
