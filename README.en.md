# INI Edit Translate 2026

[Deutsch](README.md) · [Français](README.fr.md) · [Documentation](https://opensource.ugso-software.de/en/eep/ini-edit-translate-2026/)

Windows editor and ChatGPT translator for EEP model INI files and `Installation.eep`,
developed by Uwe Grimm / UGSo Software. Version **2.1.35.0**.

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

**Updates are currently disabled**, including the automatic connection and update menu.
Google, LEO, Linguee and DeepL are no longer translation providers.

## Download

[Download Windows ZIP v2.1.35](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.35/INI-Edit-Translate-2026-v2.1.35-windows.zip) · [GitHub release and SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.35)

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

## Data and verification

Configuration is under `%LOCALAPPDATA%/UGSo/IET2026/openai.json`; the key is encrypted
with Windows DPAPI for the current Windows account. Environment variables
`IET_OPENAI_API_KEY` and `OPENAI_API_KEY` are also supported. Backups retain the original
filename under `%LOCALAPPDATA%/UGSo/IET2026/backups/`. Selected texts are sent to OpenAI
with `store: false`. Legacy application data is not automatically migrated or overwritten.

30 local checks cover mocked API responses, cancellation, errors, placeholders,
restoration, export and rendered DE/EN/FR interfaces. No live API calls are used.
Run `MSBuild.exe tests/IET.LocalTests.csproj /t:Build`, then `tests/bin/IET.LocalTests.exe`.
Review saved files; not every possible INI schema is supported.

## Licence

Continuation of [IET-Release-Nov19_02](https://github.com/rockbaer2007/IET-Release-Nov19_02),
released under the [MIT licence](LICENSE). The new repository does not import the old
Git history, server credentials, activation files or personal databases.
EEP belongs to its respective rights holder; this is an independent tool.
