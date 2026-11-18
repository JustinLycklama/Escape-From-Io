---
name: play-test
description: Verify a change in the running Unity editor through the Unity CLI. It recompiles, plays a scene, reads console warnings and errors, and takes a screenshot. Use after editing game code, or when asked to check, run or play-test Escape From Io.
---

# Play-test in the editor

PowerShell setup for every command:
`$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User'); $env:UNITY_NO_BANNER=1; $env:UNITY_NO_PAGER=1`

1. **Editor up?** `unity status` should list this project as `ready`. If not, run `unity open .` from the repo root and poll `unity status` (first open takes ~30s).
2. **Compile:** `unity recompile`. An empty table means clean. Fix every error before continuing.
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

Never edit scripts under `Assets/` while in play mode; stop first. Report what was verified and what wasn't (e.g. "visuals not checked").
