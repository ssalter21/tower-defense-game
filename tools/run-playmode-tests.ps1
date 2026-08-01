# Runs the client's PlayMode tests headlessly.
#
# Requires the Unity Editor to be CLOSED — batchmode needs the project lock.
# Deliberately does NOT pass -nographics: the renderer must attach, so the tests
# exercise the same path a real play session does.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Results = "$PSScriptRoot\..\playmode-results.xml",
    [string]$LogFile = "$PSScriptRoot\..\playmode.log"
)

$project = Resolve-Path "$PSScriptRoot\..\client"

& $Unity -batchmode -projectPath $project `
    -runTests -testPlatform PlayMode `
    -testResults $Results -logFile $LogFile

$code = $LASTEXITCODE

if (Test-Path $Results) {
    $xml = [xml](Get-Content $Results)
    $run = $xml.'test-run'
    Write-Host "tests: $($run.total)  passed: $($run.passed)  failed: $($run.failed)  result: $($run.result)"
}

exit $code
