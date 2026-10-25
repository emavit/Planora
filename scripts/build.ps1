param([ValidateSet('win-x64','osx-arm64','osx-x64','linux-x64')][string]$Runtime='win-x64')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'project-app/ProjectDesk/ProjectDesk.csproj'
$output=Join-Path $repo ('artifacts/builds/'+$Runtime+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$env:AVALONIA_TELEMETRY_OPTOUT='1'
# macOS builds are assembled as Planora.app (binaries in Contents/MacOS; see macos-bundle.ps1).
$mac=$Runtime -like 'osx-*'
$bundle=Join-Path $output 'Planora.app'
$publish=if($mac){Join-Path $bundle 'Contents/MacOS'}else{$output}
dotnet publish $project -c Release -r $Runtime --self-contained true -p:TargetFrameworks=net10.0 -p:TargetFramework=net10.0 --configfile (Join-Path $repo 'NuGet.Config') -o $publish
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
if($mac){
 $version=@(([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup | ForEach-Object {$_.Version} | Where-Object {$_})[0]
 & (Join-Path $PSScriptRoot 'macos-bundle.ps1') -Bundle $bundle -Version $version
 Write-Output "Candidate: $bundle. Validate before distributing."
 return
}
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $output
Copy-Item -LiteralPath (Join-Path $repo 'THIRD-PARTY-NOTICES.md') -Destination $output
$notices=Join-Path $output 'Notices'
New-Item -ItemType Directory -Path $notices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/LICENSE') -Destination (Join-Path $notices 'OfficeIMO-LICENSE')
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/THIRD-PARTY-NOTICES.md') -Destination $notices
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/Licenses') -Destination $notices -Recurse
Write-Output "Candidate: $output. Validate before distributing."
