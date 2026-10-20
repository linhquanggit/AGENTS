# Không gắn script lên prefab FX của artist; dùng holder + nested prefab

- **Vấn đề (WB2 P3 G6, 2026-10-20):** `Boss2.prefab` (artist) bị thay bản mới → mất `SectorMaskFxWB2` gắn trên prefab, mất maskInteraction/borderGap, thêm child `Wind`, và `Warning_G/R` để TẮT sẵn. Puzzle mất FX.
- **Quy tắc:** prefab FX của artist giữ sạch (0 script/override của gameplay). Trong prefab boss tạo GameObject **holder** (vd `G6-PuzzleFx`), kéo FX prefab vào làm **nested instance con**, gắn script điều khiển lên **holder** (ref tay hoặc tìm sâu theo tên trong con). Code bật holder, đặt world pos / rotation identity / bù lossyScale của cha, xong thì tắt + trả transform.
- **Script điều khiển tự đảm bảo trạng thái cần thiết** (SetActive root con, maskInteraction) trong Awake — không giả định artist để bật/tắt sẵn.
- Phần dùng riêng (vd `Wind`) tách ra prefab riêng, không để lẫn trong prefab dùng chung.
