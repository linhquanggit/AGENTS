# Skill: unity-perception

Gain a deep understanding of the project's health, assets, and dependencies using filesystem analysis.

Load `../knowledge/addressables.md` when the scope involves Addressables, bundles, or build size.

## Procedure
1. **Health Check (Filesystem)**:
   - **Meta Integrity**: Scan for missing `.meta` files or orphaned `.meta` files.
   - **Compile Status**: `~/AGENTS/muse-bridge/bin/unity-errors` (errors since last mark) or `unity-errors --last 50` (tail of Editor.log).
   - **Broken References**: Search for "guid: 00000000000000000000000000000000" in YAML files to find missing references.
2. **Asset Analysis**:
   - **Asset Density**: Count key asset types (Prefabs, Materials, Scripts) in the targeted feature folder to judge complexity.
   - **Script Usage**: Search for a Script's GUID inside `.unity`, `.prefab`, or `.asset` files to find all active instances in the project.
3. **Scene Perception (CLI-based)**:
   - **Hierarchy Scan**: If Scene files are YAML, scan for top-level GameObjects or specific components (e.g., Managers) to map the hierarchy.
   - **Layer/Tag Audit**: Identify all custom Tags and Layers used in the current scope.
4. **Change Tracking**:
   - **Recent Changes**: Plastic SCM project (has `.plastic/`) → `cm status --short` / `cm log`; git project → `git log --stat -n 10`. Identify the current "hotspots".

## Anti-Hallucination Guardrails
- **DO NOT** assume a scene is structured correctly without scanning its YAML content.
- **DO NOT** rely on memory for Asset GUIDs; always find them in the `.meta` file before searching.
- **STOP** and report immediately if a project-wide integrity issue (like mass missing metas) is detected.

## Output
- **Project Health Snapshot**: Summary of integrity and compile status.
- **Perceived Hierarchy/Structure**: Key objects and components found in the scope.
- **Dependency Map**: Where the targeted scripts or assets are being used.
- **Risk Assessment**: Potential issues found (e.g., circular dependencies, missing refs).
