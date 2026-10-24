# Railway glossary / Bahn-Abkürzungen / Abréviations ferroviaires

## Version 2.1.53: Bahn-Abkürzungen und Konstrukteurskennungen

20 Bahn-Abkürzungen werden jetzt anhand einer festen lokalen Zuordnung übersetzt:
in Modellnamen sowie in den über den gewählten Anbieter übersetzten Texten.
EW bedeutet Einfachweiche, auch ohne folgende Gradzahl.
Beispiel: `GK3 DKW DR DB (V8)[e]EEP16.2` wird auf Französisch
zu `GK3 TJD DR DB (V8)[e]EEP16.2`.

Die mitgelieferte Startliste enthält 272 eindeutige dreistellige Konstrukteurskennungen.
Beim ersten Programmstart wird eine externe UTF-8-Datei angelegt:
`%LOCALAPPDATA%\UGSo\IET2026\constructor-codes.txt`.
Neue Kennungen dort mit zwei Buchstaben und einer Ziffer, jeweils eine pro Zeile,
ergänzen und das Programm neu starten. Updates überschreiben die Benutzerdatei nicht.
Ergänzungen werden mit der mitgelieferten Grundliste zusammengeführt. Leerzeilen,
Kommentare und ungültige Zeilen werden ignoriert; unlesbare Dateien bleiben erhalten
und die Grundliste dient als Rückfallebene. Kennungen bleiben mit und ohne Klammern an jeder Position erhalten.
Die bisherige allgemeine Erkennung weiterer Kennungen bleibt zusätzlich aktiv.
DR, DB, Typbezeichnungen, Versionshinweise wie `(V8)` oder `(v8.2)`,
EEP-Versionen und `[e]` bleiben unverändert.

| DE | EN | FR | PL |
| --- | --- | --- | --- |
| DKW | DSS | TJD | Rkpd |
| EKW | SS | TJS | Rkp |
| EW | single turnout | BS | Rz |
| ABW | contrary-flexure turnout | ABW* | Rłd |
| IBW | similar-flexure turnout | IBW* | Rłj |
| BW | locomotive depot | dépôt de locomotives | lokomotywownia |
| Bf | station | gare | stacja |
| Hbf | main station | gare principale | dworzec główny |
| Hp | halt | halte ferroviaire | przystanek osobowy |
| Gbf | freight station | gare de marchandises | stacja towarowa |
| Rbf | marshalling yard | gare de triage | stacja rozrządowa |
| Abzw | junction | bifurcation | posterunek odgałęźny |
| BÜ | level crossing | passage à niveau | przejazd kolejowo-drogowy |
| Stw | signal box | poste d’aiguillage | nastawnia |
| EG | station building | bâtiment voyageurs | budynek dworca |
| OL | overhead line equipment | caténaire | sieć trakcyjna |
| Gl. | track | voie | tor |
| Bstg. | platform | quai | peron |
| DWW | three-way turnout | branchement à trois voies | rozjazd potrójny |
| Kr. | diamond crossing | traversée de voies | skrzyżowanie torów |

*Für ABW/IBW bleibt Französisch bis zur fachlichen Prüfung beim Originalkürzel.
Die übrigen Einträge verwenden belegte Kürzel oder ausgeschriebene Übersetzungen;
sie behaupten keine weltweit einheitliche Kürzelnorm.
Als fremdsprachige Eingaben werden zusätzlich DS/DSS/SS, TJD/TJS/BS,
Rkpd/Rkp/Rz/Rłd/Rłj, PO, Jn, PODG, LC, PN, BV und OLE erkannt.
Mehrdeutiges polnisches ST/St wird nicht automatisch zugeordnet.
Hp bedeutet hier Haltepunkt, Kr. bedeutet Gleiskreuzung; technische Typen wie
`Hp0`, `DKW190` und Kennungen wie `DKW_190` werden nicht zerlegt.

Die geprüften Begriffe werden bei Online-Übersetzungen vorübergehend durch
geschützte Platzhalter ersetzt und erst nach erfolgreicher Prüfung in der Zielsprache
eingesetzt. Das funktioniert auch über die englische Zwischensprache. Fehler oder
veränderte Platzhalter führen zu keiner Teilübernahme; Originaldateien,
Einstellungen und Zugangsdaten bleiben erhalten. Backup vor der Änderung erstellt.
MyMemory bleibt die Voreinstellung für neue Konfigurationen.

111 lokale Prüfungen bestanden, einschließlich aller 272 Kennungen, DE/EN/FR/PL,
Import/Speichern/erneutem Einlesen, Versionshinweisen und Fehlerfällen.
Keine echten API-Anfragen in diesen Prüfungen.
Im vollständigen ZIP liegen die Zuordnung und Kontrollliste unter
`docs/railway-glossary.md` und `docs/constructor-codes.txt`.

## Version 2.1.53: railway abbreviations and constructor identifiers

20 railway abbreviations now use a fixed local mapping in model names and in
texts translated through the selected provider. EW means single turnout without
requiring an angle. For example, `GK3 DKW DR DB (V8)[e]EEP16.2` becomes
`GK3 TJD DR DB (V8)[e]EEP16.2` in French.

The supplied initial list contains 272 unique three-character constructor identifiers.
The first application start creates an external UTF-8 file:
`%LOCALAPPDATA%\UGSo\IET2026\constructor-codes.txt`.
Add new identifiers with two letters and one digit, one per line, then restart the app.
Updates never overwrite this user file. Additions are merged with the supplied base
list. Blank lines, comments and invalid entries are ignored. Unreadable files are
retained and the base list remains available as a fallback.
Identifiers are retained anywhere, with or without parentheses. Generic recognition
of additional identifiers remains active. DR, DB, type labels, version notes such
as `(V8)` or `(v8.2)`, EEP versions and `[e]` remain unchanged.

| DE | EN | FR | PL |
| --- | --- | --- | --- |
| DKW | DSS | TJD | Rkpd |
| EKW | SS | TJS | Rkp |
| EW | single turnout | BS | Rz |
| ABW | contrary-flexure turnout | ABW* | Rłd |
| IBW | similar-flexure turnout | IBW* | Rłj |
| BW | locomotive depot | dépôt de locomotives | lokomotywownia |
| Bf | station | gare | stacja |
| Hbf | main station | gare principale | dworzec główny |
| Hp | halt | halte ferroviaire | przystanek osobowy |
| Gbf | freight station | gare de marchandises | stacja towarowa |
| Rbf | marshalling yard | gare de triage | stacja rozrządowa |
| Abzw | junction | bifurcation | posterunek odgałęźny |
| BÜ | level crossing | passage à niveau | przejazd kolejowo-drogowy |
| Stw | signal box | poste d’aiguillage | nastawnia |
| EG | station building | bâtiment voyageurs | budynek dworca |
| OL | overhead line equipment | caténaire | sieć trakcyjna |
| Gl. | track | voie | tor |
| Bstg. | platform | quai | peron |
| DWW | three-way turnout | branchement à trois voies | rozjazd potrójny |
| Kr. | diamond crossing | traversée de voies | skrzyżowanie torów |

*French retains ABW/IBW until their equivalents have been reviewed.
Other entries use documented abbreviations or written-out translations, rather
than claiming a universal abbreviation standard.
Additional source abbreviations: DS/DSS/SS, TJD/TJS/BS, Rkpd/Rkp/Rz/Rłd/Rłj,
PO, Jn, PODG, LC, PN, BV and OLE. Ambiguous Polish ST/St is not mapped.
Hp means a passenger halt here; Kr. means a track crossing.
Technical types such as `Hp0`, `DKW190` and identifiers such as `DKW_190` are not split.

Reviewed terms are temporarily replaced with protected placeholders for online
translation and restored in the target language only after validation, including
when English is the intermediate language. Errors or changed placeholders prevent
partial application. Original files, settings and credentials are retained.
Backup created before changes. MyMemory remains the default for new configurations.

111 local checks passed, covering all 272 identifiers, DE/EN/FR/PL, import/save/reload,
version notes and failure cases, without live API calls.
The full ZIP includes `docs/railway-glossary.md` and `docs/constructor-codes.txt`.

## Version 2.1.53 : abréviations ferroviaires et codes de constructeurs

20 abréviations ferroviaires utilisent désormais une correspondance locale fixe
dans les noms de modèles et les textes traduits par le fournisseur choisi.
EW signifie aiguillage simple, sans exiger un angle après le code.
Par exemple, `GK3 DKW DR DB (V8)[e]EEP16.2` devient
`GK3 TJD DR DB (V8)[e]EEP16.2` en français.

La liste initiale fournie contient 272 codes de constructeurs uniques de trois caractères.
Le premier démarrage crée un fichier UTF-8 externe :
`%LOCALAPPDATA%\UGSo\IET2026\constructor-codes.txt`.
Ajouter les nouveaux codes de deux lettres et un chiffre, un par ligne, puis redémarrer.
Les mises à jour n’écrasent jamais ce fichier personnel. Les ajouts sont fusionnés
avec la liste de base. Lignes vides, commentaires et entrées invalides sont ignorés.
Un fichier illisible est conservé et la liste de base sert de solution de repli. Les codes sont conservés partout, avec ou sans parenthèses.
La reconnaissance générale des autres codes reste active. DR, DB, types,
indications de version comme `(V8)` ou `(v8.2)`, versions EEP et `[e]` restent inchangés.

| DE | EN | FR | PL |
| --- | --- | --- | --- |
| DKW | DSS | TJD | Rkpd |
| EKW | SS | TJS | Rkp |
| EW | single turnout | BS | Rz |
| ABW | contrary-flexure turnout | ABW* | Rłd |
| IBW | similar-flexure turnout | IBW* | Rłj |
| BW | locomotive depot | dépôt de locomotives | lokomotywownia |
| Bf | station | gare | stacja |
| Hbf | main station | gare principale | dworzec główny |
| Hp | halt | halte ferroviaire | przystanek osobowy |
| Gbf | freight station | gare de marchandises | stacja towarowa |
| Rbf | marshalling yard | gare de triage | stacja rozrządowa |
| Abzw | junction | bifurcation | posterunek odgałęźny |
| BÜ | level crossing | passage à niveau | przejazd kolejowo-drogowy |
| Stw | signal box | poste d’aiguillage | nastawnia |
| EG | station building | bâtiment voyageurs | budynek dworca |
| OL | overhead line equipment | caténaire | sieć trakcyjna |
| Gl. | track | voie | tor |
| Bstg. | platform | quai | peron |
| DWW | three-way turnout | branchement à trois voies | rozjazd potrójny |
| Kr. | diamond crossing | traversée de voies | skrzyżowanie torów |

*En français, ABW/IBW sont conservés en attendant une validation terminologique.
Les autres entrées utilisent des abréviations attestées ou des traductions en toutes
lettres, sans prétendre à une norme universelle.
Autres abréviations reconnues en entrée : DS/DSS/SS, TJD/TJS/BS, Rkpd/Rkp/Rz/Rłd/Rłj,
PO, Jn, PODG, LC, PN, BV et OLE. Le polonais ambigu ST/St n’est pas converti.
Hp désigne ici une halte ferroviaire et Kr. une traversée de voies.
Les types techniques `Hp0`, `DKW190` et identifiants comme `DKW_190` ne sont pas découpés.

Les termes vérifiés sont remplacés temporairement par des marqueurs protégés pour
la traduction en ligne, puis rétablis dans la langue cible après validation,
y compris lorsque l’anglais sert de langue intermédiaire. Une erreur ou un marqueur
modifié empêche toute application partielle. Fichiers originaux, paramètres et
identifiants d’accès sont conservés. Sauvegarde effectuée avant modification.
MyMemory reste le choix par défaut des nouvelles configurations.

111 vérifications locales réussies : tous les 272 codes, DE/EN/FR/PL,
import/enregistrement/relecture, versions et erreurs, sans appel réel aux API.
Le ZIP complet inclut `docs/railway-glossary.md` et `docs/constructor-codes.txt`.
## Sources / Quellen

- [ISO railway vocabulary: single/double slip and curved turnouts](https://www.iso.org/obp/ui?_escaped_fragment_=iso%3Astd%3Aiso%3A23987%3A-1%3Adis%3Aed-1%3Av1%3Aen)
- [voestalpine: DSS](https://www.voestalpine.com/nortrak/en/products/Diamond-Crossings-Double-Slip-Switches-DSS/)
- [BEA-TT: BS, TJS, TJD](https://www.bea-tt.developpement-durable.gouv.fr/IMG/pdf/rapport_beatt_2018-09.pdf)
- [PKP Id-4: Rz, Rkp, Rkpd, Rłj, Rłd](https://www.plk-sa.pl/files/public/user_upload/pdf/Akty_prawne_i_przepisy/Instrukcje/Wydruk/Id-4.pdf)
- [Network Rail: Jn, OLE](https://www.networkrail.co.uk/wp-content/uploads/2017/04/Freight-Network-Study-April-2017.pdf)
- [ERA: level crossing / LC](https://www.era.europa.eu/system/files/2022-11/Implementation%20guidance%20for%20CSIs.pdf)
- [SNCF: PN](https://malignej.transilien.com/lexique-de-la-ligne-j/)
- [Chemins du Rail: BV](https://cheminsdurail.be/patrimoine-3/)

