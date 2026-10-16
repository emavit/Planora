param([string]$ExamplesDirectory)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$directory=if($ExamplesDirectory){(Resolve-Path -LiteralPath $ExamplesDirectory).Path}else{Join-Path $repo 'examples/public-mpp'}
$manifest=@(Get-Content -LiteralPath (Join-Path $directory 'manifest.json') -Raw | ConvertFrom-Json)
$prefix='https://raw.githubusercontent.com/joniles/mpxj/01c5bd162e0b83c57a118f865daa9462b1c0fa8b/'
$names=@{}
foreach($entry in $manifest){
 if($entry.name -notmatch '^[A-Za-z0-9_.-]+\.(mpp|xml)$' -or $names.ContainsKey($entry.name)){throw 'Invalid or duplicate public fixture filename.'}
 if(-not $entry.source.StartsWith($prefix,[StringComparison]::Ordinal)){throw "Unreviewed source: $($entry.name)"}
 $names[$entry.name]=$true
 $path=Join-Path $directory $entry.name
 if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing public fixture: $($entry.name)"}
 if((Get-Item -LiteralPath $path).Length -ne $entry.bytes){throw "Size mismatch: $($entry.name)"}
 if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256){throw "Checksum mismatch: $($entry.name)"}
}
$actual=@(Get-ChildItem -LiteralPath $directory -File | Where-Object {$_.Extension -in '.mpp','.xml'})
foreach($file in $actual){if(-not $names.ContainsKey($file.Name)){throw "Unreviewed fixture file: $($file.Name)"}}
$mpp=@($manifest | Where-Object {$_.name.EndsWith('.mpp')}).Count
$xml=@($manifest | Where-Object {$_.name.EndsWith('.xml')}).Count
if($mpp -ne 14 -or $xml -ne 4 -or $actual.Count -ne $manifest.Count){throw 'Public fixture inventory differs from reviewed 14 MPP / 4 XML collection.'}
$license=Get-Content -LiteralPath (Join-Path $directory 'license-provenance.json') -Raw | ConvertFrom-Json
if((Get-FileHash -LiteralPath (Join-Path $directory 'LICENSE-MPXJ.txt') -Algorithm SHA256).Hash -ne $license.sha256){throw 'Upstream license checksum mismatch.'}
Write-Output "PASS: $mpp public MPP, $xml producer XML, pinned sources/checksums and license; no unreviewed project files."
