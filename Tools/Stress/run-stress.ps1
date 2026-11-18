# Task-queue stress test. Requires the Unity editor open on this project with com.unity.pipeline (see CLAUDE.md).
# Usage (from repo root):  powershell -File Tools/Stress/run-stress.ps1 [-Ticks 36] [-TimeScale 4]
param([int]$Ticks = 36, [float]$TimeScale = 4, [int]$TickSeconds = 5)

$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
$env:UNITY_NO_BANNER = 1; $env:UNITY_NO_PAGER = 1
function U { unity @args 2>&1 | Where-Object { $_ -notmatch 'claude-code-hint|CategoryInfo|FullyQualifiedErrorId|At line:|~~~|^\s*\+' } }
function Eval($file) { (U command script eval --file $file --timeout 20 | Out-String).Trim() }

U command editor.playmode stop | Out-Null
U command scene open --path Scenes/GameScene | Out-Null
U command console clear | Out-Null
U command editor.playmode start | Out-Null

# Wait for world generation
$setup = ''
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 3
    $setup = Eval 'Tools/Stress/setup.cs'
    if ($setup -notmatch 'not initialized') { break }
}
"setup: $setup"
U command runtime set_time_scale --scale $TimeScale | Out-Null

$cursor = -1
for ($t = 1; $t -le $Ticks; $t++) {
    Start-Sleep -Seconds $TickSeconds
    "tick ${t}: $(Eval 'Tools/Stress/tick.cs')"
    $json = (U command console get --level error --tail 20 --since $cursor --json | Out-String)
    try {
        $r = $json | ConvertFrom-Json
        $cursor = $r.cursor
        foreach ($e in $r.entries) { "  ERROR: $(($e.message -split "`n")[0..6] -join ' | ')" }
    } catch { }
}

U command runtime set_time_scale --scale 1 | Out-Null
"--- final console status"
U command console status
U command editor.playmode stop | Out-Null
