param([ValidateSet('win-x64','osx-arm64','osx-x64','linux-x64')][string]$Runtime='win-x64')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'project-app/ProjectDesk/ProjectDesk.csproj'
$output=Join-Path $repo ('artifacts/builds/'+$Runtime+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
$env:AVALONIA_TELEMETRY_OPTOUT='1'
# macOS builds are assembled as Planora.app: binaries in Contents/MacOS, icon, notices and Info.plist alongside.
$mac=$Runtime -like 'osx-*'
$bundle=Join-Path $output 'Planora.app'
$publish=if($mac){Join-Path $bundle 'Contents/MacOS'}else{$output}
$legal=if($mac){Join-Path $bundle 'Contents/Resources'}else{$output}
dotnet publish $project -c Release -r $Runtime --self-contained true -p:TargetFrameworks=net10.0 -p:TargetFramework=net10.0 --configfile (Join-Path $repo 'NuGet.Config') -o $publish
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
New-Item -ItemType Directory -Path $legal -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $legal
Copy-Item -LiteralPath (Join-Path $repo 'THIRD-PARTY-NOTICES.md') -Destination $legal
$notices=Join-Path $legal 'Notices'
New-Item -ItemType Directory -Path $notices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/LICENSE') -Destination (Join-Path $notices 'OfficeIMO-LICENSE')
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/THIRD-PARTY-NOTICES.md') -Destination $notices
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/Licenses') -Destination $notices -Recurse
if($mac){
 $version=@(([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup | ForEach-Object {$_.Version} | Where-Object {$_})[0]
 if($version -notmatch '^\d+(\.\d+){1,3}$'){throw "Unexpected project version: $version"}
 Copy-Item -LiteralPath (Join-Path $repo 'project-app/ProjectDesk/Resources/Planora.icns') -Destination $legal
 $plist=(Get-Content -LiteralPath (Join-Path $repo 'project-app/ProjectDesk/Packaging/macOS/Info.plist') -Raw).Replace('@VERSION@',$version)
 Set-Content -LiteralPath (Join-Path $bundle 'Contents/Info.plist') -Value $plist -NoNewline -Encoding utf8NoBOM
 # Builds made on Windows lose the Unix executable bit; build on macOS or restore it before distributing.
 if(-not $IsWindows){& chmod +x (Join-Path $publish 'Planora');if($LASTEXITCODE -ne 0){throw 'Could not mark Planora executable.'}}
}
Write-Output "Candidate: $(if($mac){$bundle}else{$output}). Validate before distributing."
