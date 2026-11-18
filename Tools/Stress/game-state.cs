// Reports per-game state that must reset between games (#5): seed, unit counters vs live units, init flag.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Linq;

var narrator = UnityEngine.Object.FindAnyObjectByType<Narrator>();
var units = UnityEngine.Object.FindAnyObjectByType<UnitManager>()?.GetAllPlayerUnits() ?? new Unit[0];
string counts = string.Join(",", UnitManager.unitCount.Select(kv => $"{kv.Key}={kv.Value}"));
int live = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Count(u => u.factionType == Unit.FactionType.Player);
return $"initialized={narrator?.gameInitialized} seed={NoiseGenerator.seed} unitCount[{counts}] liveUnitObjects={live} registered={units.Length} research.speed={ResearchSingleton.sharedInstance.unitSpeedMultiplier}";
