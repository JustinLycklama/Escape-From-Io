# Learnings: revival sessions (Nov 2026)

What we learned bringing Escape From Io back: the 2019 → Unity 6 upgrade, the PC HUD pass (#14–#22) and the terrain work (#25–#29).
Every item says what we saw, why it happened and where it's handled, so the next session doesn't rediscover it. The short rules are in
CLAUDE.md, and the system detail is in ARCHITECTURE.md and TERRAIN.md. This file is the "how we found out" record.

## Tooling and environment

- **Unity CLI, not MCP.** `unity` (winget) drives the open editor through `com.unity.pipeline`. Run `unity command` and `unity command <group>`
  to list commands before guessing flags. `unity recompile` lists C# errors only (see shaders below).
- **`script eval` quirks**
  - The code is wrapped in a class named `Script`, which shadows the game's `Script.Get<T>()` service locator. Use `Object.FindAnyObjectByType<T>()`.
    Qualify it with `Object.`: bare `FindAnyObjectByType` failed to compile in some evals.
  - `--code '…'` for one-liners, `--file` for anything longer, `--dry-run true` to compile-check.
  - Environment variables don't reach eval code. Put constants at the top of the file and `sed` them for variants
    (that's how the lighting rigs and shader A/Bs were run).
  - Unity "fake null": don't use `??` or `?.` on `UnityEngine.Object` values in eval or game code; compare with `== null`.
- **Shells on this machine**
  - git isn't on PowerShell's PATH; use the Bash tool for git.
  - `python3` in Git Bash is the Windows Store stub. It hangs waiting for the Store, so don't call it.
  - `cat > file` without a heredoc waits on stdin forever. Always use `<<'EOF'`.
  - `powershell -File script.ps1` is blocked by the execution policy for new scripts. Use `powershell -NoProfile -Command "…"`
    or `-ExecutionPolicy Bypass -File`.
  - `System.Drawing` from PowerShell is the quickest way to crop, stitch and pixel-sample screenshots.
- **Editor modal hang.** Git rewriting an open `*.unity` file pops "scene changed on disk, reload?", which blocks the editor and the CLI.
  Merge with `Tools/merge-ticket.sh` (builds the merge commit with `git commit-tree`, no checkout), and open an empty scene
  (`Tools/open-empty-scene.cs`) before any other git operation that may touch scenes.
- **Recompiling scripts during play** reloads the domain mid-game. Non-serialized dictionaries come back null, and per-frame code throws.
  This made `TaskQueueManager.QueuedTaskCount` spam NREs once (now null-safe). Rule: stop play before touching `Assets/` scripts.
- **Shader edits are safe during play.** They don't reload the domain. Reimport with
  `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`, then re-push material data. A reimport can drop arrays set with
  `SetFloatArray` (terrain went black until `MapContainer.SetupMaterialShader()` + `UpdateMapOverhang()` ran again).
- **Shader compile errors are silent to `unity recompile`.** A failing shader falls back (here to white `Mobile/Diffuse`) and only logs
  "Shader error in …" to the console. If a material suddenly renders flat white or pink, read the console first.
- **Maps differ every run** (seeded per game). For before/after comparisons, A/B *inside one play session*: toggle the change at runtime
  (swap the material's shader, rebuild meshes with old vs new normals, enable/disable a light), capture each, then compare.

## Debugging techniques that worked

- **Measure the data before judging the picture.** The terrain "dip" (#25) looked like geometry. Dumping heights across seams
  (`Tools/Stress/terrain-seams.cs`, `terrain-dips.cs`) showed perfectly flat data, which pointed at lighting instead.
- **Toggle suspects one at a time.** For the black hill sides (#27) we toggled fog meshes, the tower point light, custom normals and
  ambient (an extreme red ambient proves whether ambient reaches the surface). Then a neutral `o.Normal` proved the shader was the cause.
- **Pixel-sample instead of eyeballing.** Reading two fixed pixels showed the new shader was 45% darker even where no blending happens,
  which narrowed it to something global (the bump scale).
- **Script the reproduction.** `Tools/Stress/mine-tile.cs` mines a tile in steps through the same call robots use and prints the
  neighbour profile at each step. That's faster and more precise than waiting for a robot.
- **Check history before "restoring" something.** Mining's neighbour rounding (#29) was believed lost in the migration. `git log -S` /
  `git diff legacy-mobile-wip master` showed it never existed. That changed the fix from "restore" to "implement".
- **Camera control from eval:** `PlayerBehaviour.PanCameraToPosition(world, heightPercent, false)` with `heightPercent` around 0.1–0.25 gives
  close-ups; raycast a viewport point to find terrain to look at. Capture at 1600×900 (the UI reference size) unless checking scaling.

## Unity gotchas we hit

### UI and prefabs (HUD pass #14–#22)
- Children of prefab instances can't be re-parented. Unpack the instance first (`PrefabUtility.UnpackPrefabInstance`).
- Stale per-instance `RectTransform` overrides beat prefab edits (robot cards had 261 elements at height 0). Revert the overrides when
  restyling a prefab.
- A *disabled* `AspectRatioFitter` still drives the height to 0 in some layouts. Destroy it, don't disable it.
- Stretch anchors need explicit `offsetMin`/`offsetMax`, then verify the actual `rect`. A builder produced a 0×0 card area that "looked right" in code.
- Text with vertical overflow set to Truncate disappears silently when the box is too short. Use Overflow for captions.
- Best Fit with a high max size and a large font overflowed the dynamic font atlas, and characters rendered as boxes. Cap Best Fit at 48
  (style guide) and use DS-DIGI for numbers.
- Hiding a UI slot before its `Awake` ran caused an NRE when it was configured. Activate it first, then configure it (`ActionsList`).
- `simulate_pointer` clicks UI through the EventSystem without moving the Input System mouse. Gameplay input needs queued device events
  (`Tools/Stress/input-sequence.cs`).
- Several components ignore parent `CanvasGroup`s (`ignoreParentGroups`). The robot markers (#21) only faded their task line until the
  child groups were made to inherit from a root group.

### Rendering and meshes (terrain #25–#28)
- **Non-uniform scale and normals.** Terrain containers are scaled (10, 100, 10). A 6° tilt in mesh space is about 45° in the world.
  Compute normals in world proportions, then pre-multiply by the scale for the mesh (Unity applies the inverse transpose).
- **Per-mesh `RecalculateNormals` disagrees at mesh seams.** Each mesh only sees its own triangles, and hidden skirt geometry bends the edge
  normals. Compute normals from shared source data (the global height map).
- **Surface shaders: `o.Normal` is tangent space.** Writing a world-space normal into it scrambles lighting: flat ground lights like a wall.
  The mesh also needs tangents (`RecalculateTangents`). Build the world tangent frame in `vert` yourself. Reading `IN.internalSurfaceTtoW*`
  in `surf` fails to compile with this setup.
- **`UnpackScaleNormal(n, scale)` ignores the scale below shader target 3.0.** The legacy bump scale of 1000 was harmless until we raised
  the target to 3.5 for texture arrays; then the terrain went 45% darker.
- **Triplanar weights from smoothed or creased normals stretch textures on cliffs.** Use the triangle's real facing
  (`cross(ddy(worldPos), ddx(worldPos))`) for projection weights and keep smooth normals for lighting.
- **Blending by "pick a neighbour" branches gives seams.** A continuous weight function over the 3×3 neighbourhood
  (exp falloff by distance to each tile's square) is smooth everywhere, including corners, with no special cases.
- **A blend that only knows horizontal distance ignores the geometry.** Banks and cliff feet sit inside the blend band, so water crept up
  banks and rock spilled onto flat ground (#31). The coordinates were correct (verified: sample s → layout (s + 0.5)/10, and the selection
  tint lands on the right tile). Masking each texture by a height band fixed it and left flat ground-to-ground edges pixel-identical.
  Ask "misaligned or too strong?" with numbers: check the mapping, then compare pixel profiles.
- A light in **Baked** mode with no baked lighting data behaves inconsistently. Use Realtime explicitly.
- A non-important directional fill light (`ForceVertex`) is folded into SH for objects, so it lifts shadow sides at almost no cost.
  Trilight ambient gives a top/side/bottom gradient that a flat ambient can't.

## Project findings, by area

- **Task system:** the v0.2 "stall" has several contributors. Path callbacks could fire twice or throw and freeze pathfinding (#2), and
  the watchdog compared `TimeSpan.Seconds` (#3). `refuseTaskSet` never expires: that's the open #1. Locked queues mean a robot ignoring
  other types' tasks is *expected*, not a stall.
- **Terrain heights:** unexplored tiles used to be height 0 until revealed (#28). Tile-edge ramps are generated from neighbours, so any
  terraform must re-shape neighbours too (#29). Expected behaviour is written down in TERRAIN.md.
- **Terrain rendering:** most of the "bad lighting" was the shader's tangent-space bug, not the light setup (#27). Fix shaders before
  adding lights.
- **The black around the map** is unexplored terrain drawn with the black Unknown texture, not empty space. A skybox only shows at the
  outer edges (#19). Styling unexplored ground is parked as #24.
- **Missing assets:** `.obj` models were never committed, because an old global gitignore excluded them. Unity deletes their orphaned metas on
  open; don't commit those deletions (CLAUDE.md). The two models used in-game have ProBuilder stand-ins (#6).

## Process that worked

- One GitHub issue per change, with findings written into the issue body or a comment *before* fixing, a branch `<n>-name`,
  `Fixes #<n>`, and a merge via `Tools/merge-ticket.sh`. Docs are updated in the same commit as behaviour changes.
- Builders as re-runnable editor eval scripts in `Tools/` (`build-*.cs`, `set-*.cs`, `fix-*.cs`) instead of hand-editing scenes. Every
  layout, lighting and material change can be reproduced or tweaked by editing constants and re-running.
- Ask for a mock-up or a choice before big visual work (HUD layout A was picked from mock-ups). Show before/after captures for every
  visual change.
