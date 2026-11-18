// Play Mode audit: for every Image under a root (ROOT_NAME), report the sprite's source texture size and import settings
// versus the size it is drawn at on screen. Flags heavy downscaling, compression, missing mipmaps and 9-slice borders
// that are thicker than the drawn rect allows.
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.Collections.Generic;

const string ROOT_NAME = "RobotCards";
var root = GameObject.Find(ROOT_NAME);
if (root == null) return ROOT_NAME + " not found";
var canvas = root.GetComponentInParent<Canvas>().rootCanvas;
float scale = canvas.scaleFactor;
var sb = new StringBuilder();
sb.AppendLine($"canvas scaleFactor={scale:0.##} (screen px per reference unit), screen={Screen.width}x{Screen.height}");
var seen = new HashSet<string>();

foreach (var img in root.GetComponentsInChildren<Image>(false).Where(i => i.enabled && i.sprite != null)) {
    var rt = (RectTransform)img.transform;
    var sp = img.sprite;
    var tex = sp.texture;
    float drawW = rt.rect.width * scale, drawH = rt.rect.height * scale;
    float srcW = sp.rect.width, srcH = sp.rect.height;
    string key = sp.name + "@" + Mathf.RoundToInt(drawW) + "x" + Mathf.RoundToInt(drawH);
    if (!seen.Add(key)) continue;

    var b = sp.border; // L, B, R, T in sprite pixels
    string slice = "";
    if (img.type == Image.Type.Sliced && b != Vector4.zero) {
        // drawn border thickness in screen px = sprite px / (sprite PPU / canvas reference PPU) / multiplier * canvas scale
        float ppuRatio = sp.pixelsPerUnit / canvas.referencePixelsPerUnit * img.pixelsPerUnitMultiplier;
        float bl = b.x / ppuRatio * scale, br = b.z / ppuRatio * scale, bt = b.w / ppuRatio * scale, bb = b.y / ppuRatio * scale;
        bool overlapX = bl + br > drawW, overlapY = bt + bb > drawH;
        slice = $" sliced border(px drawn L{bl:0} R{br:0} T{bt:0} B{bb:0}){(overlapX || overlapY ? " OVERLAP" : "")} ppuMult={img.pixelsPerUnitMultiplier}";
    }
    float down = Mathf.Max(srcW / Mathf.Max(drawW, 1), srcH / Mathf.Max(drawH, 1));
    sb.AppendLine($"{img.name,-22} sprite={sp.name,-24} src={srcW:0}x{srcH:0} drawn={drawW:0}x{drawH:0} downscale={down:0.0}x type={img.type}{slice} | tex {tex.width}x{tex.height} fmt={tex.format} mips={tex.mipmapCount} filter={tex.filterMode}");
}
return sb.ToString();
