# INI Edit Translate 2026

## Version 2.1.46: fertige Stream-Texte erhalten

Fehlt der Übersetzungstext im letzten Abschlussereignis, berücksichtigt die App nun
die zuvor abgeschlossenen `response.output_item.done`, `response.content_part.done`
und `response.output_text.done`. Übernommen wird erst nach `response.completed` mit
Status `completed`. Bis dahin gesammelte Text-Deltas können erst dann die vollständige
Sprach-/JSON-Prüfung durchlaufen. Deltas ohne Gesamtabschluss, Fehler, Abbrüche, Ablehnungen und lückenhafte
Textteile werden nicht als erfolgreiche Übersetzung verwendet.

Eine weiterhin leere Antwort bekommt eine eigene Meldung mit Transport und festen
Ereigniszählern, ohne Antworttexte, Tokens oder Benutzerkennungen. Der konkrete
Ereignisverlauf auf dem entfernten Rechner muss noch bestätigt werden. 78 lokale
Prüfungen bestanden ohne echte API-Aufrufe. Modellnamen und Originaldaten bleiben erhalten.

[OpenAI Docs zu Streaming-Ereignissen](https://developers.openai.com/api/reference/resources/responses/streaming-events).


## Version 2.1.45: JSON-Ausgabe der Übersetzung

Die Anweisung fordert nun ausdrücklich ein JSON-Objekt mit den drei Zielsprachen
an; das strenge Schema bleibt aktiv. Ein einzelner JSON-Markdown-Codeblock darf
entfernt werden, bevor dieselbe vollständige Sprachprüfung erfolgt. Erklärtexte,
beschädigtes JSON, doppelte oder zusätzliche Sprachschlüssel werden nicht übernommen.

Statt eines allgemeinen JsonReaderException zeigt die Diagnose, ob die Übersetzungsausgabe,
die API-Antwort, die Tokenantwort oder die Identitätsschlüssel betroffen sind.
Es werden keine Originalantworten oder Zugangsdaten angezeigt. 75 lokale Prüfungen
bestanden ohne echte API-Aufrufe. Der genaue Ursprung des gemeldeten Fehlers muss
auf dem entfernten Rechner bestätigt werden. Modellnamen bleiben unverändert.


## Version 2.1.44: Antwortformat bei ChatGPT-Übersetzung

Die App fordert den Ereignisstream ausdrücklich an und prüft den Antwortinhalt,
statt ihn allein wegen eines fehlenden oder abweichenden Content-Type abzulehnen.
Gültige SSE-Abschlussereignisse und vollständig abgeschlossene Responses-JSON-Objekte
werden verarbeitet. HTML, Fehlerobjekte, unvollständige Ergebnisse und zu große
Antworten ersetzen keine Texte. Die kopierbare Diagnose nennt eine feste Kategorie
für Antworttyp und Inhalt; Tokens und rohe Servertexte bleiben ausgeschlossen.

Die Anfrage bleibt `store: false`, `stream: true`, mit `input` als Array gemäß
[OpenAI Docs](https://developers.openai.com/siwc/token-sharing-open-source/preview-limitations).
73 lokale Prüfungen und 43 Updater-Prüfungen bestanden ohne echte API-Aufrufe.
Welche Antwort der entfernte Rechner tatsächlich liefert, muss der nächste Test zeigen.


## Version 2.1.43: Update bei verweigertem Prozesszugriff

Der Updater behandelt „Zugriff verweigert“ beim Warten auf das Ende des Editors.
Er fährt nur fort, wenn die lokale Prozessliste bestätigt, dass die Prozess-ID nicht
mehr existiert. Solange der Prozess vorhanden ist oder die Prüfung fehlschlägt,
beginnt keine Installation. Die normale Identitätsprüfung bleibt erhalten.
Künftige Updates starten den Updater aus dem erneut geprüften neuen Paket.

**Einmalige Reparatur bei betroffenem altem Updater:** Editor schließen, die einzelne
`IET.Updater.exe` aus dem GitHub-Release herunterladen und nur diese Datei neben der
Editor-EXE ersetzen (vorher die bisherige Datei sichern). Danach den Editor starten
und **Über → Nach Updates suchen** wählen. Daten, Wörterbücher und Einstellungen
nicht mit dem kompletten ZIP überschreiben. Die einzelne EXE ist ebenfalls in
`SHA256SUMS.txt` aufgeführt.


## Version 2.1.42: Modellnamen erhalten

Die Übersetzung lässt alle vier Modellnamen in Modell-INI und Installation.eep
unverändert. Nur Beschreibungen, Bewegungsachsen und Signaltexte werden übersetzt;
Namen werden nicht an OpenAI gesendet. Die gewollte Umwandlung von Unterstrichen
in Leerzeichen beim Einlesen bleibt erhalten. Falsche Sprach-/Abschnittszuordnungen
bei Installationsbeschreibungen sind korrigiert.

Auch Fehler während der eigentlichen Übersetzung können jetzt über **Fehler kopieren**
übernommen werden. Streamfehler zeigen geprüfte Fehlercodes; ungültige Antwortformate
haben eigene Meldungen. Tokens und rohe Servertexte werden nicht angezeigt. Die
Ursache des gemeldeten ChatGPT-Fehlers ist weiterhin nicht bestätigt.


## Version 2.1.41: Diagnose bei Anmeldung und Modellabruf

Fehler nennen jetzt den betroffenen Schritt. Eine bestätigte Anmeldung wird von einem
anschließenden Fehler beim Modellabruf unterschieden. **Fehler kopieren** kopiert die
Meldung mit HTTP-Status, geprüftem Fehlercode und Anfrage-ID, ohne Tokens oder rohe
Servertexte. Kontobeschränkungen, vorübergehende Nichtverfügbarkeit, Netzwerkfehler
und ein unerwartetes Modelllistenformat erhalten eigene Meldungen in DE/EN/FR.
Dies grenzt den gemeldeten Fehler auf dem entfernten Rechner ein; dessen Ursache
ist noch nicht bestätigt. Einstellungen, API-Schlüssel und Verbindungen bleiben erhalten.

[Windows-ZIP v2.1.46](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.46/INI-Edit-Translate-2026-v2.1.46-windows.zip) · [Release und SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.46)

## Version 2.1.40: ChatGPT-Anmeldung

Die Korrektur der Programmpfad-Eingabe aus 2.1.39 ist enthalten.

Unter **Optionen → OpenAI-Übersetzung** lässt sich der Zugang auswählen:
**OpenAI-API · separate Abrechnung** oder **ChatGPT-Abo verwenden**.
Für die Abo-Nutzung **Mit ChatGPT fortfahren** wählen, im Standardbrowser anmelden
und die Abo-Nutzung selbst freigeben. Anschließend ein für das Konto verfügbares
Modell wählen und **Speichern**. **Anderes Konto** ergänzt eine getrennte Verbindung;
**Abmelden** beendet nur die gewählte Verbindung.

Ausgewählte Modelltexte werden an OpenAI gesendet. Die Abo-Variante nutzt das
vorhandene ChatGPT-Kontingent und die erteilte App-Freigabe; sie hat kein eigenes
unbegrenztes Kontingent. **Nutzung verwalten** öffnet die ChatGPT-Nutzungsübersicht.
Berechtigung und Verfügbarkeit werden bei der echten Anmeldung geprüft.
Bei Fehlern oder Abo-Limits gibt es keinen automatischen Wechsel zur kostenpflichtigen API.

API-Schlüssel bleiben in der bestehenden Konfiguration erhalten. Neue OAuth-Zugangsdaten
liegen separat, mit Windows-DPAPI verschlüsselt, unter
`%LOCALAPPDATA%/UGSo/IET2026/chatgpt-session.dat`.
Die Abo-Anmeldung benötigt keinen API-Schlüssel. HTTP-429-Meldungen unterscheiden nun
bekannte API-Kontingent- und Anfragelimit-Fehler.

63 lokale Prüfungen mit simulierten OpenAI-Antworten und einem eigenen lokalen
Anmelderückweg bestanden; keine echte Übersetzung mit einem ChatGPT-Konto ausgelöst.
Die echte Kontoanmeldung, Freigabe und Übersetzung sind anschließend noch zu prüfen.
[Release und Prüfergebnisse](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.40).

Offizielle Grundlagen: [Anmeldung](https://developers.openai.com/siwc/token-sharing-open-source/sign-in),
[Modelle und Streaming](https://developers.openai.com/siwc/token-sharing-open-source/models-and-inference),
[Vorschaugrenzen](https://developers.openai.com/siwc/token-sharing-open-source/preview-limitations).

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

**Version 2.1.46.0. Manuelle GitHub-Updates mit bestätigter Installation.** Es gibt keine automatische Updateprüfung beim Start. Google, LEO,
Linguee und DeepL werden nicht mehr als Übersetzungsdienste angeboten.

## Download

[Windows-ZIP v2.1.46 herunterladen](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.46/INI-Edit-Translate-2026-v2.1.46-windows.zip) · [GitHub-Release und SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.46)

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

Der Info-Dialog wählt die geprüfte Hilfe `Info.de.rtf`, `Info.en.rtf` oder `Info.fr.rtf` passend zur Oberflächensprache. Die historische `Info.rtf` bleibt unverändert als Original erhalten. Die aktuellen Hilfetexte beschreiben OpenAI und die wiederhergestellte Nachschlagehilfe.

## GitHub-Updates

Unter **Über → Nach Updates suchen** wird das neueste öffentliche GitHub-Release
abgefragt. Es gibt keine automatische Prüfung beim Programmstart. Nur eine neuere
stabile Version wird angeboten; Versionsnummer und Release-Notizen werden angezeigt.

**Herunterladen und installieren** lädt die Windows-ZIP samt SHA-256-Prüfsumme.
Download und Paketprüfung sind abbrechbar und erfolgen vor dem Schließen der Anwendung.
Vor der Installation müssen offene Dateien gespeichert und Info_trans geschlossen sein.
Die Installation startet erst nach Bestätigung. Ein separates Hilfsprogramm wartet auf
das Ende des Editors, sichert die bisherigen Dateien und installiert das Paket.

Einstellungen und der verschlüsselte OpenAI-Schlüssel bleiben erhalten. Vorhandene
IET2.FDB-, CSV-, DAT- und Konfigurationsdateien der Nachschlagehilfe sowie die ursprüngliche
Info.rtf werden gesichert und nicht ersetzt. Eigene Modelldateien bleiben unangetastet.
Bei einem Installationsfehler wird die Wiederherstellung der bereits ersetzten Dateien
versucht; ein Bericht nennt das Ergebnis. Sicherungen und Berichte liegen unter
`%LOCALAPPDATA%/UGSo/IET2026/updates/<Kennung>/`.

Der Updater benötigt keine Serverzugangsdaten. Prüfsummen dienen der Integritätsprüfung;
es ist keine zusätzliche digitale Paketsignatur implementiert.
2.1.38 korrigiert die Prozessübergabe beim Update, auch zwischen x86 und x64.
Falls der alte 2.1.37-Updater vor der Installation scheitert, das vollständige
2.1.38-ZIP zunächst in einen neuen Ordner entpacken. Eigene Daten vorher sichern;
vorhandene Datenbanken und Wörterbücher nicht durch Paketdateien ersetzen.

36 isolierte Updateprüfungen decken Download, Abbruch, Paketpfade, Datenerhalt,
gesperrte Dateien und Rücknahme nach einem teilweisen Schreibfehler ab.

## Zweites Programm: Info_trans

Das Paket enthält auch `help/Info_trans.exe`. **Nachschlagehilfe** im Haupteditor
öffnet dieses separate Programm in der gewählten Sprache. Die echte `IET2.FDB`
und die zugehörige native Firebird-2.5.8-Laufzeit wurden aus dem Archiv von 2019
übernommen. Eine separate Firebird-Serverinstallation ist nicht nötig.

Die Datenbank enthält 5 Immobilien- und 2 Gütereinträge sowie 2 Updateeinträge.
Die Tabellen für Signale, Rollmaterial, Gleisobjekte und Landschaft sind im
Original leer. Zusätzlich sind 236 Iconeinträge, 369 technische Begriffe und
2.428 bereinigte Wörterbucheinträge enthalten. Das neue „Wörterbuch DE/EN/FR“
ergänzt Französisch und korrigiert deutsche Begriffe anhand der englischen Quelle.
Die ursprüngliche DE/EN-Datei bleibt erhalten. Alle offenen Rückfragen sind geklärt;
die bestätigten Korrekturen stehen in [der Prüfliste](docs/dictionary-review.md).

`Info_trans` wird passend zu den nativen DLLs für x86 gebaut. Die verwaltete DLL
FirebirdSql.Data.FirebirdClient 7.5.0.0 wurde mit der echten Firebird-2.5.8-Datenbank
geprüft. Tests lesen nur getrennte Datenbankkopien und vergleichen alle sieben
Tabellen mit der Anzeige in DE/EN/FR. Das Paket enthält die unveränderte Quelldatenbank.

Fehlende oder nicht ladbare Bestandteile werden angezeigt. SQL-Import bleibt dann
gesperrt; vor einem verfügbaren manuellen SQL-Import wird die Datenbank lokal gesichert.
Die automatische Programmaktualisierung bleibt deaktiviert.

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

Debug- und Release-Build mit Visual Studio 2026/.NET Framework 4.8. 41 lokale Prüfungen
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
