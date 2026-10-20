# QLTools ListView — custom view list/array kiểu Odin (tự code, KHÔNG dùng Odin)
goal: Thêm module QLTools.ListView: attribute [QLList(...)] + PropertyDrawer vẽ List<T>/T[] giống Odin ListDrawer (header gập/mở, đếm phần tử, paging, kéo thả sắp xếp, nút +/− từng dòng, label theo field, search). Tuyệt đối không reference/using Sirenix.
project: ~/wkspaces/WB
source: Project 1941 — dò trực tiếp Assets/QL_Tools (không có doc knowledge về chủ đề này)

## T1 — Tạo asmdef + runtime attribute QLList
owner: muse
depends: -
context:
- Theo pattern module TextEffects: Runtime asmdef autoReferenced + Editor asmdef includePlatforms ["Editor"].
- Attribute API (runtime, không phụ thuộc UnityEditor):
  [QLList(ShowPaging=true, NumberOfItemsPerPage=10, ShowIndexLabels=true, ListElementLabelName="", DraggableItems=true, HideAddButton=false, HideRemoveButton=false, IsReadOnly=false, Expanded=false, ShowItemCount=true, Searchable=false, CustomAddFunction="", OnTitleBarGUI="")]
  — đặt tên property giống ListDrawerSettings của Odin để dễ thay thế.
- Cấm: using Sirenix, [ListDrawerSettings], bất kỳ dll Odin.
verified:
- Unity 2021.3.45f2 (ProjectSettings/ProjectVersion.txt:1) → PropertyDrawer cho List áp attribute lên từng phần tử; cần dùng cách wrap (xem T2).
- Assets/QL_Tools/TextEffects/Runtime/TextEffects.asmdef: name "QLTools.TextEffects", rootNamespace, autoReferenced true — mẫu để copy.
- Assets/QL_Tools/TextEffects/Editor/TextEffects.Editor.asmdef: includePlatforms ["Editor"], reference runtime asmdef.
- Không có file nào trong Assets dùng "Sirenix" (grep -rl rỗng).
- Không có PropertyAttribute nào sẵn trong Assets/QL_Tools.
refs: Assets/QL_Tools/TextEffects/Runtime/TextEffects.asmdef
files: Assets/QL_Tools/ListView/Runtime/QLTools.ListView.asmdef, Assets/QL_Tools/ListView/Runtime/QLListAttribute.cs
done_when: compile 0 error; namespace QLTools.ListView; không có chuỗi "Sirenix" trong folder.

## T2 — Core drawer: QLListDrawer (ReorderableList-based)
owner: muse
depends: T1
context:
- Unity 2021 gọi PropertyDrawer theo phần tử, không theo cả list ⇒ hai đường:
  (a) Editor tự vẽ: QLListInspector (CustomEditor(typeof(MonoBehaviour/ScriptableObject), true, isFallback=true)) duyệt SerializedProperty, gặp field có [QLList] và isArray (không phải string) thì vẽ bằng QLListDrawer; field khác DrawDefault.
  (b) Hỗ trợ thêm wrapper class QLList<T> (Serializable, field `List<T> items`) có PropertyDrawer riêng — dùng khi class đã có CustomEditor.
- QLListDrawer cache ReorderableList theo key (serializedObject target InstanceID + propertyPath).
- Tính năng: header foldout + "N items" bên phải + nút "+" trên header; paging (◀ trang x/y ▶) khi ShowPaging & count > NumberOfItemsPerPage; mỗi dòng: handle kéo (DraggableItems), label "Element i" hoặc giá trị field ListElementLabelName, nút "×" bên phải; phần tử là class/struct thì foldout vẽ children (EditorGUI.PropertyField includeChildren, GetPropertyHeight); IsReadOnly → GUI.enabled=false; Searchable → ô search lọc theo displayName/stringValue/objectReference name; CustomAddFunction/OnTitleBarGUI gọi bằng reflection trên target.
- Undo/Prefab override: chỉ thao tác qua SerializedProperty (InsertArrayElementAtIndex/DeleteArrayElementAtIndex/MoveArrayElement) + ApplyModifiedProperties; hỗ trợ multi-object editing ở mức hiển thị "—".
verified:
- Assets/QL_Tools/QLToolsWindow.cs:4-6 class bị comment hoàn toàn → không xung đột tên.
- Không có CustomPropertyDrawer/ReorderableList nào trong Assets/QL_Tools (grep rỗng) → không có CustomEditor fallback cũ trong QL_Tools.
files: Assets/QL_Tools/ListView/Editor/QLTools.ListView.Editor.asmdef, QLListDrawer.cs, QLListInspector.cs, QLListWrapperDrawer.cs, Runtime/QLList.cs (wrapper)
done_when: compile 0 error; list int, string, GameObject, class Serializable, struct đều vẽ được; kéo thả & thêm/xoá ghi Undo (Ctrl+Z hoạt động); prefab override hiển thị đậm.

## T3 — Style giống Odin
owner: muse
depends: T2
context:
- Header nền tối hơn (EditorStyles.toolbar hoặc box màu EditorGUIUtility.isProSkin ? #2D2D2D : #C8C8C8), dòng xen kẽ màu nhạt (zebra), viền 1px, padding 2px; index label cột trái rộng 24px mờ; nút "×" nhỏ 16px; khi rỗng hiện "List is empty" in nghiêng.
- Kéo thả Object từ Project vào header → thêm phần tử nếu khớp type (DragAndDrop).
files: Assets/QL_Tools/ListView/Editor/QLListStyles.cs, QLListDrawer.cs
done_when: so sánh mắt với screenshot Odin ListDrawer: header/count/paging/zebra/nút xoá tương đương; light & dark skin đều đọc được.

## T4 — Scene/asset demo + EditMode test
owner: claude
depends: T3
context:
- Tạo ScriptableObject demo QLListDemo (int[], List<string>, List<GameObject>, List<DemoItem{string name; int hp; Sprite icon}> với ListElementLabelName="name", list 50 phần tử paging 10, list IsReadOnly).
- EditMode test: tạo SerializedObject của demo, gọi API thêm/xoá/move của QLListDrawer (tách static helpers), assert arraySize & thứ tự.
files: Assets/QL_Tools/ListView/Demo/QLListDemo.cs, QLListDemo.asset, Assets/QL_Tools/ListView/Tests/Editor/QLListTests.cs (+ asmdef test)
done_when: test pass qua run_tests; mở QLListDemo.asset trong Inspector thấy đủ tính năng; read_console 0 error/warning mới.

## T5 — README ngắn + migrate-guide từ Odin
owner: any
depends: T4
context:
- Bảng map: Odin [ListDrawerSettings(...)] → [QLList(...)]; giới hạn (không có nested inline editor, không có TableList).
files: Assets/QL_Tools/ListView/README.md
done_when: README có ví dụ code + bảng map thuộc tính.

## Câu hỏi mở
- Có muốn QLListInspector làm fallback cho TẤT CẢ MonoBehaviour (ảnh hưởng mọi inspector không có CustomEditor) hay chỉ khi class có [QLList] / kế thừa marker? Mặc định đề xuất: fallback toàn cục nhưng chỉ đổi cách vẽ field có [QLList].
- Có cần TableList (vẽ dạng bảng cột) như Odin [TableList] không — hiện để ngoài phạm vi.
- Project có OneGame.OdinRemovalAudit.Editor.csproj — có quy ước gì từ audit gỡ Odin cần tuân theo không?
