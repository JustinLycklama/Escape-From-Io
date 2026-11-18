# Terrain: generation, behaviour and rendering

How the map is built, how it changes during play, and how it's drawn. This is the reference for **expected behaviour**: if the terrain stops
doing something described here, that's a bug. Code is in `Assets/Map Generation/`. For coordinate spaces, see ARCHITECTURE.md → Coordinate spaces.

## Scale and layout

- The world is `mapCountX × mapCountY` **map containers** (`MapContainer`, 5×5 at the time of writing). Each has its own mesh, material and `Map`.
- A container is `layoutMapWidth × layoutMapHeight` **tiles** (layout coordinates, 5×5).
- A tile is `featuresPerLayoutPerAxis` **height samples** per axis (map coordinates, 10×10).
- Containers are scaled (10, 100, 10). One sample is 10 world units wide, and a height of 1.0 is 100 world units.

## Generation (`MapGenerator`)

1. **Layout noise**, one value per tile, picks the **region**: Water < 0.3 ≤ Land < 0.475 ≤ Mountain (`TerrainManager` region table).
   Mutator noise then picks the **terrain type** within the region: Water; Empty, Grass, Mud, Sand…; LooseRock, Rock, HardRock, SolidRock.
   The lowest N mountain mutator values become Alunar (azure) rock, so the azure count is controlled rather than random.
   The layout is re-rolled until spawn has walkable land next to it.
2. **Plateau**: `PlateauLayoutMap` flattens each tile's layout value to its region height (`HeightAtRegion`), so a region is one plateau.
3. **Features**: per-sample ground and mountain noise add detail: ± for land, up for mountains, none for water.
4. **Edge shaping** (`MapAtXYWithFeatures`): wherever a tile borders a **lower-priority region** (Mountain 2 > Land 1 > Water 0), its outer
   3 samples (`dipRadius`) ramp down toward its region base. Mountains slope down to the ground beside them, and land slopes down into water.
   Tiles of equal priority meet without a ramp.
5. Everything is normalized (`minMaxOfFinalMap`) into `finalNoiseMap`, the one global height map that every mesh, normal and unit height reads.
6. One seeded `NoiseGenerator.random` drives all of it. `MapGenerator.Awake` reseeds it and logs `Seed: N`.

### Unexplored tiles

- `terrainMap` (what the player knows) starts as all **Unknown**. `originalLayoutTerrainMap` keeps what each tile really is.
- **Expected:** unexplored tiles already have their **real heights** (#28). The height map is built from the original terrain, and they
  only render with the black Unknown texture. A revealed edge meets the real ground beyond it, never a cliff down to 0.
- Wherever shaping needs a tile's type, Unknown resolves to the original type (`TerrainAtLocation`).
- **Reveal** (`MapContainer.StatusEffectMapUpdated`, from tower vision): terraforms the tile's data to its known type, animates fog in,
  re-meshes and re-textures under the fog, then dissolves the fog.

## Behaviour during play: terraforming

- Player actions come from `TerrainManager.ActionsAvailableForTerrain` and `CanTerriformTo`:
  - Mine: any mountain except SolidRock becomes Mud.
  - Clean: Mud, Grass or Sand becomes Empty.
  - Path: Empty becomes Path.
  - Water → Mud exists in the rules, but its action is commented out.
- Each action queues a `MasterGameTask` targeting the `Map`. Robots call `Map.TerraformAtLocation` with a rate.
  - Height animates from the initial to the target heights (`TerraformTarget`) as the percentage rises.
  - On completion the terrain type changes, the pathfinding grid updates, and minerals drop.
- **Expected: neighbouring tiles re-shape too (#29).** Changing a tile changes which edges need a ramp. For example:
  - Mining out a mountain must make the **adjacent mountains round down** into the new hole, instead of standing as sheer walls.
  - Filling water must lift the adjacent land's shoreline ramp.

  `MapGenerator.CalculateNeighbourEdgeTargets` recomputes the 3-sample edge band of the 4 neighbouring tiles as if the terraform were complete.
  Those samples animate with the same percentage, and `Map.UpdateNeighbourTileMeshes` re-meshes them, including in other containers.
  Before #29 only the target tile was recomputed, so mined holes had vertical walls.
  `Tools/Stress/mine-tile.cs` mines a tile in steps and prints the neighbour's edge profile.
- Known limitation: if a neighbour is itself mid-terraform, its own animation was computed with the old surroundings and will overwrite
  its edge band as it progresses. When it completes, its edges follow the surroundings it saw when it started.

## Mesh (`MeshGenerator`, `MeshData`)

- One grid mesh per container: one vertex per sample, plus a 1-vertex **overhang** ring.
  - The left/top overhang copies the neighbouring container's edge heights, which makes the seam.
  - The right/bottom overhang is a hidden skirt at 0. Each container renders the seam strip on its own left/top side.
- **Normals** come from `MeshGenerator.CalculateNormals` (#25), not `RecalculateNormals`:
  - They're computed from the global height map, so neighbouring containers agree at the seam.
  - They're in world proportions (the 10/100 scale matters).
  - They're creased: at the foot or lip of a cliff the flatter side wins, so flat ground doesn't shade like a groove.
- Meshes have **tangents** (`RecalculateTangents`), because the shader needs them (below).
- A terraform at a container edge also redraws the neighbouring containers whose overhang heights, overhang textures or edge normals
  depend on it (`MapContainer.UpdateNeibourOverhangsForCoordinate`).

## Rendering (`Custom/TerrainSelectionMobile`)

- Triplanar sampling of a texture array, one layer per terrain type. Per-container data:
  - `layoutTextures`: 7×7, the tiles plus the overhang ring, -1 = no tile.
  - `indexPriority` and `indexScale`.
  - `selection` and `actionAtIndex` (tints).
- **Blending (#26):** each pixel blends the 3×3 surrounding tiles.
  - The weight is `exp(-(distance to the tile's square − priority × priorityReach) / blendFalloff)`.
  - The blend is continuous across edges and round at corners.
  - Higher regions reach a little past their edge, so cliff faces get rock rather than the ground's texture.
  - Albedo, the selection and action tints, and bump all blend together.
- **Projection (#28):** triplanar weights come from the triangle's real facing (`ddx`/`ddy` of world position), not the creased vertex
  normal. Otherwise cliff faces get the top-down projection and the texture stretches.
- **Bump (#27):** the blended bump normal is world space and is converted to tangent space with a frame passed from `vert`.
  Don't write a world normal to `o.Normal`, and don't read `IN.internalSurfaceTtoW*` in `surf`: it doesn't compile here, and a failed
  compile silently falls back to white `Mobile/Diffuse`. Keep the bump scale at 1. At target ≥ 3.0, `UnpackScaleNormal` applies it, and
  the legacy value of 1000 was only ever ignored.
- **Lighting:** `Tools/set-game-lighting.cs`: a Realtime warm sun (60°, 0.8), a cool opposing fill light (vertex/SH, 0.45) and Trilight ambient.

## Tools

| Script | Use |
|---|---|
| `Tools/Stress/mine-tile.cs` | Mine a mountain near spawn in steps and print the neighbour edge profile (play mode) |
| `Tools/Stress/terrain-seams.cs` | Height profile across tile boundaries near spawn |
| `Tools/Stress/terrain-dips.cs` | Scan revealed seams for heights below both neighbours |
| `Tools/set-game-lighting.cs` / `set-game-skybox.cs` | Scene lighting and skybox |
| `Assets/Tests/Editor/TerrainNormalTests.cs` | Normal creasing and cross-container agreement |
