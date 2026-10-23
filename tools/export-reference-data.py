"""Export the preserved EEP reference workbook; never modify its source."""
from pathlib import Path
import csv
import openpyxl

root = Path(__file__).resolve().parents[1] / "Info_trans" / "reference-data"
book = openpyxl.load_workbook(root / "sources" / "iet.xlsx", read_only=True, data_only=True)
for sheet in book:
    rows = list(sheet.iter_rows(values_only=True))
    if tuple(rows[0]) != ("ID", "GER", "ENG", "FRA", "POL"):
        raise ValueError("Unexpected workbook schema: " + sheet.title)
    records = [row for row in rows[1:] if any(value is not None for value in row)]
    with (root / (sheet.title + ".csv")).open("w", encoding="utf-8-sig", newline="") as target:
        writer = csv.writer(target, delimiter=";")
        writer.writerow(rows[0])
        writer.writerows(records)
    print(sheet.title + ": " + str(len(records)) + " reference rows")
