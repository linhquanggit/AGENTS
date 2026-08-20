# Di chuyển module lưu prefs-riêng vào Player.Data
Scope: Assets/Project (save system)  |  Evidence: Assets/Project/UI/Scripts/Spins/LuckyWheel.cs:14-32, Assets/Project/Scripts/Player.cs:294-302

Pattern chuẩn (LuckyWheel làm mẫu, gốc từ WorldCup/BattleTeam): cached `static Process process` + `EnsureProcess()` đọc `Player.XxxProcess`; nếu null → migrate 1 lần từ key DPPlayerPrefs cũ rồi `DeleteKey` ngay (không xoá key cũ thì DeleteProgress sẽ resurrect data cũ); `SaveData()` route về setter `Player.XxxProcess`; thêm `Refresh() => EnsureProcess()` và gọi trong `Player.Refresh()` để ChangeData/cloud-load re-point. Logic reset ngày/chu kỳ phải chạy SAU khi chốt process (sau migrate).
