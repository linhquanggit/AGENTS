# Parse date server: các module "event connect" dùng DateTimeStyles.AdjustToUniversal
Scope: Assets/Project (Events, WorldBoss)  |  Evidence: UIBTIntro.cs:44-45, UIAirDefense.cs:178, UIMilestoneProgressionIntro.cs:76-77

⚠️ Project KHÔNG đồng nhất một style — mixed:
- **AdjustToUniversal**: các module event-connect (BattleTeam / AirDefense / MilestoneProgression) parse `start_time`/`end_time`/`current_time` server bằng `DateTime.Parse(str, null, DateTimeStyles.AdjustToUniversal)`. WorldBoss đã align về nhóm này (07-07/07-24; từng lệch `RoundtripKind`, đã đổi hết 7 chỗ trong WorldBoss + UISelectMode.cs:298).
- **RoundtripKind** vẫn tồn tại ở feature khác: UIEvent.cs:232, ItemFrame.cs:107, Sweep.cs:80 — KHÔNG sửa khi ngoài scope.

Chốt cho lần sau: thêm parse date **trong luồng WorldBoss / event-connect** → bám `AdjustToUniversal` cho khớp analogue gần nhất. Với chuỗi có `Z` (server WB xác nhận luôn kèm `Z`) thì 2 style cho cùng instant UTC → khác biệt chỉ là nhất quán code, không đổi hành vi. Không quét-đổi toàn project.
