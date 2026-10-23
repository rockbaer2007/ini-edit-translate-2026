param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$runtime = Join-Path $repoRoot 'Ini Edit - Translate/bin/Release'
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $runtime 'INI Edit Translate 2026.exe')).FileVersion
$shortVersion = $version -replace '\.0$', ''
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $outputRoot ('stage-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$mainFiles = @('INI Edit Translate 2026.exe','INI Edit Translate 2026.exe.config','IET.Updater.exe',
    'IniFile.dll','Newtonsoft.Json.dll','Info.rtf','Info.de.rtf','Info.en.rtf','Info.fr.rtf')
# Project references can copy helper content into the main build directory as well.
# Package only the intended main files; the complete helper belongs under help/.
$packageFiles = @($mainFiles | ForEach-Object { Get-Item -LiteralPath (Join-Path $runtime $_) })
$packageFiles += @(Get-ChildItem -LiteralPath (Join-Path $runtime 'help') -File -Recurse |
    Where-Object { $_.Extension -notin @('.pdb','.xml','.log') })
foreach ($locale in @('de','en','fr')) {
    $packageFiles += Get-Item -LiteralPath (Join-Path $runtime "$locale/INI Edit Translate 2026.resources.dll")
}
$packageFiles | ForEach-Object {
    $relative = $_.FullName.Substring($runtime.Length).TrimStart('\','/')
    $target = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $target
}
foreach ($filename in @('LICENSE','README.md','README.en.md','README.fr.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $filename) -Destination $stage
}
New-Item -ItemType Directory -Path (Join-Path $stage 'docs/images') -Force | Out-Null
Copy-Item -Path (Join-Path $repoRoot 'docs/images/*.png') -Destination (Join-Path $stage 'docs/images')
foreach ($filename in @('dictionary-review.md','dictionary-review.json')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/$filename") -Destination (Join-Path $stage 'docs')
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'Info_trans/reference-data/README.md') -Destination (Join-Path $stage 'help/REFERENCE-DATA.md')
# Always ship the untouched source database, rather than an opened runtime copy.
Copy-Item -LiteralPath (Join-Path $repoRoot 'Info_trans/reference-data/IET2.FDB') -Destination (Join-Path $stage 'help/IET2.FDB') -Force
$required = @('INI Edit Translate 2026.exe','INI Edit Translate 2026.exe.config','IET.Updater.exe','IniFile.dll','Info.rtf',
    'Newtonsoft.Json.dll','help/Info_trans.exe','help/Info_trans.exe.config','help/FirebirdSql.Data.FirebirdClient.dll',
    'help/iconhelp.csv','help/twb.csv','help/ws_D_E.csv','help/ws_D_E_F.csv','help/object.csv',
    'docs/dictionary-review.md','docs/dictionary-review.json',
    'help/estate.csv','help/goods.csv','help/landscape.csv','help/stock.csv','help/signals.csv',
    'help/licenses/Firebird-provider-license.txt','help/IET2.FDB','help/iconhelp.dat',
    'help/fbembed.dll','help/fbclient.dll','help/ib_util.dll','help/firebird.conf','help/firebird.msg',
    'help/icudt30.dll','help/icuin30.dll','help/icuuc30.dll','help/msvcp80.dll','help/msvcr80.dll',
    'help/Microsoft.VC80.CRT.manifest','help/intl/fbintl.dll','help/intl/fbintl.conf')
foreach ($locale in @('de','en','fr')) {
    $required += "Info.$locale.rtf"
    $required += "$locale/INI Edit Translate 2026.resources.dll"
    $required += "help/$locale/Info_trans.resources.dll"
}
foreach ($filename in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $stage $filename))) { throw "Missing release file: $filename" }
}
if ((Get-FileHash -LiteralPath (Join-Path $stage 'Info.rtf')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $repoRoot 'Info.rtf')).Hash) {
    throw 'Packaged Info.rtf differs from original'
}
if ((Get-FileHash -LiteralPath (Join-Path $stage 'help/IET2.FDB')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $repoRoot 'Info_trans/reference-data/IET2.FDB')).Hash) {
    throw 'Packaged database differs from original'
}
if ((Get-FileHash -LiteralPath (Join-Path $stage 'help/ws_D_E_F.csv')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $repoRoot 'Info_trans/reference-data/ws_D_E_F.csv')).Hash) {
    throw 'Packaged dictionary differs from the reviewed source'
}
if (Get-ChildItem -LiteralPath $stage -File -Recurse | Where-Object { $_.Name -in @('openai.json','user.config','key2019.dat') }) {
    throw 'Personal configuration found in release staging'
}
[xml]$configuration = Get-Content -LiteralPath (Join-Path $stage 'INI Edit Translate 2026.exe.config')
foreach ($setting in $configuration.SelectNodes('//setting')) {
    if ($setting.name -in @('sshname','sshpw','sshhost','key1','key2','username','regstring_n','regstring_k') -and
        -not [string]::IsNullOrWhiteSpace($setting.value.InnerText)) { throw 'Nonempty private default' }
}
$zip = Join-Path $outputRoot "INI-Edit-Translate-2026-v$shortVersion-windows.zip"
if (Test-Path -LiteralPath $zip) { throw "Package already exists: $zip" }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $outputRoot 'SHA256SUMS.txt'), "$hash  $([IO.Path]::GetFileName($zip))" + [Environment]::NewLine)
Write-Output $zip
Write-Output "SHA256 $hash"
