// Play-mode eval (#29): mine out a revealed mountain tile near spawn that has a mountain neighbour, in 4 steps through
// Map.TerraformAtLocation (the path robots use), and print the neighbour's height profile across the shared edge before/after.
// Set STEP_ONLY = true to just pick and report the tile without mining.
using UnityEngine;
using System.Linq;
using System.Text;

const bool STEP_ONLY = false;
if (!Object.FindAnyObjectByType<Narrator>().gameInitialized) return "game not initialized yet";
var gen = Object.FindAnyObjectByType<MapGenerator>();
var c = Object.FindAnyObjectByType<Constants>();
var mm = Object.FindAnyObjectByType<MapsManager>();
int f = c.featuresPerLayoutPerAxis, W = c.layoutMapWidth, H = c.layoutMapHeight;
var sp = gen.spawnCoordinate;
int sx = sp.mapContainer.mapX * W + sp.x, sy = sp.mapContainer.mapY * H + sp.y;
int tw = gen.GetFinalNoiseMap().GetLength(0) / f, th = gen.GetFinalNoiseMap().GetLength(1) / f;
bool Mountain(int x, int y) => x >= 0 && y >= 0 && x < tw && y < th && gen.GetTerrainAtAbsoluteXY(x, y).regionType == RegionType.Type.Mountain;
var tm = Object.FindAnyObjectByType<TerrainManager>();
bool Minable(int x, int y) => Mountain(x, y) && tm.CanTerriformTo(gen.GetTerrainAtAbsoluteXY(x, y)) != null;

// nearest revealed minable mountain with a revealed mountain to its right (+x), so the profile is easy to read
(int x, int y) pick = (-1, -1);
for (int r = 0; r <= 3 && pick.x < 0; r++)
    for (int dy = -r; dy <= r && pick.x < 0; dy++)
        for (int dx = -r; dx <= r && pick.x < 0; dx++)
            if (Minable(sx + dx, sy + dy) && Mountain(sx + dx + 1, sy + dy)) pick = (sx + dx, sy + dy);
if (pick.x < 0) return "no mountain pair near spawn";

var sb = new StringBuilder($"mining tile ({pick.x},{pick.y}) {gen.GetTerrainAtAbsoluteXY(pick.x, pick.y).type}; neighbour ({pick.x + 1},{pick.y}) {gen.GetTerrainAtAbsoluteXY(pick.x + 1, pick.y).type}\n");
string Profile() {
    float[,] h = gen.GetFinalNoiseMap(); int row = pick.y * f + f / 2;
    return string.Join(" ", Enumerable.Range(pick.x * f + f - 3, 3 + 5).Select(x => (x == (pick.x + 1) * f ? "| " : "") + h[x, row].ToString("0.00")));
}
sb.AppendLine($"before: {Profile()}");
if (STEP_ONLY) return sb.ToString();

var cont = mm.mapContainers.First(m => m.mapX == pick.x / W && m.mapY == pick.y / H);
var coord = new LayoutCoordinate(pick.x % W, pick.y % H, cont);
foreach (float p in new[] { 0.25f, 0.5f, 0.75f, 1f }) {
    cont.map.TerraformAtLocation(coord, p, false);
    sb.AppendLine($"at {p:0.00}: {Profile()}");
}
var w = new WorldPosition(new MapCoordinate(coord)).vector3;
Object.FindAnyObjectByType<PlayerBehaviour>().PanCameraToPosition(w, 0.2f, false);
sb.AppendLine($"now {gen.GetTerrainAtAbsoluteXY(pick.x, pick.y).type}; camera over {w}");
return sb.ToString();
