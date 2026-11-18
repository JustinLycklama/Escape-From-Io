# UI review captures (#14 and the HUD layout tickets). Requires the editor open on this project.
# Captures TitleScene, the in-game HUD, and the HUD with a tile selected, at each resolution given.
# Usage (repo root): powershell -File Tools/capture-ui-review.ps1 -Prefix after -Resolutions "1920x1080,2560x1440,1280x720"
param([string]$Prefix = "review", [string]$Resolutions = "1920x1080")

$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
$env:UNITY_NO_BANNER = 1; $env:UNITY_NO_PAGER = 1
function U { unity @args 2>&1 | Where-Object { $_ -notmatch 'claude-code-hint|CategoryInfo|FullyQualifiedErrorId|At line:|~~~|^\s*\+' } }
function E($f) { (U command script eval --file $f | Out-String).Trim() }

foreach ($res in ($Resolutions -split ',' | ForEach-Object { $_.Trim() })) {
    $w, $h = $res -split 'x'
    $resScript = Join-Path $env:TEMP "set-res.cs"
    Set-Content -Path $resScript -Value "UnityEditor.PlayModeWindow.SetCustomRenderingResolution($w, $h, `"review $res`"); return `"$res`";"
    E $resScript | Out-Null

    U command editor.playmode stop | Out-Null
    U command scene open --path Scenes/TitleScene | Out-Null
    U command editor.playmode start | Out-Null
    Start-Sleep 6
    U command capture game_view --output-path "Screenshots/$Prefix-$res-1-title.png" --width $w --height $h | Out-Null
    U command editor.playmode stop | Out-Null
    Start-Sleep 2

    U command scene open --path Scenes/GameScene | Out-Null
    U command console clear | Out-Null
    U command editor.playmode start | Out-Null
    for ($i = 0; $i -lt 40; $i++) { Start-Sleep 3; if ((E 'Tools/Stress/game-state.cs') -match 'initialized=True') { break } }
    Start-Sleep 3
    U command capture game_view --output-path "Screenshots/$Prefix-$res-2-hud.png" --width $w --height $h | Out-Null
    E 'Tools/Stress/ui-states.cs' | Out-Null
    Start-Sleep 2
    U command capture game_view --output-path "Screenshots/$Prefix-$res-3-selection.png" --width $w --height $h | Out-Null
    $c = (U command console get --level warn --tail 20 --json | Out-String) | ConvertFrom-Json
    "$res console: $($c.counts | ConvertTo-Json -Compress)"
    U command editor.playmode stop | Out-Null
}
