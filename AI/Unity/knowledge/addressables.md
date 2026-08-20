# Knowledge: Addressables Verification

How to verify an Addressables setup without opening Unity. Use for `unity-perception` (health audit),
`unity-review` (a module claims to be addressable-ized), and `unity-optimize` (build size).

Core premise: **"marked Addressable" does not mean "out of the build."** Most real defects are a
silently-broken link in the chain below, not a broken build.

---

## 0. Resolve the setup first — never assume

```bash
grep -n "com.unity.addressables" Packages/manifest.json
ls Assets/AddressableAssetsData/AssetGroups/*.asset
grep -n -A12 "m_LabelTable" Assets/AddressableAssetsData/AddressableAssetSettings.asset
grep -n "m_ActivePlayerDataBuilderIndex\|m_BuildRemoteCatalog" Assets/AddressableAssetsData/AddressableAssetSettings.asset
```

Resolve the active data builder by GUID (index into `m_DataBuilders`, 0-based) against
`AddressableAssetsData/DataBuilders/*.meta`. `BuildScriptPackedMode` = production; anything else
(`FastMode`, `VirtualMode`) means the Editor is bypassing bundles and Play-Mode results prove nothing
about a real build.

Resolve profile path variables — `m_ProfileSettings.m_Values` maps an ID to a value, and
`m_ProfileEntryNames` maps that ID to a name (`Local.BuildPath`, `Remote.LoadPath`, …). Group schemas
reference those IDs, so classify groups by ID:

```bash
# substitute the Remote.BuildPath / Local.BuildPath IDs read from the settings file
grep -l "<REMOTE_BUILDPATH_ID>" Assets/AddressableAssetsData/AssetGroups/Schemas/*_BundledAssetGroupSchema.asset
grep -l "<LOCAL_BUILDPATH_ID>"  Assets/AddressableAssetsData/AssetGroups/Schemas/*_BundledAssetGroupSchema.asset
```

A **Local** group still ships inside the app — it buys lazy loading (RAM), not download size. Report
local vs remote explicitly; do not call a local group "moved out of the build."

---

## 1. The five-link chain

A module is addressable-ized only if **all five** hold. Any missing link degrades silently — no
compile error, no runtime error, the assets just ship in the base build.

| # | Link | How to verify |
|---|---|---|
| 1 | Group(s) exist | a `<module>-*.asset` in `AssetGroups/` |
| 2 | Assets are entries | asset GUID appears in an `AssetGroups/*.asset` |
| 3 | Label assigned | label in `m_LabelTable` **and** in the entry's `m_SerializedLabels` |
| 4 | Consumer uses an `AssetReference` | not a direct object reference — see §2 |
| 5 | Download + release wired | a download call before entering the feature, a release on exit |

Entries store `m_Address` as the full asset path, so a path grep over `AssetGroups/*.asset` is a valid
first pass. It is **not** conclusive — always confirm by GUID, read from the `.meta`:

```bash
g=$(grep -m1 '^guid:' "<asset>.meta" | cut -d' ' -f2)
grep -rl "$g" Assets/AddressableAssetsData/AssetGroups/ || echo "NOT IN ANY GROUP"
```

A folder named `Addressable/` is a naming convention with **zero** technical meaning. Never infer
link 2 from a folder name.

---

## 2. ⚠️ Double-wire — the most common and most expensive defect

**Symptom:** an asset is registered in a group *and* still hard-referenced from a serialized field.

**Mechanics:** marking an asset Addressable does not remove it from the player build. Unity includes
any asset reachable from (a) a scene in Build Settings, or (b) any `Resources/` folder. If a
serialized field still points at it directly, the asset ships in the base build **and** in the bundle
— paid for twice in download size, and loaded twice into memory at runtime.

Two sub-cases, and the difference matters:

| Case | Setup | Caught by Addressables *Analyze*? |
|---|---|---|
| **(a) Build ↔ bundle** | consumer is **not** addressable (lives in a build scene or `Resources/`); asset **is** | ❌ **No** |
| **(b) Bundle ↔ bundle** | consumer and asset are in **different** groups | ✅ Yes — *Check Duplicate Bundle Dependencies* |

Case (a) is the dangerous one: the Analyze window reports clean while the asset ships twice. It is
only findable by the filesystem checks below.

### 2.1 `AddressableMapSprite` double-wire (recurring in this codebase)

The component (`Assets/Project/Scripts/AddressableMapSprite.cs`) loads `spriteReference` in `OnEnable`
and assigns it to `mySpriteRender`. If `SpriteRenderer.m_Sprite` is *also* wired in the prefab, the
sprite is in the build **and** in the bundle.

Detect — resolve the script GUID from its `.meta` first, never hardcode it:

```bash
AMS=$(grep -m1 '^guid:' Assets/Project/Scripts/AddressableMapSprite.cs.meta | cut -d' ' -f2)

cat > /tmp/dw.awk <<EOF
/guid: $AMS/                            { has=1 }
/^  m_Sprite: \{fileID: [0-9]+, guid: / { match(\$0,/guid: [0-9a-f]{32}/); sr=substr(\$0,RSTART+6,32) }
/^    m_AssetGUID: [0-9a-f]{32}/        { match(\$0,/[0-9a-f]{32}/);       ar=substr(\$0,RSTART,32) }
END { if (has && sr!="" && ar!="") print (sr==ar ? "SAME" : "DIFF"), FILENAME }
EOF

find Assets -name "*.prefab" -not -path "*/Library/*" -print0 \
  | xargs -0 -n1 awk -f /tmp/dw.awk 2>/dev/null
```

- `SAME` — same sprite wired both ways. Pure duplication.
- `DIFF` — `m_Sprite` points at a *different* asset than `spriteReference`. Worse: a third asset ships,
  and the sprite visible in the Editor is not the one shown at runtime. Always inspect these by hand;
  the in-build sprite may be an intentional placeholder or a stale leftover.

**Fix:** clear the direct reference, leave `spriteReference` alone. The component already ships the
tool for it — the `[Button] RemoveSprite()` sets `sprite = null` and alpha to 0; `OnLoadAddressable`
restores `Color.white` once the bundle resolves. So the correct authoring order is: wire
`spriteReference` → press **RemoveSprite** → save prefab.

Before clearing, confirm the runtime path actually works for that prefab, or it renders invisible:
`mySpriteRender` is wired, the GameObject is active (load runs in `OnEnable`), and an
`AddressableManager` instance exists in the scene — `AddressableMapSprite` calls
`AddressableManager.instance.AddLoadedHandle(handle)` with no null guard.

Snapshot taken 2026-07-28 on WorldBoss: **140 prefabs** double-wired (130 `SAME`, 10 `DIFF`),
**130 unique sprites ≈ 12 MB** of source, concentrated in `Maps/Chapter17` (89),
`Events/Holiday/Noel/.../Level` (37), `Events/AirDefense/.../Map` (12). Re-run the detector rather
than trusting these numbers.

### 2.2 Generic double-wire sweep (any asset type)

The same trap applies to `Image.m_Sprite`, `MeshRenderer.m_Materials`, `AudioSource.m_audioClip`, and
every `[SerializeField]` object reference. For a suspect addressable asset:

```bash
g=<asset guid>
# hard references from anything reachable by the build
grep -rl "guid: $g" Assets --include="*.prefab" --include="*.unity" --include="*.asset" \
  | grep -v "AddressableAssetsData"
```

Then classify each hit: is it inside a `Resources/` folder, or reachable from a scene listed in
`ProjectSettings/EditorBuildSettings.asset`? If yes → case (a), the asset ships in the base build.

---

## 3. Build-inclusion rules (why link 4 is where audits fail)

- **`Resources/` is unconditional.** Every asset under any `Resources/` folder is packed into the
  player, addressable flag or not, referenced or not. A lazy `Resources.Load` loader reduces *RAM*,
  never *build size*.
- **A ScriptableObject in `Resources/` is a common hidden root.** It ships, so everything it
  serializes ships — prefabs, those prefabs' materials, those materials' textures. Follow the chain
  before concluding a module is out of the build.
- **Scenes in Build Settings** pull their whole reference graph. Check `enabled: 1` — a disabled row
  ships nothing.
- **An addressable asset referenced by a build-reachable asset ships twice.** This is §2 restated; it
  is the single rule most often missed.

---

## 4. Consumer-side patterns to check for link 4

Search the module's scripts for the loading API actually in use before judging:

```bash
grep -rn --include="*.cs" "Addressables\.\|AssetReference\|useAddressable" Assets/Project/<Module>/
```

- A declared `AssetReference*` field proves nothing — check the serialized value. An empty
  `m_AssetGUID:` in the `.asset`/`.prefab` is a dead field, and `RuntimeKeyIsValid()` returns false,
  so the load is skipped silently with no log.
- A boolean toggle (`useAddressable: 0/1` on this project's `Node`) selects between the direct field
  and the reference field. Verify the flag in the prefab YAML, not just the code.
- Grep the download trigger and the release. A module that loads bundles but never releases leaks
  until scene change.

---

## 5. Content-update readiness

If `m_BuildRemoteCatalog: 1`, the project intends to patch content without a store release. That
requires `addressables_content_state.bin` from the previous build:

```bash
ls Assets/AddressableAssetsData/Android Assets/AddressableAssetsData/iOS
grep -n "m_ContentStateBuildPath" Assets/AddressableAssetsData/AddressableAssetSettings.asset
```

Empty platform folders (with no override path) mean *Update a Previous Build* cannot run — every
release is a full rebuild and every player re-downloads every bundle. Report this as a finding, not
as a note.

---

## 6. Audit checklist

1. Resolve package version, active data builder, profile paths, label table (§0).
2. Per module: verify all five links (§1), by GUID, not by folder name.
3. Classify each group local vs remote; state it explicitly (§0).
4. **Run the double-wire sweep** (§2) — both the `AddressableMapSprite` detector and a generic pass
   over the heaviest entries. Remember Analyze cannot see case (a).
5. Trace `Resources/` roots and build-enabled scenes into the module's assets (§3).
6. Check content-state readiness (§5).
7. Report source-file sizes as **source**, never as build size — textures recompress. Point at
   `BuildReportTool` (present in this project) for real numbers.

## Guardrails

- **DO NOT** conclude "addressable-ized" from a group, a label, or a folder name alone. All five links.
- **DO NOT** hardcode a script or asset GUID — read it from the `.meta` every time.
- **DO NOT** report source MB as build MB.
- **DO NOT** mass-clear direct references found by the double-wire sweep. Verify the runtime load path
  per prefab first (§2.1); a wrong clear renders the object invisible with no error.
