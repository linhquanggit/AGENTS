# int.ShortToString() — helper chuẩn rút gọn K/M/B/T cho reward count
Scope: toàn dự án (UI hiển thị reward/currency)  |  Evidence: DPUtils.cs:1647 (định nghĩa, namespace DP.Utilities), ItemWorldCupPackReward.cs:18 (usage)

Family `ShortToString()` (overload cho int/Double/decimal/BigDouble tại DPUtils.cs) là helper chuẩn rút
gọn số dạng K/M/B/T khi hiển thị reward count trong UI — ưu tiên dùng thay vì `FormatString()` (chỉ thêm
dấu chấm hàng nghìn, không rút gọn) hay `ToReadableFormat<T>()` (cũng rút gọn K/M/B/T nhưng ít dùng hơn
trong codebase).
