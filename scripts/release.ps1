param(
 [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
 [switch]$IncludeNative
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$definition=Join-Path $repo "releases/$Version/release.json"
$notes=Join-Path $repo "releases/$Version/release.md"
if(!(Test-Path -LiteralPath $definition) -or !(Test-Path -LiteralPath $notes)){throw 'A release definition and release.md are required.'}
$spec=Get-Content -LiteralPath $definition -Raw | ConvertFrom-Json
# User-approved combined release: combined workspace release is numbered 0.3.3.
# User requested immediate release of the timeline toggle in 0.3.10.
$expectedFeatureCount=if($Version -eq '0.3.3'){4}elseif($Version -eq '0.3.10'){1}else{2}
if($spec.version -ne $Version){throw 'Release version does not match.'}
if($spec.kind -eq 'bugfix'){
 if(@($spec.features).Count -ne 0 -or @($spec.fixes).Count -lt 1){throw 'Bugfix releases require fixes and no claimed new features.'}
}else{
 if(@($spec.features).Count -ne $expectedFeatureCount){throw "Release $Version must declare exactly $expectedFeatureCount new features."}
}
$project=Join-Path $repo 'project-app/ProjectDesk/ProjectDesk.csproj'
if(([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version -ne $Version){throw 'Project version does not match release definition.'}
$output=Join-Path $repo "project-app/releases/$Version-win-x64"
if(Test-Path -LiteralPath $output){throw 'Release destination already exists. Use a new version; completed releases are immutable.'}
$staging=Join-Path $repo ('artifacts/release-staging/'+$Version+'-'+[Guid]::NewGuid().ToString('N'))
$env:AVALONIA_TELEMETRY_OPTOUT='1'
Push-Location $repo
try {
 dotnet publish $project -c Release -r win-x64 --self-contained true -p:TargetFrameworks=net10.0 -p:TargetFramework=net10.0 --configfile (Join-Path $repo 'NuGet.Config') -o $staging
 if($LASTEXITCODE -ne 0){throw 'Publish failed; no release created.'}
 foreach($file in @('LICENSE','THIRD-PARTY-NOTICES.md')){Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $staging}
 $notices=Join-Path $staging 'Notices';New-Item -ItemType Directory -Path $notices -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/LICENSE') -Destination (Join-Path $notices 'OfficeIMO-LICENSE')
 Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/THIRD-PARTY-NOTICES.md') -Destination $notices
 Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/Licenses') -Destination $notices -Recurse
 $evidence=Join-Path $staging 'validation';New-Item -ItemType Directory -Path $evidence -Force | Out-Null
 $assembly=Join-Path $staging 'Planora.dll'
 $checks=@('durable-test','core-test','auto-model-test','recovery-ui-test','close-test')+@($spec.checks)
 foreach($check in ($checks | Select-Object -Unique)){
  if($check -notmatch '^[a-z-]+-test$'){throw 'Invalid validation command.'}
  $folder=Join-Path $evidence $check;New-Item -ItemType Directory -Path $folder -Force | Out-Null
  dotnet $assembly ('--'+$check) $folder 2>&1 | Tee-Object -FilePath (Join-Path $folder 'result.log')
  if($LASTEXITCODE -ne 0){throw "Validation failed: $check. Staging retained; no release created."}
 }
 & (Join-Path $PSScriptRoot 'verify-mpp-regressions.ps1') -Assembly $assembly -Output (Join-Path $evidence 'public-mpp')
 $nativeStatus='Not run; native Windows qualification pending.'
 if($IncludeNative){
  $fixture=Join-Path $evidence 'core-test/core-functions.mpp'
  $nativeLog=Join-Path $evidence 'native.log';$nativeErrors=Join-Path $evidence 'native-errors.log'
  $process=Start-Process -FilePath (Join-Path $staging 'Planora.exe') -ArgumentList @('--stability-check',('"'+$fixture+'"')) -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput $nativeLog -RedirectStandardError $nativeErrors
  if(!$process.WaitForExit(60000)){throw 'Owned native check exceeded 60 seconds; staging retained for investigation.'}
  $process.Refresh()
  if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $nativeLog -Pattern 'PASS native Windows' -Quiet)){throw 'Native Windows check failed; no release created.'}
  $nativeStatus='Passed: 20 native insert/paste/format/undo/redo/scroll cycles.'
 }
 Copy-Item -LiteralPath $notes -Destination (Join-Path $staging 'release.md')
 Add-Content -LiteralPath (Join-Path $staging 'release.md') -Value "`n## Build validation`n`nNative: $nativeStatus`n`nAutomated checks: $($checks -join ', '). See validation/ for results."
 Copy-Item -LiteralPath $definition -Destination $staging
 $hashes=Get-ChildItem -LiteralPath $staging -File -Recurse | ForEach-Object { [ordered]@{path=[IO.Path]::GetRelativePath($staging,$_.FullName);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} }
 $hashes | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $staging 'SHA256SUMS.json') -Encoding utf8
 New-Item -ItemType Directory -Path (Split-Path $output -Parent) -Force | Out-Null
 # Verify resolved directory boundaries before moving a recursive tree.
 $repoPrefix=[IO.Path]::GetFullPath($repo).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
 foreach($target in @($staging,$output)){if(![IO.Path]::GetFullPath($target).StartsWith($repoPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Release path escaped repository.'}}
 Move-Item -LiteralPath $staging -Destination $output
 Write-Output "Release ready: $output"
} finally {Pop-Location}
