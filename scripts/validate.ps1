param([switch]$IncludeUi,[switch]$NoBuild)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
 $env:AVALONIA_TELEMETRY_OPTOUT='1'
 if(-not $NoBuild){dotnet build project-app/ProjectDesk/ProjectDesk.csproj -c Release -p:TargetFrameworks=net10.0 -p:TargetFramework=net10.0 --configfile NuGet.Config;if($LASTEXITCODE -ne 0){throw 'Build failed.'}}
 $assembly=Join-Path $repo 'project-app/ProjectDesk/bin/Release/net10.0/Planora.dll'
 $results=Join-Path $repo ('artifacts/validation/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
 $checks=@('durable-test','core-test','auto-model-test')
 if($IncludeUi){$checks+=@('recovery-ui-test','close-test')}
 foreach($check in $checks){$output=Join-Path $results $check;New-Item -ItemType Directory -Path $output -Force | Out-Null;dotnet $assembly ('--'+$check) $output 2>&1 | Tee-Object -FilePath (Join-Path $output 'result.log');if($LASTEXITCODE -ne 0){throw "Check failed: $check"}}
 Write-Output "Validation evidence: $results"
} finally {Pop-Location}
