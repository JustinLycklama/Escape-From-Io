// Play-mode eval: measure the height profile across tile boundaries in the revealed spawn area.
// For each pair of x-adjacent revealed tiles near spawn, prints the mid-row heights (MapCoordinate samples) across both tiles,
// so dips at the seam show up as values below both plateaus. Also prints the region config the edge blending uses.
using UnityEngine;
using System.Linq;
using System.Text;
using System.Reflection;

if (!Object.FindAnyObjectByType<Narrator>().gameInitialized) return "game not initialized yet";
var sb = new StringBuilder();
var gen = Object.FindAnyObjectByType<MapGenerator>();
var tm = Object.FindAnyObjectByType<TerrainManager>();
var constants = Object.FindAnyObjectByType<Constants>();
int f = constants.featuresPerLayoutPerAxis;
foreach (var kv in tm.regionTypeMap) sb.AppendLine($"region {kv.Key}: base={kv.Value.noiseBase} max={kv.Value.noiseMax} plateauAtBase={kv.Value.plateauAtBase}");
var mm = typeof(MapGenerator).GetField("minMaxOfFinalMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gen);
sb.AppendLine($"features/tile={f} groundImpact={gen.groundFeaturesImpactOnLayout} mountainImpact={gen.mountainFeaturesImpactOnLayout} minMax={JsonUtility.ToJson(mm)}");

float[,] h = gen.GetFinalNoiseMap();
var spawn = gen.spawnCoordinate;
int sx = spawn.mapContainer.mapX * constants.layoutMapWidth + spawn.x;
int sy = spawn.mapContainer.mapY * constants.layoutMapHeight + spawn.y;
int shown = 0;
for (int dy = -2; dy <= 2 && shown < 8; dy++) {
    for (int dx = -2; dx <= 1 && shown < 8; dx++) {
        int ax = sx + dx, ay = sy + dy;
        if (ax < 0 || ay < 0 || ax + 1 >= h.GetLength(0) / f || ay >= h.GetLength(1) / f) continue;
        var a = gen.GetTerrainAtAbsoluteXY(ax, ay); var b = gen.GetTerrainAtAbsoluteXY(ax + 1, ay);
        if (a.regionType == RegionType.Type.Unknown || b.regionType == RegionType.Type.Unknown) continue;
        int row = ay * f + f / 2;
        var vals = Enumerable.Range(ax * f, 2 * f).Select(x => h[x, row].ToString("0.000"));
        sb.AppendLine($"({ax},{ay}) {a.type}/{a.regionType} | {b.type}/{b.regionType}:  {string.Join(" ", vals.Take(f))} | {string.Join(" ", vals.Skip(f))}");
        shown++;
    }
}
return sb.ToString();
