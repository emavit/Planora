param([Parameter(Mandatory)][string]$Assembly,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$baseline=Get-Content -LiteralPath (Join-Path $repo 'docs/compatibility-known-failures.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Path $Output -Force | Out-Null
dotnet $Assembly --public-mpp-test (Join-Path $repo 'examples/public-mpp') $Output 2>&1 | Tee-Object -FilePath (Join-Path $Output 'result.log')
$exitCode=$LASTEXITCODE
$result=Get-Content -LiteralPath (Join-Path $Output 'results.json') -Raw | ConvertFrom-Json
if($exitCode -notin @(0,1) -or ($result.checks+$result.failures) -ne $baseline.totalStages){throw 'Corpus runner failed or stage coverage changed; review required.'}
if(@($result.results).Count -ne @($baseline.files).Count){throw 'Corpus file count changed.'}
foreach($file in $result.results){
 $expected=@($baseline.files | Where-Object file -eq $file.file)
 if($expected.Count -ne 1 -or $file.sha256 -ne $expected[0].sha256){throw 'Corpus identity changed; requalify the baseline.'}
 foreach($stage in $file.stages | Where-Object status -eq 'fail'){
  $known=@($baseline.failures | Where-Object { $_.file -eq $file.file -and $_.stage -eq $stage.stage -and $_.error -eq ($stage.error -split "\r?\n")[0] })
  if($known.Count -ne 1){throw "New MPP regression: $($file.file) / $($stage.stage)"}
 }
}
Write-Output "MPP regression gate passed: $($result.checks) passed stages, $($result.failures) documented existing failures; full compatibility is not qualified."

$global:LASTEXITCODE=0
