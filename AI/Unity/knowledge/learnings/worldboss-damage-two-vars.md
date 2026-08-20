# WorldBoss: 2 biến damage = 2 hợp đồng server, không merge
Scope: Assets/Project/WorldBoss  |  Evidence: WorldBoss.cs:300-317, 1072-1073

`UserTotalDamageDeal` = mọi damage thật từ ngày 1 → score LEADERBOARD (server đọc field này để xếp hạng). `UserRealDamageDeal` = chỉ damage sau giai đoạn fake (tập con của Total) → server SUM toàn player = `total_damage_all` cho progress boss. Không merge 2 biến, không để server cộng cả 2 (đếm đôi). Damage truyền dạng string `BigDouble.ToString()` (`"123456789000000"` hoặc `"1.2085e23"`). Fake progress chỉ chi phối hiển thị (`DisplayedTotalDamageDeal`), không chi phối ranking.
