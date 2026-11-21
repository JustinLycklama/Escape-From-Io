# Terrain: generation, behaviour and rendering

How the map is built, how it changes during play, and how it's drawn. This is the reference for **expected behaviour**: if the terrain stops
doing something described here, that's a bug. Code is in `Assets/Map Generation/`. For coordinate spaces, see ARCHITECTURE.md → Coordinate spaces.

## Scale and layout

- The world is `mapCountX × mapCountY` **map containers** (`MapContainer`, 5×5 at the time of writing). Each has its own mesh, material and `Map`.
- A container is `layoutMapWidth × layoutMapHeight` **tiles** (layout coordinates, 5×5).
- A tile is `featuresPerLayoutPerAxis` **height samples** per axis (map coordinates, 10×10).
- Containers are scaled (10, 100, 10). A tile is 100 world units wide, and a height of 1.0 is 100 world units.
- **Tile-aligned samples (#35).** A tile's samples span it edge to edge: sample k sits at `k × f / (f − 1)` sample-widths from the tile's
  start (`MapGenerator.SamplePosition`). The first and last samples are **on** the tile boundaries, and neighbouring tiles' edge samples share a
  position. **Expected:** every slope lies inside one tile, so the grid line, the selection highlight and the texture blend all match the geometry.
  (Before #35 samples sat at tile centres, and the quad joining two tiles straddled the boundary: half of every cliff lay on the neighbouring tile.)
- Logic positions (`MapCoordinate` x/y) stay uniform: tile t spans `[t·f, (t+1)·f)`. `MapGenerator.GetHeightAt` samples the surface at a
  position (bilinear within the tile), so robots, ore and buildings sit on the mesh. Code holding sample *indices* uses
  `HeightAtSample` or `finalNoiseMap` directly.

## Generation (`MapGenerator`)

1. **Layout noise**, one value per tile, picks the **region**: Water < 0.3 ≤ Land < 0.475 ≤ Mountain (`TerrainManager` region table).
   Mutator noise then picks the **terrain type** within the region: Water; Empty, Grass, Mud, Sand…; LooseRock, Rock, HardRock, SolidRock.
   The lowest N mountain mutator values become Alunar (azure) rock, so the azure count is controlled rather than random.
   The layout is re-rolled until spawn has walkable land next to it.
2. **Plateau**: `PlateauLayoutMap` flattens each tile's layout value to its region height (`HeightAtRegion`), so a region is one plateau.
3. **Features**: ground and mountain noise add detail: ± for land, up for mountains, none for water. It's read at each sample's tile-aligned
   position (`FeatureAtSample`), so two tiles of the same region get identical heights on their shared edge (no step between two mountains).
4. **Edge shaping** (`MapAtXYWithFeatures`, #35): wherever a tile borders a **lower-priority region** (Mountain 2 > Land 1 > Water 0), or
   the world border, it eases down (smoothstep over `edgeRampWidth` = 0.3 tile, samples 0–2) to that region's plateau, **reaching it exactly
   on the shared boundary**. **Expected:**
   - Mountains slope down to the ground beside them, and ground slopes down to the water, entirely inside the higher tile.
   - The lower tile stays flat, so the water level and its edges are straight.
   - The surfaces meet with no step.
   - All 8 neighbours count (Euclidean distance to each neighbour's square), so corners close cleanly.
   - Tiles of equal priority meet without shaping.
5. Everything is normalized (`minMaxOfFinalMap`) into `finalNoiseMap`, the one global height map that every mesh, normal and unit height reads.
6. One seeded `NoiseGenerator.random` drives all of it. `MapGenerator.Awake` reseeds it and logs `Seed: N`. `MapGenerator.rnd` reads
   `NoiseGenerator.random` each time: a field captured at construction kept the pre-reseed stream, so the seed didn't reproduce the map (#33).

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

  `MapGenerator.CalculateNeighbourEdgeTargets` recomputes every sample within `edgeBand` (3) of the changed tile, in all 8 neighbouring tiles
  (diagonals shape corners, #35), as if the terraform were complete. Shared boundary samples stay equal throughout the animation.
  Those samples animate with the same percentage, and `Map.UpdateNeighbourTileMeshes` re-meshes them, including in other containers.
  Before #29 only the target tile was recomputed, so mined holes had vertical walls.
  `Tools/Stress/mine-tile.cs` mines a tile in steps and prints the neighbour's edge profile.
- Known limitation: if a neighbour is itself mid-terraform, its own animation was computed with the old surroundings and will overwrite
  its edge band as it progresses. When it completes, its edges follow the surroundings it saw when it started.

## Mesh (`MeshGenerator`, `MeshData`)

- One grid mesh per container: one vertex per sample, plus a 1-vertex **overhang** ring.
  - With tile-aligned samples the ring sits on the container boundary, at the same position as the edge samples (#35).
  - Left/top: the neighbouring container's edge heights. These normally equal ours, so the joining strip has zero size.
  - Right/bottom: our own edge heights when a neighbour exists (zero-size strip), or 0 at the world border, which drops a skirt down the
    world's outer edge. Terraform redraws call `UpdateMapOverhang` so the ring follows edge changes.
  - Fog-of-war meshes use the old uniform spacing (`GenerateTerrainMesh(heights, tileAligned: false)`).
- **Normals** come from `MeshGenerator.CalculateNormals` (#25), not `RecalculateNormals`:
  - They're computed from the global height map, so neighbouring containers agree at the seam.
  - They're in world proportions (the 10/100 scale matters).
  - They're creased: at the foot or lip of a cliff the flatter side wins, so flat ground doesn't shade like a groove.
  - Slopes use the real tile-aligned spacing, skipping the neighbouring tile's edge sample that shares a position (#35).
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
- **Height bands (#31):** each texture is also faded outside its region's height band (`heightLow`/`heightHigh` per texture index,
  from `TextureGenerator.HeightBandLists`). The band edge between two regions sits 0.01 (1 world unit) above the lower region's plateau.
  **Expected:** water only below the water surface, ground on its plateau and on banks, rock only where a cliff rises above the ground.
  Without this the purely horizontal blend put water up banks and rock out onto flat ground. Flat ground-to-ground edges are unaffected.
  A tiny weight floor keeps a valid blend when every candidate is out of band (e.g. mid-terraform).
- **Projection (#28):** triplanar weights come from the triangle's real facing (`ddx`/`ddy` of world position), not the creased vertex
  normal. Otherwise cliff faces get the top-down projection and the texture stretches.
- **Bump (#27):** the blended bump normal is world space and is converted to tangent space with a frame passed from `vert`.
  Don't write a world normal to `o.Normal`, and don't read `IN.internalSurfaceTtoW*` in `surf`: it doesn't compile here, and a failed
  compile silently falls back to white `Mobile/Diffuse`. Keep the bump scale at 1. At target ≥ 3.0, `UnpackScaleNormal` applies it, and
  the legacy value of 1000 was only ever ignored.
- **Tile grid lines (#34):** a thin line on every tile boundary, sized in screen pixels (`fwidth` of the layout coordinate), so each clickable
  tile is clear. Faded on steep faces. Colour, strength and width are set in `MapContainer` (`tileGridLineColor`, `tileGridLineWidth`).
  **Expected:** tile edges stay straight. Noisy, wandering transitions were tried and shelved (#32, branch `32-natural-edges`): the water level got
  too variable, and textures and shading didn't line up where edges overlapped. A straight, flat water level plus grid lines reads better.
- **Lighting:** `Tools/set-game-lighting.cs`: a Realtime warm sun (60°, 0.8), a cool opposing fill light (vertex/SH, 0.45) and Trilight ambient.

## Tools

| Script | Use |
|---|---|
| `Tools/Stress/mine-tile.cs` | Mine a mountain near spawn in steps and print the neighbour edge profile (play mode) |
| `Tools/Stress/terrain-seams.cs` | Height profile across tile boundaries near spawn |
| `Tools/Stress/terrain-dips.cs` | Scan revealed seams for heights below both neighbours |
| `Tools/set-game-lighting.cs` / `set-game-skybox.cs` | Scene lighting and skybox |
| `Assets/Tests/Editor/TerrainNormalTests.cs` | Normal creasing and cross-container agreement |
