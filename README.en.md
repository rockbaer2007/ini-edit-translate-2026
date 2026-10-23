# INI Edit Translate 2026

[Deutsch](README.md) · [Français](README.fr.md) · [Documentation](https://opensource.ugso-software.de/en/eep/ini-edit-translate-2026/)

Windows editor and ChatGPT translator for EEP model INI files and `Installation.eep`,
developed by Uwe Grimm / UGSo Software. Version **2.1.37.0**.

![English application interface without personal data](docs/images/editor-en.png)

## Features

- Edit model names and descriptions in English, German, French and Polish.
- Work with movement axes, signal functions, PDF references and model metadata.
- Edit installation sections and compare the original file with the output preview.
- Translate directly from the source language into the other three languages using OpenAI.
- Cancel translation; reject incomplete responses and changed placeholders while preserving existing texts.
- Automatically back up existing INI files before overwriting them.
- Export complete tables as tab-separated UTF-8 files with a BOM.
- Reviewed German, English and French interface texts.

**Manual GitHub updates with confirmed installation are available.** There is no automatic startup check.
Google, LEO, Linguee and DeepL are no longer translation providers.

## Download

[Download Windows ZIP v2.1.37](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.37/INI-Edit-Translate-2026-v2.1.37-windows.zip) · [GitHub release and SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.37)

Extract the complete ZIP and run `INI Edit Translate 2026.exe`. Requires Windows 11
and .NET Framework 4.8; Visual Studio is only needed to build from source.
Keep DLLs and language folders together. No installer is required.

## Build and use

Windows 11, Visual Studio 2026, the .NET desktop workload and .NET Framework 4.8 Developer Pack.

```powershell
nuget restore "INI Edit Translate 2026.sln"
MSBuild.exe "INI Edit Translate 2026.sln" /t:Build /p:Configuration=Release
```

Run `Ini Edit - Translate/bin/Release/INI Edit Translate 2026.exe`. Keep its DLLs
and language directories together. This repository does not include a ready-made installer.

Open **About → Settings → OpenAI translation** and enter your own API key and model
(default: `gpt-4.1-mini`). Start by opening a backup copy in **Read only** mode.
Choose **Read and translate** or use the translation buttons when ready. Review the
source language and output preview before saving.

OpenAI requires internet access and separate API billing. A ChatGPT subscription does
not replace API billing. Live API access and generated translation quality still need
verification with your own account.

The information dialog selects the reviewed `Info.de.rtf`, `Info.en.rtf` or `Info.fr.rtf` to match the interface language. The historical `Info.rtf` is retained unchanged. Current help describes OpenAI translation and the restored reference program.

## GitHub updates

Choose **About → Check for updates** to query the latest public GitHub release.
There is no automatic startup check. Only a newer stable version is offered,
with its version number and release notes.

**Download and install** downloads the Windows ZIP and SHA-256 checksum.
Download and package validation can be cancelled and finish before the application closes.
Save open files and close Info_trans before installation. Installation requires confirmation.
A separate helper waits for the editor to exit, backs up existing files and installs the package.

Settings and the encrypted OpenAI key are retained. Existing reference-help FDB, CSV,
DAT and configuration files, plus the original Info.rtf, are backed up and not replaced.
Your model files are left intact. On installation failure, the updater attempts to restore
files already replaced and records the result. Backups and reports are stored under
`%LOCALAPPDATA%/UGSo/IET2026/updates/<identifier>/`.

No server credentials are needed. Checksums verify integrity; no additional digital
package signature is implemented. 32 isolated update checks cover downloads,
cancellation, package paths, retained data, locked files and rollback after partial writes.

## Second program: Info_trans

The package also includes `help/Info_trans.exe`. **Reference help** in the main
editor opens it in the selected language. The actual `IET2.FDB` and its native
Firebird 2.5.8 runtime were restored from the 2019 archive.
No separate Firebird server installation is required.

The database contains 5 building entries, 2 goods entries and 2 update entries.
Signals, rolling stock, track objects and landscape tables are empty in the
original. Also included: 236 icon entries, 369 technical terms and 2,428 reviewed
dictionary entries. The new DE/EN/FR dictionary adds French and corrects German
terms using English as the reference. The original DE/EN file is retained.
All outstanding source questions are resolved; confirmed corrections are recorded
in the [review list](docs/dictionary-review.md).

`Info_trans` is built for x86 to match the native DLLs. The managed provider
FirebirdSql.Data.FirebirdClient 7.5.0.0 was verified with the real Firebird 2.5.8
database. Tests read separate database copies and compare all seven tables with
the DE/EN/FR interface. Packaging includes the unchanged source database.

Missing or unloadable components are reported and SQL import is then disabled.
A local database backup precedes an available manual SQL import.
Automatic application updates remain disabled.

## Data and verification

Configuration is under `%LOCALAPPDATA%/UGSo/IET2026/openai.json`; the key is encrypted
with Windows DPAPI for the current Windows account. Environment variables
`IET_OPENAI_API_KEY` and `OPENAI_API_KEY` are also supported. Backups retain the original
filename under `%LOCALAPPDATA%/UGSo/IET2026/backups/`. Selected texts are sent to OpenAI
with `store: false`. Legacy application data is not automatically migrated or overwritten.

41 local checks cover mocked API responses, cancellation, errors, placeholders,
restoration, export and rendered DE/EN/FR interfaces. No live API calls are used.
Run `MSBuild.exe tests/IET.LocalTests.csproj /t:Build`, then `tests/bin/IET.LocalTests.exe`.
Review saved files; not every possible INI schema is supported.

## Licence

Continuation of [IET-Release-Nov19_02](https://github.com/rockbaer2007/IET-Release-Nov19_02),
released under the [MIT licence](LICENSE). The new repository does not import the old
Git history, server credentials, activation files or personal databases.
EEP belongs to its respective rights holder; this is an independent tool.
