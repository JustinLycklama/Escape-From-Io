# Escape From Io — Architecture

Catalog of the game as it stood at v0.2 (last gameplay commit 2020-05, repo dormant since 2021-04).
Written from a read of the source in Nov 2026; items marked **(unverified)** are inferred from code and not yet confirmed by running the game.

## The game

Indirect-control RTS / colony builder. **Current target: PC only (Windows, mouse + keyboard).** It was originally built mobile-first
(Android/iOS builds, a Joystick Pack, touch input). The mobile code was removed in #9; `legacy-mobile-wip` keeps the old line. You are stranded on Io with a handful of
robots. You never steer robots directly: you place **tasks** (mine this rock, build that, clean this terrain) and robots pull work
from per-type queues. Robots have a battery **duration** and shut down when it runs out, so you must keep building new ones.
Rock Golems spawn over time and attack. **Win:** build the Escape Shuttle. **Lose:** no Miners, Movers or Builders left.
Score = completion time, posted to a Firebase leaderboard.

Core loop: mine terrain → ore drops → Movers haul ore to construction sites → Builders build → more robots/buildings → ship parts → shuttle.

## Layout of the code

Only these folders are first-party (~18k lines). Everything else under `Assets/` is asset-store/vendor content.

| Folder | Contents |
|---|---|
| `Assets/Game Management/` | Scene-level managers (task queue, units, buildings, time, enemies, input, narrator/game flow, scene switching) |
| `Assets/Game Objects/` | `Unit` + subclasses, `Building` + subclasses, `ActionableItem`, `Ore`, `GameResourceManager` |
| `Assets/Map Generation/` | Procedural world: noise, terrain types, mesh/texture gen, `Map`/`MapContainer`, fog of war, minimap |
| `Assets/PathFinding/` | A* grid, path requests, path following |
| `Assets/Math/` | `Heap` (A* open set), `Line`, vision/shadow-casting algorithms (several experimental implementations) |
| `Assets/UI/` | Panels, windows, table views, tooltips, tutorial, leaderboard (`FirebaseManager`). Subfolders `PieChart`, `SciFi UI Pings VFX`, `Assets/Icons` are vendor |
| `Assets/Resources/` | Prefabs loaded by name at runtime (units, buildings, UI). Blueprint `fileName`s must match paths here |
| `GridIdentifiers/` (repo root) | Stray `GridAxis.cs`, outside `Assets/`, not compiled |

Vendor (don't edit, don't document): Voxel Robots, Voxel Ships/Turrets, Mech Robots/Spiders, L3D_Assets, SF_Fighter,
LowPolyWater, PeterLu_HologramShader, Standard Assets, Stylized texture packs, Firebase, Parse.
Exception: `Game Objects/Voxel Ships/RandomShipGenerator.cs`, `StationShip.cs` and `L3D_Assets/EarthElementalController.cs` are yours.

## Scenes

Build settings contain only two scenes:

- `Assets/Scenes/TitleScene.unity` (index 0): title, menu, tutorial picker, leaderboard
- `Assets/Scenes/GameScene.unity` (index 1): the game

`PathfindingScene`, `WorldGeneration`, `PrefabLayoutScene` are dev sandboxes. All other `.unity` files are vendor demos.

`SceneManagement` (plain singleton, not a MonoBehaviour) switches scenes by `State { Title, GameFinish, NewGame, Tutorial }`.
Tutorial mode reuses GameScene with `TutorialManager.isTutorial`, a premade map (`PremadeNoiseGenerator`) and 3x action speed.

## Service locator: `Tag` / `Script` (Constants.cs)

Managers are found via `Script.Get<T>()`: each type maps to a Unity **tag** + component type, results cached, cache cleared on scene unload.
Most managers live on the `Narrator` GameObject. **Adding a manager = add a `Script` entry, add it to `allScripts`, ensure the tag exists.**
Calling `Script.Get` for an unregistered type silently returns `null`.

Other singletons use `sharedInstance` statics: `ResearchSingleton`, `TutorialManager`, `ColorSingleton`, `NameSingleton`, `FirebaseManager`, `SceneManagement`.

## Observer pattern ("Delegate List")

Since 2019-06 the convention is: objects don't track who needs updates; subscribers call `RegisterFor…(this)` / `End…(this)` and
implement a `…Delegate` interface. Registration usually pushes the current state immediately. Always pair Register with End in `OnDestroy`.
Examples: `TaskQueueDelegate`, `TaskLockUpdateDelegate`, `UnitManagerDelegate`, `MasterTaskUpdateDelegate`, `TerrainUpdateDelegate`,
`TimeUpdateDelegate`, `BuildingsUpdateDelegate`, `SelectionManagerDelegate`.

## Game flow: `Narrator`

`Narrator.Start` runs a queue of init "chunks" spread over frames (with a loading percentage):
1. Grab managers, pause, hide UI.
2. `MapGenerator.GenerateWorldStepOne` (+ premade map for tutorial).
3. `GenerateWorldStepTwo` in 4 chunks, then step three.
4. Place the starting Light Tower and starting units (Miner, Mover, Builder; one extra Miner outside the tutorial).
5. Camera fly-in, fog-of-war fade, music fade-in, start tutorial or enable notifications.

Then two watchdog coroutines run:
- `CheckForNoRobots`: game over when no Build/Mine/Move units remain.
- `CheckForTaskQueueManager`: restarts the task-assignment coroutine if it hasn't ticked for >4s, and logs a warning with
  `TaskQueueManager.DiagnosticSummary()` and `PathRequestManager.DiagnosticSummary()` (see Known issues).

`gameInitialized` becomes true once the intro hands control to the player.

`EndGameSuccess` is called when the Escape Shuttle completes; time becomes the score.

## World and coordinates

The world is a grid of `mapCountX × mapCountY` **maps** (`MapContainer` + `Map`), each with its own mesh. Sizes come from the
`Constants` component on Narrator. There are four coordinate spaces (`Map Generation/MapStructs.cs`); mixing them up is the
most likely source of bugs:

| Type | Granularity | Used for |
|---|---|---|
| `LayoutCoordinate` | One gameplay tile (per map, + `mapContainer`) | Terrain type, buildings, user actions, tasks |
| `MapCoordinate` | Height-map sample (`featuresPerLayoutPerAxis` per layout tile) | Height, mesh |
| `PathGridCoordinate` | A* node (`nodesPerLayoutPerAxis` per layout tile) | Pathfinding |
| `WorldPosition` | Unity world space | Transforms, task targets |

Conversions live on the structs and on `MapsManager`. Grid↔map conversion needs padding (fixed in 2019-06-24). `Tools/Stress/coordinates.cs` round-trips every tile through all spaces in a running game.

**Generation** (`MapGenerator`): noise is generated twice, once for the layout (terrain region per tile) and once for features (height detail),
plus mutator maps for ground and mountains. The lowest N mountain values become Azure ("Alunar") rock, so the Azure count is controlled, not random.
The spawn area is plateaued. One seeded `NoiseGenerator.random` drives everything, so a seed reproduces a map. `MapGenerator.Awake` reseeds it for each game and logs `Seed: N`; call `NoiseGenerator.ResetRandom(N)` before generation to replay a map.

**Terrain** (`TerrainManager`, `TerrainType`): Water, Empty, Grass, Mud, Path, ScorchedEarth, LooseRock, Rock, HardRock, SolidRock, AlunarRock, Sand.
Each has walkable, buildable, walk-speed and modification-speed multipliers, textures and mineral drop chances. Region types: Water, Land, Mountain.
`MapGenerator` remembers the original terrain so terraforming can restore it.

**Terraforming**: player actions on a tile come from `TerrainManager.ActionsAvailableForTerrain`: mine rock, "Clean" to Empty, build Path.
Each queues a `MasterGameTask` whose target is the `Map` (an `ActionableItem`). Mining drops `Ore`. Height maps are re-meshed in place.

Other systems: `FogOfWar` + `Math/Vision Casting` (tower vision radius), `MiniMap`, `IntervalActionPipeline` (spreads per-map work across frames).

## Task system (the heart of the game)

`Game Management/GameTask.cs`, `TaskQueueManager.cs`, `Game Objects/Units/Unit.cs`

- **`GameTask`**: one step: `Build, Mine, PickUp, DropOff, FlattenPath, AttackMele, AttackRanged`. It has a target (`WorldPosition`)
  and a `PathRequestTargetType` (World/Layout/PathGrid, or Unknown meaning "find the nearest ore of type X / enemy of faction Y").
  It also has an `actionItem` and an optional `SatisfiesStartRequirements` predicate.
- **`MasterGameTask`**: an ordered list of GameTasks with an `ActionType` (`Mine, Build, Move, Attack`) that picks its queue.
  It supports:
  - `blockerTasks` (must finish first) and `taskBlockedByThis`
  - `repeatCount`: the queue hands out clones until exhausted, and the parent finishes when all children finish
  - `alwaysPerformLast`: only taken when nothing else is available (used for ship parts)
  - `itemContingentOnTask`: destroyed if the task is cancelled, e.g. an unbuilt building
  - Cancelling cascades to clones and blockers.

**Example: placing a building** (`GameResourceManager.CueueGatherTasksForCost`). For each mineral in the cost, it queues a **Move** master task
"Collect X" (PickUp nearest ore, then DropOff at the site) with `repeatCount = amount`. Then it queues a **Build** master task blocked by all of them.

**`TaskQueueManager`** keeps one list per ActionType. Each list has a **lock** (locked = only that unit type may take it; unlocked = any idle robot may).
Each list also has a state (Empty/Blocked/Smooth/Inefficient) for the UI. `DishOutTasks` is a coroutine that ticks every 0.5s:
1. For each unit waiting for work, it checks whether it could **steal** another unit's in-progress task. A steal happens when the
   requester's path cost is < 75% of the remaining cost, it is still on the first GameTask, and it isn't a Move task.
   This requires async path requests.
2. Otherwise, it builds a distance-sorted candidate list per unit. The search order is: own queue, then other *unlocked* queues,
   then `alwaysPerformLast` tasks.
3. Steals are granted first. Then a greedy loop gives the globally-closest unit/task pair, removes exhausted tasks from the
   other units' lists, and repeats.

**`Unit` pipeline**:
1. `ResetTaskState` → `RequestNextDoableTask`, and the callback calls `DoTask`.
2. `DoTask` enqueues the child GameTasks, then `ContinueGameTaskQueue` → `RequestPath` → `foundWaypoints` → `FollowPath` → `PerformTaskAction`.
3. `performAction` runs on the `ActionableItem` until it reports 100%, then the next GameTask runs. When the queue is empty, `MarkTaskFinished`.
4. If no path exists, the unit adds the task to `refuseTaskSet` and puts it back.

Unit state for the UI: Idle / Efficient (doing its primary type) / Inefficient (doing another type).

## Pathfinding

`PathfindingGrid` holds the A* nodes, with movement penalties from terrain, buildings and paths. It also blurs the penalty map and updates on terrain changes.
`PathFinding` runs A* as coroutines using `Math/Heap`, then simplifies the result into `LookPoint`s.
`PathRequestManager` is a **single serialized queue**: one request at a time, drained in a loop. Searches complete synchronously, every request calls back exactly once, and a throwing callback is logged instead of blocking the queue.
It also handles "closest ore of type" and "closest unit of faction" searches. `Path` and `Unit.FollowPath` do smoothed turning along look points.
`movementPenaltyMultiplier` is set per prefab: 0 (ignores terrain) for Advanced Mover, Defender and Golem, and 1 for the rest.

## Units (`Game Objects/Units/`)

Blueprints (cost, prefab and placement rule) are static fields on `Unit.Blueprint`. A unit is "built" like a building:
the prefab carries a `UnitBuilding` component, and `Unit.Initialize()` runs on completion.

| Unit | Primary | Notes |
|---|---|---|
| Miner | Mine | Base unit; speed per task type via `SpeedForTask` |
| Mover | Move | Hauls ore |
| Builder | Build | 3 Copper + 2 Silver |
| Defender | Attack | `AttackingUnit`, hunts Golems. 1 Copper + 3 Silver |
| Adv. Miner / Builder | Mine / Build | Faster; must be built adjacent to the Adv. Units Building |
| Adv. Mover | Move | Ignores terrain penalties |
| Golem | Attack (Enemy faction) | Spawned by `EnemyManager` |

Each unit has `duration` (battery seconds; Research adds to it), `unitHealth`, a status tooltip and a `TimeManager` timer that calls `Shutdown()`.
`Shutdown` puts the task back, plays the death animation, then fades out and destroys the unit.

## Buildings (`Game Objects/Buildings/`)

`Building : ActionableItem`. Construction progress goes through `performAction` with `constructionModifierSpeed` and terrain modifiers
(grass/mud allowed but slow), and meshes appear in tiers. Blueprints are on `Building.Blueprint`:

- Path (faster movement), Light Tower (vision), Azure Sensor (points to the nearest Azure), Turret (`DefenderTower`), Adv. Units Building
- **Ship**: Shuttle Frame (`ShipProps`, needs 4 free buildable neighbours, only one allowed) → Thrusters (+50% move), Reactor (+60s duration),
  Machining (+50% action speed) and Telemetry (vision 3→5) adjacent to it → **Escape Shuttle** on the frame once all four are built = **win**.
  Ship parts double as global upgrades through `ResearchSingleton`.
- Cut features: Refinery and Terraform Land (classes `Refinery`, `TerraformLandBuilding` still exist; their blueprints were commented out and removed in #10, see git history). Minerals include unused `Refined*` types.

`BuildingManager` tracks placement and adjacency, and holds status effects (`BuildingEffectStatus`) that affect the grid.

## Enemies (`EnemyManager`)

Ticks via `TimeManager`. There are no spawns for the first 3 minutes. Spawn frequency scales with player unit count (maxed at 20 units),
between 0.2 and 4 per minute. `evolution` ramps 0→1 over 25 minutes and scales Golem attack (1–10). No spawns in the tutorial.

## Time, input, UI

- `TimeManager`: game clock, pausable (`PlayerBehaviour.gamePaused` / internal pause), and timers via `AddNewTimer(seconds, update, complete)`.
- **Top bar** (#15, `GUI Overlay Panel/TopBar` in GameScene): resources on the left; play, pause, speed and the clock in the centre; threat and settings on the right.
  `GameSpeedControl` sets `Time.timeScale` to 1, 2 or 3 (click to cycle, or keys 1–3). It's disabled in the tutorial, and timeScale is reset to 1 when the scene unloads.
  Pause is separate (`gamePaused`), so the two combine. Built by `Tools/build-top-bar.cs`, which unpacked the `TaskAndUnitPanel` and `OverviewPanel` prefab instances in the scene.
- `PlayerBehaviour`: camera and hotkeys on the **Input System** (#8). The project uses input handling "Both", because vendor scripts still call legacy `Input`;
  first-party code uses only the Input System. Controls: WASD/arrows pan, middle-drag pans (grab the ground), the wheel zooms, left-click selects,
  Space pauses, Q/E/Escape are hotkeys (Escape also works in the tutorial). Camera motion uses unscaled time.
  Hotkey registrations keep `KeyCode` and are mapped to Input System keys. `PanControlsPanel` only shows the WASD hint during the tutorial.
  UI uses `InputSystemUIInputModule`. `Assets/InputSystem.inputsettings.asset` routes all input to the Game view in Play Mode (editor-only), for CLI testing.
- `SelectionManager`: selects terrain or a `Selectable` (unit or building) and drives the detail panels and available `UserAction`s.
- `UIManager` + `UI/Modular Panels`, `Navigation Panels` (Blueprint picker, task and unit overview), `TableViews + Cells`, `Tooltips`, `Windows` (title, menu, help, leaderboard).
- `TutorialManager` + `TutorialBasic/Defense/Escape`: trigger-driven (`TutorialManager.Fire(TutorialTrigger.X)` is sprinkled through gameplay code), and can isolate a single allowed user action.
- `NotificationPanel`, `MessageManager`, `AudioManager`, `VersionNumber`.
- `FirebaseManager`: high scores. Originally Firebase anonymous auth + Realtime Database. Now a local store in PlayerPrefs
  (on the `unity6-upgrade` branch). The name is kept so `HighscoreController` didn't need to change.

## UI style guide

Agreed visual direction: **HUD layout A** (#22). The map fills the screen and the UI sits at the edges: a top bar, robot cards on the left, a command card at bottom centre (only while something is selected), and the minimap bottom-right.

- **Scaling:** every screen-space canvas uses a `CanvasScaler` in Scale With Screen Size mode, with a **1600×900 reference** and match 0.5.
  That's 1.2× at 1080p and 0.8× at 720p. Author sizes in reference units. `Tools/set-canvas-scaling.cs` applies it.
- **Spacing:** 20-unit screen margins, and gaps in multiples of 8.
- **Fonts:** **Agency FB** (regular and bold) for all text, and **DS-DIGI** for numeric readouts only (counts, clock, resources). No other faces.
- **Text sizes (reference units):** 14 caption, 18 body and buttons, 25 heading, 40 title. Don't go below 14.
- **Best Fit:** avoid it in new UI. If you use it, cap the maximum at 48: large Best Fit sizes can overflow the dynamic font atlas and render boxes.
- **Review:** `powershell -File Tools/capture-ui-review.ps1 -Prefix <name> -Resolutions "1920x1080,1280x720"` captures the title screen, HUD and selection
  at true Game-view resolutions (into the gitignored `Screenshots/`).

## Known issues and hazards

The full backlog is in [GitHub Issues](https://github.com/JustinLycklama/Escape-From-Io/issues). This section lists the ones that affect how you read the code.

1. **Idle robots with full queues** ([#1](https://github.com/JustinLycklama/Escape-From-Io/issues/1)). `Unit.refuseTaskSet` never expires, so a single failed path means a robot ignores that task forever.
   The stress test reproduces idle robots with **unlocked** queues. (With locked queues, a robot ignoring other types' tasks is expected.)
   Some task targets also look wrong; that's under investigation in #1.
2. **Pathfinding and assignment hardening** (fixed in #2). `PathRequestManager` delivers each result exactly once, logs callback exceptions without freezing, and fails stuck requests through `ResetIfStuck`, which the watchdog calls.
   `DishOutTasks` bounds its steal wait, re-validates units and tasks before handing them out, and survives per-unit exceptions.
   The v0.2 watchdog remains as a safety net and logs a warning if it ever fires. Regression check: `Tools/Stress/throwing-callback.cs`.
3. Per-game statics are reset on GameScene load (#5): `UnitManager.unitCount` in `UnitManager.Awake`, the random seed in `MapGenerator.Awake`,
   `EnemyManager` in `Start`, and `ResearchSingleton`/`TutorialManager` on scene change. `MasterGameTask` ids are deliberately session-unique.
   Check with `Tools/Stress/game-state.cs` across a scene reload.
4. Many `Resources.Load` calls by string path, and Unity tags. Renames break things at runtime, not compile time.

## Unity 6 upgrade (Nov 2026, now `master`)

Status (2026-11-17): compiles clean on 6000.3.28f1. TitleScene plays with 0 console warnings or errors, and you confirmed the game runs.
The only code fix needed was removing a wildcard `AssemblyVersion` in `VersionNumber.cs`. The API updater changed one vendor call
(`angularDrag` → `angularDamping`). `com.unity.pipeline` (Unity CLI bridge) was added.

Target: **Unity 6000.3.28f1 LTS**. Before the upgrade, these were removed:
- Firebase, Parse, the Google External Dependency Manager, `google-services*.json` and the Firebase plugin folders
- Packages: Ads, Analytics, Purchasing, Collab, Multiplayer HLAPI, ProGrids, XR legacy input helpers, Package Manager UI, and the VS Code IDE package (replaced by `com.unity.ide.visualstudio`)

None of them were referenced by first-party code, scenes or prefabs. ProBuilder stays because the Ship Props prefabs use `ProBuilderMesh`.

## Repo state (Nov 2026, before the upgrade branch)

- Checkout is a **detached HEAD at `6030893`** (v0.2 + download-link fix). `master` is 1 commit ahead ("WIP Mobile Touch Camera", 174 files incl. textures).
  The other branches have no unmerged work except `MeshGenerationTests` (1 experimental commit, 2019).
- Working tree shows **163 deleted `.meta` files and 1 modified ProBuilder settings file**, all uncommitted.
  The binaries they belong to are **missing from disk and were never committed**:
  - Firebase DLLs (`Assets/Firebase/Plugins/*.dll`)
  - Parse plugins
  - many vendor `.obj` models (Voxel Robots/Turrets/Ships)
  - `Buildings/arrow.obj`

  Unity deleted the orphaned metas. The Firebase/Parse DLLs survive in the sibling folder `../PathfindingAndroid`, but they're moot now that Firebase is removed.
  The `.obj` models exist nowhere on disk. Re-download the Voxel Robots/Turrets/StarForce packs from the Asset Store to get them back.
- `docs/` is the GitHub Pages site (Jekyll output). `Static-Site-Jekyll/` is its source. Neither is developer documentation.
- `Build/` holds old Android/OSX/Windows builds (gitignored). They're useful for seeing how the game played.
