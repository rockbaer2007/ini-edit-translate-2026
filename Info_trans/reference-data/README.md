# Info_trans reference data

The original iconhelp.csv, twb.csv and ws_D_E.csv are copied unchanged from the
original IET repository. Keep any source attribution contained in their text.
The technical glossary contains a title row before the column header.
The DE/EN glossary has no header; its first entry is data.

The additional ws_D_E_F.csv has a Deutsch/English/Français header and 2,428
reviewed entries with French translations, after user-confirmed merges, splits
and removals on 2026-10-23. It takes precedence in the
dictionary tab. The original DE/EN file, including its trailing blank record,
remains unchanged. English is the reference language; confirmed German and
column corrections are recorded in docs/dictionary-review.json. All source review
questions are resolved. At the user's request, English translations were supplied
for control/display and revenue responsibility; the corresponding French text was
reviewed. Incomplete '(i.e' fragments were removed from both affected entries.
The CSV is UTF-8 and supports quoted semicolons and accents. Existing installations
without this additional file retain the legacy DE/EN dictionary fallback.

The original sources/iet.xlsx is retained unchanged. tools/export-reference-data.py
exports its six sheets as UTF-8 CSV without changing source text or adding entries.
The workbook contains one populated entry in object, and no entries in estate,
goods, landscape, stock or signals.

The restored IET2.FDB and iconhelp.dat are copied unchanged from the archived
2019 Info_trans release. Its database contains 5 ESTATE rows, 2 GOODS rows and
2 DB_UPDATES rows. SIGNALS, ROLLING_STOCK, TRACK_OBJECT and LANDSCAPE are empty.
This actual database takes precedence over the sparse workbook-derived CSV tables.

Info_trans is built for x86 to match the supplied Firebird 2.5.8 embedded runtime.
The managed provider remains version 7.5.0.0; real queries and DE/EN/FR form contents
were verified against separate database copies. No Firebird server installation or
network connection is required. The CSV dictionaries are loaded separately.
Manual SQL import is available only with a database present. A timestamped copy
of IET2.FDB is required before import; automatic application updates stay disabled.
