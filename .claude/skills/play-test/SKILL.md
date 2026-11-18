---
name: play-test
description: Verify a change in the running Unity editor through the Unity CLI. It recompiles, plays a scene, reads console warnings and errors, and takes a screenshot. Use after editing game code, or when asked to check, run or play-test Escape From Io.
---

# Play-test in the editor

PowerShell setup for every command:
`$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User'); $env:UNITY_NO_BANNER=1; $env:UNITY_NO_PAGER=1`

1. **Editor up?** `unity status` should list this project as `ready`. If not, run `unity open .` from the repo root and poll `unity status` (first open takes ~30s).
2. **Compile:** `unity recompile`. An empty table means clean. Fix every error before continuing.
   Then run the tests: `unity command test run --mode editor`. All must pass.
3. **Play** (default scene `Scenes/GameScene`, or `Scenes/TitleScene` for menu/UI changes):
   ```
   unity command editor.playmode stop
   unity command scene open --path Scenes/GameScene
   unity command console clear
   unity command editor.playmode start
   ```
   GameScene generates the world for ~20–40s. The game is ready when `Narrator.gameInitialized` is true.
   `Tools/Stress/setup.cs` returns "not initialized yet" until then.
4. **Exercise the change.** Use `unity command script eval --file <file.cs>` for game state. Eval wraps code in a class named `Script`, so
   use `UnityEngine.Object.FindAnyObjectByType<T>()`, not `Script.Get<T>()`. Put reusable eval scripts in `Tools/`, and dry-run them first with `--dry-run true`.
   For task-system changes, run `powershell -File Tools/Stress/run-stress.ps1 -Ticks 30` (it handles steps 3–6 itself).
5. **Read the console:** `unity command console get --level warn --tail 50`.
   Known noise to ignore: "Material … doesn't have _Stencil property" (vendor UI ping VFX).
6. **Look:** `unity command capture game_view --output-path Screenshots/<name>.png`, then Read the PNG.
7. **Stop:** `unity command editor.playmode stop`.

## Visual changes (rendering, lighting, shaders, UI look)

Maps are generated fresh each run, so compare before and after **in one play session**:
- Toggle the change at runtime and capture each state at the same camera. For example: swap the material's shader to a temporary copy of
  the old one, rebuild meshes with old vs new normals, or enable/disable a light. Delete any temporary copies under `Assets/` afterwards.
- Frame a spot: `PlayerBehaviour.PanCameraToPosition(worldPoint, 0.1–0.25, false)`. Find terrain by raycasting a viewport point.
- Compare with numbers, not just eyes. Stitch or crop captures and read pixels with `System.Drawing` through
  `powershell -NoProfile -Command`. A uniform brightness shift means a global cause, not the edge logic.
- Isolate suspects one at a time: hide fog objects or renderers, set an extreme ambient (red) or zero a light, or force a neutral output in
  the shader. Shader edits can be reimported during play (CLAUDE.md); after a reimport, re-push the material data.
- After any shader change, read the console for "Shader error". A failed compile shows as flat white or pink, not as a recompile error.
- For terrain, check the data before the pixels: `Tools/Stress/terrain-seams.cs`, `terrain-dips.cs`, `mine-tile.cs` (see TERRAIN.md).

Never edit scripts under `Assets/` while in play mode; stop first. Report what was verified and what wasn't (e.g. "visuals not checked").
