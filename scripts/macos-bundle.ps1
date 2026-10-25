param([Parameter(Mandatory)][string]$Bundle,[Parameter(Mandatory)][string]$Version)
# Completes Planora.app after dotnet publish wrote the binaries to Contents/MacOS:
# notices and icon in Contents/Resources, Info.plist from the template, executable bit.
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if($Version -notmatch '^\d+(\.\d+){1,3}$'){throw "Unexpected project version: $Version"}
$executable=Join-Path $Bundle 'Contents/MacOS/Planora'
if(-not (Test-Path -LiteralPath $executable -PathType Leaf)){throw 'Publish output is missing Contents/MacOS/Planora.'}
$resources=Join-Path $Bundle 'Contents/Resources'
New-Item -ItemType Directory -Path $resources -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination $resources
Copy-Item -LiteralPath (Join-Path $repo 'THIRD-PARTY-NOTICES.md') -Destination $resources
$notices=Join-Path $resources 'Notices'
New-Item -ItemType Directory -Path $notices -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/LICENSE') -Destination (Join-Path $notices 'OfficeIMO-LICENSE')
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/THIRD-PARTY-NOTICES.md') -Destination $notices
Copy-Item -LiteralPath (Join-Path $repo 'project-app/vendor/OfficeIMO/OfficeIMO.Core/Licenses') -Destination $notices -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'project-app/ProjectDesk/Resources/Planora.icns') -Destination $resources
$plist=(Get-Content -LiteralPath (Join-Path $repo 'project-app/ProjectDesk/Packaging/macOS/Info.plist') -Raw).Replace('@VERSION@',$Version)
Set-Content -LiteralPath (Join-Path $Bundle 'Contents/Info.plist') -Value $plist -NoNewline -Encoding utf8NoBOM
# Bundles made on Windows lose the Unix executable bit; build on macOS or restore it before distributing.
if(-not $IsWindows){& chmod +x $executable;if($LASTEXITCODE -ne 0){throw 'Could not mark Planora executable.'}}
# Ad-hoc sign the finished bundle so its signature covers the added resources; otherwise Gatekeeper reports a
# downloaded copy as damaged instead of offering Open Anyway. This is not a Developer ID signature.
if($IsMacOS){& codesign --force --deep --sign - $Bundle;if($LASTEXITCODE -ne 0){throw 'Could not ad-hoc sign Planora.app.'}}
