# QLMonitor — hub config cho mọi editor tool trong QL_Tools
Scope: Assets/QL_Tools/Editor  |  Evidence: QLMonitor.cs (window + QLToggle), AutoIncrementName.cs:8-12 (public static bool Enabled)

Mọi editor-tool toggle-able gom về window `QLMonitor` (menu Tools → OG → QL Monitor). Quy ước: mỗi tool
expose `public static bool Enabled { get => EditorPrefs.GetBool(KEY, true); set => ...; }` (EditorPrefs là
source of truth, giữ hook `[InitializeOnLoad]` và UI đồng bộ, không cần biến trung gian). Thêm tool mới =
1 dòng trong `QLMonitor.OnGUI`: `Tool.Enabled = DrawToggleRow(title, desc, Tool.Enabled)`. Toggle vẽ bằng
`QLToggle.Draw` (iOS-style, animate qua Mathf.MoveTowards + Repaint; dùng EditorApplication.timeSinceStartup
cho delta-time). `ToggleSwitch.cs`/`QLToolsWindow.cs` cũ trong QL_Tools đã comment hết — dùng QLMonitor thay thế.
