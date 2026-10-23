# INI Edit Translate 2026

**EEP-Modellbeschreibungen bearbeiten und mit ChatGPT übersetzen.**

[English](README.en.md) · [Français](README.fr.md) · [Funktionsbeschreibung](https://opensource.ugso-software.de/eep/ini-edit-translate-2026/)

Windows-Anwendung für EEP-Modell-INI-Dateien und `Installation.eep`, entwickelt
von Uwe Grimm / UGSo Software. Die 2026-Fassung führt den bisherigen INI-Editor
mit OpenAI-Übersetzung und bereinigter Oberfläche fort.

![Deutsche Programmoberfläche ohne private Inhalte](docs/images/editor-de.png)

## Funktionen

- Modellnamen und Beschreibungen für Englisch, Deutsch, Französisch und Polnisch bearbeiten.
- Bewegungsachsen, Signalfunktionen, PDF-Verweise und Modellmetadaten anzeigen und bearbeiten.
- Installationsabschnitte aus `Installation.eep` bearbeiten.
- Originaldatei und erzeugte Dateivorschau vergleichen.
- Texte mit der OpenAI Responses API direkt aus der Ausgangssprache in die drei anderen Sprachen übersetzen.
- Übersetzungen abbrechen; bei Fehlern oder veränderten Platzhaltern bisherige Texte wiederherstellen.
- Vor dem Überschreiben einer vorhandenen INI-Datei automatisch eine Sicherung anlegen.
- Tabellen vollständig als tabulatorgetrennte UTF-8-Dateien mit BOM exportieren.
- Oberfläche mit geprüften deutschen, englischen und französischen Texten.

**Version 2.1.35.0. Die Updatefunktion ist vorerst deaktiviert.** Es gibt keine
automatische Updateverbindung und keinen Update-Menüpunkt. Google, LEO,
Linguee und DeepL werden nicht mehr als Übersetzungsdienste angeboten.

## Download

[Windows-ZIP v2.1.35 herunterladen](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.35/INI-Edit-Translate-2026-v2.1.35-windows.zip) · [GitHub-Release und SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.35)

ZIP vollständig entpacken und `INI Edit Translate 2026.exe` starten. Windows 11 mit
.NET Framework 4.8 genügt; Visual Studio ist für die fertige Anwendung nicht nötig.
DLLs und Sprachordner zusammen lassen. Das Paket benötigt keinen Installer.

## Voraussetzungen und Build

- Windows 11 und .NET Framework 4.8.
- Visual Studio 2026 mit „.NET-Desktopentwicklung“ und .NET Framework 4.8 Developer Pack.
- NuGet-Paketwiederherstellung für Newtonsoft.Json; die bisherigen INI-Bibliotheken liegen im Projekt.

```powershell
nuget restore "INI Edit Translate 2026.sln"
MSBuild.exe "INI Edit Translate 2026.sln" /t:Build /p:Configuration=Release
```

Danach `Ini Edit - Translate/bin/Release/INI Edit Translate 2026.exe` starten.
Beim Weitergeben den gesamten Ausgabeordner einschließlich DLLs und Sprachordnern
verwenden. Ein vorgefertigtes Installationspaket ist in diesem Repository nicht enthalten.

## Erste Schritte

1. Unter **Über → Einstellungen** die gewünschte Sprache und den Bearbeitungsmodus wählen.
2. **OpenAI-Übersetzung** öffnen und den eigenen API-Schlüssel sowie das Modell eintragen.
   Voreinstellung: `gpt-4.1-mini`. Ein leeres Schlüsselfeld erhält den bisherigen Schlüssel.
3. Eine eigene Datei zunächst mit **Nur einlesen** öffnen und die Inhalte prüfen.
4. Zum Übersetzen den Modus **Einlesen und übersetzen** verwenden oder die entsprechenden
   Übersetzungsschaltflächen betätigen. Bei Bewegungsachsen und Signalfunktionen die
   vorhandene Ausgangssprache auswählen.
5. Ergebnis in der Vorschau prüfen und bewusst speichern.

Die API erfordert Internet und eigene OpenAI-API-Abrechnung. Ein ChatGPT-Abonnement
ersetzt diese API-Abrechnung nicht. Ein echter API-Aufruf und die sprachliche Qualität
der erzeugten Modelltexte müssen mit dem eigenen Zugang geprüft werden.

## Daten und Sicherungen

- OpenAI-Konfiguration: `%LOCALAPPDATA%/UGSo/IET2026/openai.json`.
- Der API-Schlüssel wird mit Windows-DPAPI für das aktuelle Windows-Konto verschlüsselt.
- Optionaler Schlüssel über `IET_OPENAI_API_KEY` oder `OPENAI_API_KEY`.
- Dateisicherungen: `%LOCALAPPDATA%/UGSo/IET2026/backups/<Zeitstempel>/<Originaldateiname>`.
- Nur die ausdrücklich übersetzten Texte werden an OpenAI gesendet; die API-Anfrage verwendet `store: false`.
- Vorhandene Daten und Registrierungen der alten Anwendung werden nicht automatisch migriert oder überschrieben.

Dateien und Projekte vor der ersten Bearbeitung zusätzlich sichern. Die Anwendung
erkennt nicht jedes denkbare INI-Schema und ersetzt keine inhaltliche Prüfung des Modells.

## Prüfstand

Debug- und Release-Build mit Visual Studio 2026/.NET Framework 4.8. 30 lokale Prüfungen
mit simulierten API-Antworten: Abbruch, HTTP-Fehler, Antwortprüfung, Platzhalter, Cache,
DPAPI, Wiederherstellung, Tabellenexport und gerenderte Oberflächen für DE/EN/FR.
Keine echten API-Aufrufe in der Testsuite.

```powershell
MSBuild.exe tests/IET.LocalTests.csproj /t:Build
./tests/bin/IET.LocalTests.exe
```

Die neue Veröffentlichung enthält keine alte Git-Historie, Serverzugänge,
Registrierungsdateien oder persönliche Datenbanken. Die bisherigen Projekte bleiben erhalten.

## Herkunft und Lizenz

Fortführung von [IET-Release-Nov19_02](https://github.com/rockbaer2007/IET-Release-Nov19_02).
MIT-Lizenz; ursprünglicher Copyright-Hinweis in [LICENSE](LICENSE).
EEP ist ein Produkt seines jeweiligen Rechteinhabers; dieses Werkzeug ist eine unabhängige Entwicklung.

[Offizielle OpenAI-Dokumentation](https://developers.openai.com/api/docs/guides/text)
