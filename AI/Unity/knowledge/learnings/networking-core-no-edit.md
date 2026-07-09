# Networking Core (ApiResponse/HttpClient) không được sửa
Scope: Assets/CC_Manager/Networking/Core/  |  Evidence: ApiResponse.cs:43, HttpClient.cs:20

User không có quyền sửa `ApiResponse`/`HttpClient` (Core dùng chung toàn game). Khi HTTP non-2xx mà
server vẫn trả JSON business hợp lệ, không sửa Core để giữ `RawJson` — tự bóc JSON từ `ApiResponse.Error`
(chứa nguyên body sau prefix `"Protocol error (xxx): "`) bằng `JsonConvert.DeserializeObject` ngay trong
script gọi (mẫu: `UIWorldCupPredictionReward.ParseClaimResponse()`).
