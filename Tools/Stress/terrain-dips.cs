// Play-mode eval: scan every boundary between two revealed tiles (both axes) in the final height map and report "dips":
// seam samples lower than the lower tile's median height by more than THRESHOLD. Water tiles are skipped (shorelines slope on purpose).
using UnityEngine;
using System.Linq;
using System.Text;
using System.Collections.Generic;

const float THRESHOLD = 0.02f;
if (!Object.FindAnyObjectByType<Narrator>().gameInitialized) return "game not initialized yet";
var gen = Object.FindAnyObjectByType<MapGenerator>();
var constants = Object.FindAnyObjectByType<Constants>();
int f = constants.featuresPerLayoutPerAxis;
float[,] h = gen.GetFinalNoiseMap();
int tw = h.GetLength(0) / f, th = h.GetLength(1) / f;
var sb = new StringBuilder();
int pairs = 0, dips = 0;

float Median(int tx, int ty) {
    var v = new List<float>();
    for (int x = 2; x < f - 2; x++) for (int y = 2; y < f - 2; y++) v.Add(h[tx * f + x, ty * f + y]);
    v.Sort(); return v[v.Count / 2];
}
bool Known(TerrainType t) => t.regionType != RegionType.Type.Unknown && t.regionType != RegionType.Type.Water;

for (int ty = 0; ty < th; ty++) for (int tx = 0; tx < tw; tx++) {
    var a = gen.GetTerrainAtAbsoluteXY(tx, ty);
    if (!Known(a)) continue;
    foreach (var (dx, dy) in new[] { (1, 0), (0, 1) }) {
        int bx = tx + dx, by = ty + dy;
        if (bx >= tw || by >= th) continue;
        var b = gen.GetTerrainAtAbsoluteXY(bx, by);
        if (!Known(b)) continue;
        pairs++;
        float floor = Mathf.Min(Median(tx, ty), Median(bx, by));
        // the two sample lines on either side of the seam, along its whole length (excluding the 3-sample corner ramps)
        float worst = float.MaxValue; string where = "";
        for (int k = 3; k < f - 3; k++) {   // skip the corner ramps toward lower neighbours
            for (int side = -1; side <= 0; side++) {
                int x = dx == 1 ? bx * f + side : tx * f + k;
                int y = dx == 1 ? ty * f + k : by * f + side;
                if (h[x, y] < worst) { worst = h[x, y]; where = $"sample({x},{y})"; }
            }
        }
        if (worst < floor - THRESHOLD) {
            dips++;
            if (dips <= 25) sb.AppendLine($"DIP ({tx},{ty}){a.type}/{a.regionType} {(dx == 1 ? "→x" : "→y")} ({bx},{by}){b.type}/{b.regionType}: floor={floor:0.000} seamMin={worst:0.000} at {where}");
        }
    }
}
return $"revealed non-water seams: {pairs}, dips: {dips}\n{sb}";
