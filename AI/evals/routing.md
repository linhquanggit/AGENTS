# Evals: Intent Routing

Verifies that natural-language intent routes to the correct skill per [Workflows.md](../context/Workflows.md). Commands are optional shortcuts; plain language must route identically.

### EVAL-ROUTE-01: Bug report
- Intent: "Nút Play bấm không có phản ứng gì cả."
- Expected: Routes to `unity-investigate`.
- Pass: [ ] Selects `unity-investigate` [ ] Restates symptom vs. expected before searching.
- Must NOT: Jump to a fix without locating the entry point first.

### EVAL-ROUTE-02: New functionality
- Intent: "Thêm popup xác nhận thoát game."
- Expected: Routes to `unity-feature`.
- Pass: [ ] Selects `unity-feature` [ ] Looks for an existing `PopupBase` sibling to extend.
- Must NOT: Invent a new popup pattern from scratch.

### EVAL-ROUTE-03: Refactor
- Intent: "Đổi tên class GameManager cho gọn hơn."
- Expected: Routes to `unity-refactor`.
- Pass: [ ] Selects `unity-refactor` [ ] Maps callers/references before renaming.
- Must NOT: Rename before mapping dependencies.

### EVAL-ROUTE-04: Explanation
- Intent: "Giải thích luồng load màn chơi."
- Expected: Routes to `unity-explain`.
- Pass: [ ] Selects `unity-explain` [ ] Bounds scope to the load flow only.

### EVAL-ROUTE-05: Performance
- Intent: "Game bị tụt FPS khi spawn enemy."
- Expected: Routes to `unity-optimize`.
- Pass: [ ] Selects `unity-optimize` [ ] Asks for / requires bottleneck evidence before optimizing.

### EVAL-ROUTE-06: Review
- Intent: "Review giúp đoạn code tôi vừa sửa."
- Expected: Routes to `unity-review`.
- Pass: [ ] Selects `unity-review` [ ] Reviews only the changed scope.

### EVAL-ROUTE-07: Architecture advice
- Intent: "Nên tổ chức hệ thống inventory thế nào cho dễ mở rộng?"
- Expected: Routes to `unity-advisory`.
- Pass: [ ] Selects `unity-advisory` [ ] Advises (Why/How) without implementing.

### EVAL-ROUTE-08: Project health
- Intent: "Kiểm tra sức khỏe project / có asset nào hỏng không?"
- Expected: Routes to `unity-perception`.
- Pass: [ ] Selects `unity-perception` [ ] Scans filesystem/YAML, does not assume structure.

### EVAL-ROUTE-10: Record a learning
- Intent: "Lưu lại pattern này cho lần sau."
- Expected: Routes to `unity-learn`.
- Pass: [ ] Selects `unity-learn` [ ] Asks approval before writing [ ] Records with `file:line`.
- Must NOT: Write to `knowledge/` without approval.

### EVAL-ROUTE-09: Built-in command does not bypass runtime
- Intent: User runs a built-in like `/review`.
- Expected: Still enforces `Conventions.md` + `Rules.md` + skill selection.
- Pass: [ ] DPDebug/naming/no-comment rules still applied [ ] Token efficiency still applied.
- Must NOT: Treat the built-in as an escape hatch from the runtime.
