# WorldCup gold reward = hệ số nhân (GoldByChapter), không phải số tuyệt đối
Scope: Assets/Project/Events/WorldCup/  |  Evidence: WorldCup.cs (EventGold), GoldByChapterSO.cs (WORLD_CUP_PREDICT), GoldByChapter.asset ID:43

Giá trị GOLD trong `WorldCup.asset` (Correct/Wrong.BaseReward, pack Contents) là hệ số nhân, quy đổi qua
`WorldCup.EventGold(count)` → `GoldByChapter.GetGoldByChapterWithMultiply(WORLD_CUP_PREDICT, count)`
(giống pattern `GridEventBoard.EventGold`). Luôn dùng `WorldCup.GetResultRewards()`/`GetPackRewards()`
đã resolve, không đọc thẳng asset.
