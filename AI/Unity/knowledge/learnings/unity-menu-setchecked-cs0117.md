# Menu.SetChecked → CS0117 (không dùng để tick menu item)
Scope: Editor scripts (Unity 2021.3.45f2 + Roslyn C# LSP)  |  Evidence: QLMonitor.cs thay thế cho checkmark menu

`UnityEditor.Menu.SetChecked(string, bool)` KHÔNG resolve trong project này (lỗi `CS0117: 'Menu' does not
contain a definition for 'SetChecked'`), dù docs có ghi. Đừng dùng nó để hiện dấu tick trên MenuItem. Muốn
trạng thái on/off có visual → vẽ toggle riêng trong EditorWindow (xem [QLMonitor](ql-monitor-editor-config-hub.md)),
hoặc chỉ log trạng thái ra Console.
