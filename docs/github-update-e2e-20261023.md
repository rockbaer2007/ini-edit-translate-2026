# GitHub-Updateprüfung vom 23.10.2026

## Ergebnis

Der veröffentlichte Updater 2.1.37 scheiterte beim realen Prozessübergang mit
`Win32Exception: ReadProcessMemory` in `Process.MainModule`. Der Editor war dabei
bereits im Beenden. Vor der Fehlermeldung wurde keine der 63 Installationsdateien
verändert. Das veröffentlichte ZIP bleibt unverändert.

Eine lokale Korrektur verwendet einen stabilen Windows-Prozesshandle mit
`QueryFullProcessImageName`, `GetProcessTimes` und `WaitForSingleObject`. Sie prüft
weiterhin Programmpfad und Startzeit, wartet auf das Ende und beendet keinen
fremden Prozess. Vier neue Prüfungen decken echte x86-/x64-Prozesse, bereits
beendete Prozesse, falsche Identität und Zeitüberschreitung ab. Insgesamt bestehen
36 Updateprüfungen. Die Korrektur wurde separat als 2.1.38 veröffentlicht;
das öffentliche 2.1.37-ZIP bleibt unverändert.

## Echter Wiederholungstest

- Ausgangspunkt: isolierter Testbuild 2.1.36 aus dem 2.1.37-Quellstand; keine
  historische offizielle 2.1.36-Veröffentlichung wird damit behauptet.
- Ziel: unverändertes öffentliches GitHub-Release 2.1.37 mit SHA-256
  `7b56d05806102ba0310e7218b45056445bec929ae5b542564c90a1f7dd852e16`.
- Vollbackup der Testinstallation und Sicherung der 16 vorhandenen
  Benutzerkonfigurationen vor dem Test.
- Mit lokal korrigiertem Ausgangshelfer: echter GitHub-Download, Paketprüfung,
  Installationsbestätigung, Dateisicherung, Installation und automatischer
  Neustart erfolgreich. Der installierte Editor ist bytegleich zum Release.
- Erneute Prüfung im gestarteten Editor: installiert 2.1.37.0, GitHub 2.1.37.0,
  kein neueres Release; Installationsschaltfläche deaktiviert.
- Alle zuvor gespeicherten Einstellungswerte übernommen, 17 Daten-/Konfigurations-
  und Modelldateien unverändert; eigener Wörterbucheintrag bleibt erhalten.
- Kein echter OpenAI-Schlüssel war hinterlegt. Ein ausdrücklich synthetischer,
  mit Windows-DPAPI verschlüsselter Testwert blieb bytegleich und nach dem
  Neustart entschlüsselbar. Danach wurde ausschließlich diese temporäre
  Testkonfiguration entfernt und der ursprüngliche Zustand ohne Schlüssel
  wiederhergestellt. Keine OpenAI-Anfrage wurde ausgelöst.
- Alle 16 ursprünglichen Benutzerkonfigurationen bleiben bytegleich. Die
  Arbeitsinstallation und die Quelldatenbank wurden nicht verändert.

Der erfolgreiche Wiederholungstest belegt die lokale Helferkorrektur. Er ist
keine nachträgliche Freigabe des fehlerhaften veröffentlichten 2.1.37-Helfers.

## Veröffentlichung und vollständiger Ablauf 2.1.38

- Release-Commit: `74612cd5831b679e111870dcacac146e1a846c46`.
- Öffentliches ZIP: SHA-256
  `d23297f608f4ebffc2de1a0cceeea87d96ebadb3cfc41256e0cc26c0c0de8d57`.
- Frische lokale Sicherung der Testinstallation und aller 18 vorhandenen
  Benutzerkonfigurationen vor diesem Test; Arbeitsinstallation unverändert.
- Ausgangspunkt diesmal: unveränderte veröffentlichte 2.1.37-Programmdateien
  einschließlich des alten Helfers. Dieser konkrete Versuch gelang trotz des
  zuvor nachgewiesenen Übergabefehlers; daraus folgt keine allgemeine Freigabe
  des alten Helfers. Der manuelle Umstiegsweg bleibt in DE/EN/FR dokumentiert.
- Echter GitHub-Download und Paketprüfung, vom Nutzer bestätigte Installation,
  Installationsbericht `Installed 2.1.38.0`, Erfolgsmeldung und automatischer
  Neustart mit neuer Prozess-ID erfolgreich.
- Editor, Info_trans und installierter neuer Helfer sind bytegleich zum Release.
  Alle 32 bisherigen Einstellungswerte wurden nach 2.1.38 übernommen.
- Alle 17 geprüften Daten-/Modell-/Konfigurationsdateien und alle 18 vorherigen
  Benutzerkonfigurationen bleiben unverändert. Der eigene Wörterbucheintrag bleibt erhalten.
- Der synthetische DPAPI-Testwert blieb bytegleich und entschlüsselbar; anschließend
  nur die temporäre Testkonfiguration entfernt, ursprünglicher Zustand ohne Key wiederhergestellt.
  Kein echter API-Schlüssel verwendet, keine OpenAI-Anfrage ausgelöst.
- 41 lokale Prüfungen, 36 Updateprüfungen, reale Datenbanktests DE/EN/FR und
  Annahme des vollständigen 2.1.38-Pakets durch den Updater bestanden.
- Die Windows-Steuerung konnte nach dem Neustart das neue Fenster auflisten,
  dessen Zustand aber auch beim Wiederholungsversuch nicht erfassen
  (`foreground window did not report a process id`). Neustart und Migration
  wurden deshalb anhand Prozess-ID, Dateiversionen, Hashes und Konfiguration geprüft.
