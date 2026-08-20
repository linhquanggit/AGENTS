# Checklist P1 — Tạo 3 Addressable group cho WorldBoss (làm tay)

Addressables `1.19.19`. Mọi tên menu/nút dưới đây đã đối chiếu source package, đúng version này.
Số liệu nguồn: [worldboss-addressable-plan.md](worldboss-addressable-plan.md) §P1.

**Nguyên tắc xuyên suốt:** P1 **không giảm** build size. Sau P1 asset ship 2 lần (base build + bundle) — build tạm PHÌNH. Đúng như thiết kế, đừng sửa.

---

## Bước 0 — Chuẩn bị (1 lần)

- [ ] Backup / commit `Assets/AddressableAssetsData/` trước khi bắt đầu (P1 sửa thư mục này).
- [ ] Chạy `BuildReportTool` lấy **baseline build size** — không có baseline thì P2–P4 không chứng minh được gì.
- [ ] Mở `Window > Asset Management > Addressables > Groups`. Dock rộng ra để thấy đủ 3 cột: tên / **Path** / **Labels**.
- [ ] Xác nhận toolbar hiện `Profile: Default`.

> ⚠️ Đừng lấy `Default Local Group` làm mẫu "Local" — schema của nó **trỏ Remote**, tên gây nhầm. Mẫu đúng là `noel-prefab`.

---

## Bước 1 — Group `worldboss-prefab` (75 entry)

### 1.1 Tạo group
- [ ] Groups window → toolbar → dropdown **`New`** → chọn **`Packed Assets`**.
- [ ] Group mới tên `Packed Assets` xuất hiện → chọn nó → **F2** (hoặc click chậm 2 lần vào tên) → đổi thành `worldboss-prefab`.

> Chọn `Packed Assets` chứ **không** phải `Blank (no schema)`. Template này đã đúng sẵn 20 field (`Compression: LZ4`, `Bundle Mode: Pack Together`, …), chỉ trống 2 path.

### 1.2 Set path Remote
- [ ] Right-click group → **`Inspect Group Settings`** (hoặc chọn group, xem Inspector).
- [ ] Mục **`Content Packing & Loading`** → dropdown **`Build & Load Paths`** → chọn **`Remote`**.
- [ ] Đọc 2 dòng HelpBox ngay dưới, phải đúng:
```
Build Path: ServerData/Android
Load Path : https://onegamecdn.b-cdn.net/1941/14.9/Android
```
- [ ] Nếu dropdown hiện `<custom>` → chọn sai, làm lại. Nếu Load Path ra version khác `14.9` → **dừng**, hỏi người quản CDN trước.

### 1.3 Add asset (2 lần kéo, per-file)
- [ ] Project window → chọn folder `Assets/Project/WorldBoss/Addressable/Enemies/Prefabs`
- [ ] Gõ **`t:Object`** vào ô search của Project window (bắt buộc — để lấy cả file trong 4 subfolder `Bullets`/`Nodes`/`Paths`/`VFX`).
- [ ] Xác nhận scope search là folder đó (không phải `Assets` toàn bộ) → `Cmd+A` → phải chọn được **40 asset**.
- [ ] Kéo cả khối vào dòng group `worldboss-prefab`.
- [ ] Lặp lại với folder `Addressable/Effects` → `t:Object` → `Cmd+A` → **35 asset** → kéo vào cùng group.
- [ ] Group hiện tổng **75 entry**.

> 🛑 **KHÔNG kéo folder.** Kéo folder sinh *folder-entry*, lệch convention project (mọi group hiện có đều per-file, `campaign-texture` 402 entry file-level). Phải chọn **file** rồi kéo.

### 1.4 Gán label `WorldBoss` (đồng thời tạo label)
- [ ] Trong group, click entry đầu → `Shift+Click` entry cuối → chọn cả 75.
- [ ] Click vào cột **`Labels`** của một entry đang chọn → popup mở ra.
- [ ] Gõ `WorldBoss` vào ô search → popup hiện gợi ý **"Return to add 'WorldBoss'"** → nhấn **Return**.
- [ ] Label được tạo trong settings **và** gán cho cả 75 entry cùng lúc.
- [ ] Kiểm: cột `Labels` của mọi dòng đều hiện `WorldBoss`.

> Cách khác nếu muốn tạo label trước: mở popup Labels → click icon **Manage Labels** (góc trên phải popup) → window `Addressables Labels` → `+` → gõ tên → **Save**.

---

## Bước 2 — Group `worldboss-spine` (90 entry)

- [ ] `New` → `Packed Assets` → F2 → đổi tên `worldboss-spine`.
- [ ] Inspector → `Content Packing & Loading` → `Build & Load Paths` → **`Remote`** → xác nhận lại 2 dòng HelpBox y như 1.2.
- [ ] Project: folder `Addressable/Enemies/Textures/World_Boss_1` → `t:Object` → `Cmd+A` → **36 asset** → kéo vào group.
- [ ] Project: folder `Addressable/Enemies/Textures/World_Boss_3` → `t:Object` → `Cmd+A` → **54 asset** → kéo vào group.
- [ ] Tổng **90 entry**.
- [ ] Chọn cả 90 → cột `Labels` → tick `WorldBoss` (giờ label đã tồn tại, chỉ tick).

> Số 36/54 đã trừ `.DS_Store` (Unity không import nên bạn sẽ không thấy chúng). Nếu Unity hiện 37/55 thì bạn đang chọn sai folder.
> Group này gồm cả `.asset` (SkeletonData/Atlas), `.txt` (atlas), `.bytes` (skel), `.png`, `.mat` — `t:Object` bắt hết, đúng ý.

---

## Bước 3 — Group `worldboss-texture` (106 entry)

- [ ] `New` → `Packed Assets` → F2 → đổi tên `worldboss-texture`.
- [ ] Inspector → `Build & Load Paths` → **`Remote`** → xác nhận 2 dòng HelpBox.
- [ ] Project: folder `Addressable/UI/Textures/Use` → `t:Object` → `Cmd+A` → **30 asset** → kéo vào group.
- [ ] Project: folder `Addressable/UI/Textures/Atlas` → `t:Object` → `Cmd+A` → **76 asset** → kéo vào group.
- [ ] Tổng **106 entry**.
- [ ] Chọn cả 106 → cột `Labels` → tick `WorldBoss`.

> ⚠️ Chỉ 2 subfolder `Use` + `Atlas`. **KHÔNG** lấy 14 ảnh `demo_*`/`infor_box.png` ở `UI/Textures/` gốc — đang giữ để test, dọn ở release.
> `UI/Prefabs/` (19 file) **cố ý không vào group nào** — 8 cell prefab EnhancedScroller (đồng bộ, đã chốt không làm) + 12 nested instance trong popup `Resources/` + `Popup-WB-GamePlay.prefab`.

---

## Bước 4 — Verify bằng shell (không cần mở Unity)

- [ ] Trong Unity: `Cmd+S` / `File > Save Project` để flush YAML, rồi chạy:

```bash
cd /Users/mobione/wkspaces/WorldBoss/Assets/AddressableAssetsData
for g in worldboss-prefab worldboss-spine worldboss-texture; do
  printf "%-18s entry=%-4s label=%-4s buildpath=%s\n" "$g" \
    "$(grep -c 'm_Address:' AssetGroups/$g.asset)" \
    "$(grep -c '^    - WorldBoss$' AssetGroups/$g.asset)" \
    "$(grep -A1 m_BuildPath AssetGroups/Schemas/${g}_BundledAssetGroupSchema.asset | tail -1 | awk '{print $2}')"
done
```
Kỳ vọng **chính xác**:
```
worldboss-prefab   entry=75   label=75   buildpath=ec634c1d2b34bea4b9acc0ccae136b78
worldboss-spine    entry=90   label=90   buildpath=ec634c1d2b34bea4b9acc0ccae136b78
worldboss-texture  entry=106  label=106  buildpath=ec634c1d2b34bea4b9acc0ccae136b78
```
`entry` ≠ `label` ⇒ sót label, quay lại tick. `buildpath` khác ⇒ set sai path.

- [ ] Schema phải giống `noel-prefab` tuyệt đối (chỉ khác `m_Name`/`m_Group`):
```bash
cd /Users/mobione/wkspaces/WorldBoss/Assets/AddressableAssetsData/AssetGroups/Schemas
for g in worldboss-prefab worldboss-spine worldboss-texture; do
  diff -q <(grep -v 'm_Name:\|m_Group:' noel-prefab_BundledAssetGroupSchema.asset) \
          <(grep -v 'm_Name:\|m_Group:' ${g}_BundledAssetGroupSchema.asset) >/dev/null \
    && echo "$g SCHEMA OK" || echo "$g SCHEMA LECH"
done
```
- [ ] Label table có đúng 9 label:
```bash
cd /Users/mobione/wkspaces/WorldBoss/Assets/AddressableAssetsData
grep -A12 m_LabelTable AddressableAssetSettings.asset | grep -c '^    - '   # ky vong: 9
```
- [ ] Không có folder-entry (address phải luôn có đuôi file):
```bash
cd /Users/mobione/wkspaces/WorldBoss/Assets/AddressableAssetsData/AssetGroups
grep -h 'm_Address:' worldboss-*.asset | grep -v '\.[a-zA-Z0-9]\+$'   # ky vong: khong in gi
```

---

## Bước 5 — Analyze duplicate

- [ ] `Window > Asset Management > Addressables > Analyze` → tick **`Check Duplicate Bundle Dependencies`** → **Analyze Selected Rules**.
- [ ] Có dup → **Fix Selected Rules** (Addressables gom dependency dùng chung vào 1 group). Nếu nó tạo group mới, gom thủ công vào group `asset-duplication` đã có sẵn cho khớp convention.
- [ ] Ghi lại số dup tìm được (dùng đối chiếu sau P2–P4).

> Analyze **chỉ** bắt dup bundle↔bundle. Dup base-build↔bundle (loại nguy hiểm hơn, chính là trạng thái sau P1) nó **không** thấy — đó là việc của `Tools > Addressables > Check textures duplicated in APK`.

---

## Bước 6 — Build bundle

- [ ] Chọn đúng platform trong `Build Settings` (Android hoặc iOS).
- [ ] `Window > Asset Management > Addressables > Groups` → toolbar `Build` → **`New Build > Default Build Script`**.
- [ ] Console không error.
- [ ] Kiểm bundle sinh ra:
```bash
ls -la /Users/mobione/wkspaces/WorldBoss/ServerData/Android/ | grep -i worldboss
```
Phải thấy 3 bundle `worldboss-prefab_*`, `worldboss-spine_*`, `worldboss-texture_*` (+ hash).

> ⚠️ `m_BuildAddressablesWithPlayerBuild: 2` = **DoNotBuildWithPlayer** ⇒ bundle **không** tự build khi build app. Bước này phải làm tay trước **mỗi** lần build player, không chỉ lần này.

---

## Bước 7 — Smoke test (P1 không được đổi hành vi)

- [ ] Mở scene `WorldBoss.unity`, Play → vào 1 trận.
- [ ] 2 boss × 3 phase spawn đúng, không NullRef. (Hard ref còn nguyên nên **không cần mạng** ở bước này.)
- [ ] Mở đủ 8 popup WB (PopupId 200–207) → không ô trắng/tím.
- [ ] Nếu có gì sai ở bước 7 thì **không** phải do P1 — P1 chỉ thêm entry, không cắt ref nào.

---

## Nếu cần revert

Xoá 3 group trong Groups window (right-click → **`Remove Group(s)`**) → Unity tự dọn `m_GroupAssets` + 6 schema asset. Label `WorldBoss` xoá riêng qua **Manage Labels**. Không file gameplay nào bị đụng.

---

## Xong P1 rồi thì sao

P2 là bước đầu tiên **thực sự** giảm build size (~32 MB): 6 Node prefab → `useAddressable: 1` + `enemyReference`, thêm `CheckAndDownload("WorldBoss", …)` vào `UIWorldBoss._OnPlayClick`, `ReleaseAllHandle()` ở `LevelWorldBossManager.cs:92`. Logic đã có sẵn ở `Node.cs:75-96` — không sửa code gameplay.
