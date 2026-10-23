"""Build the reviewed DE/EN/FR dictionary from preserved source rows and French batches.

Usage: python tools/build-dictionary-fr.py [directory-with-fr-NNNN.txt]
The original DE/EN file is never overwritten. English source wording is retained,
except for the explicitly confirmed swapped row and clearly merged columns.
"""
import csv
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "Info_trans/ws_D_E.csv"
with source.open(encoding="utf-8-sig", newline="") as stream:
    original = list(csv.reader(stream, delimiter=";"))
translations = []
batch_directory = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent / "dictionary-fr"
for start in range(0, 2400, 400):
    lines = (batch_directory / f"fr-{start:04d}.txt").read_text(encoding="utf-8").splitlines()
    expected = min(400, 2430 - start) if start < 2000 else 430
    if len(lines) != expected:
        raise ValueError(f"Batch {start}: expected {expected} lines, got {len(lines)}")
    translations.extend(lines)
if len(original) != 2431 or len(translations) != 2430 or any(original[-1]):
    raise ValueError("Unexpected source shape; review the source before regeneration")

# Zero-based original row numbers. Corrections follow the user's English-first rule.
german = {
    37: "Automatischer Abstands- und Geschwindigkeitsregler", 228: "Bandbreite",
    104: "und Räder (Satzfragment)", 168: "Massenbeförderungsmittel",
    249: "Bedien- und Anzeigesystem", 382: "Bremsassistent", 748: "Fertigungstoleranz",
    851: "Getriebe", 877: "Gleisbild",
    889: "Gleisnummernanzeiger", 1033: "Hohlwelle", 1083: "Gegenbogen",
    1097: "Wagen mit Klappdeckeln", 1102: "Mikroschalter", 1327: "Zweifrequenzlokomotive",
    1336: "Kommunikationsleitungsadapter", 1366: "Nachlaufdrehgestell", 1367: "Nachlaufdrehgestell",
    1388: "Nietenzählen (übertriebene Detailkritik)", 1396: "Öffentlicher beweglicher Landfunk",
    1410: "Privater Mobilfunk (PMR)", 1523: "Quantifizierung", 1530: "Radbremsscheibe",
    1534: "Achslast", 1535: "Achslastverwiegung",
    1551: "Radmaterialien", 1555: "Datenrahmen", 1575: "Vorsichtssignal (calling-on signal)",
    1604: "Reibungsfrei", 1622: "Richtungsgleis", 1699: "Scheibenradkörper", 1729: "Schienenstromverbinder",
    1735: "Öffentliches Verkehrssystem", 1765: "Buchse / Anschlussdose",
    1772: "Trichterwagen", 1818: "Stromabnehmer-Hebesignal",
    1800: "Schwimmwinkel / Schräglagewinkel", 1814: "Selbstentladewagen",
    1902: "Betonbalkenbrücke", 1903: "Betonbalkenbrücke", 1934: "Steuerungsgestänge",
    1939: "Schrägstirnradgetriebe", 1953: "Gleisführung / Gleisplan",
    1959: "Mittlere Abweichung", 1965: "Wechselrichter", 2034: "Gleisbett",
    2040: "Kuppelstange", 2098: "Wechselrichter", 2167: "Verkehrslast",
    2251: "Adressnachricht", 2284: "Lokwechselstelle", 2300: "Weichensperrkreis",
    2311: "Umkehrvorrichtung", 2326: "Fahrgeldmanagementsystem",
    2348: "Zugbeeinflussung", 2385: "Zugleistung", 2386: "Zugleistung",
    2374: "Zugentlastung (Kabel)",
}
split_columns = {
    612: ("Elektronischer Buchfahrplan und Verzeichnis der Langsamfahrstellen", "electronic schedule sheet and catalog of restricted speed zones"),
    1308: ("Marktbereich Agrarprodukte / Forstwirtschaft / Konsumgüter", "Market Unit Agriculture/Forestry/ Consumer Goods"),
    1310: ("Marktbereich Chemie / Mineralöl / Düngemittel", "Market Unit Chemicals/ Mineral Oil/Fertilizers"),
    1564: ("Rangierbahnhof, Zugbildungsanlage, Zugbildungsbahnhof", "marshaling yard"),
    2084: ("UIC – Internationaler Eisenbahnverband", "International Railway Union/Organisation"),
    2110: ("Unbegleiteter kombinierter Ladungsverkehr", "unaccompanied Combined Transport"),
    2195: ("Vollautomatischer fahrerloser spurgeführter Betrieb", "automatic guideway transit (AGT)"),
}
french = {
    168: "moyens de transport collectif", 249: "système de commande et d'affichage",
    851: "boîte de vitesses, transmission", 877: "schéma des voies",
    1083: "contre-courbe", 1396: "radio mobile publique",
    1534: "charge à l'essieu", 1551: "matériaux de roue",
    1575: "signal d'appel autorisant une marche prudente",
    1327: "locomotive bifréquence", 1366: "bogie porteur arrière", 1367: "bogie porteur arrière",
    1604: "sans frottement", 2301: "appareil de manœuvre, manœuvre d'aiguillage",
}
# User-confirmed review, 2026-10-23. Keys always refer to the unchanged original
# row numbers (zero-based), so merges and splits cannot shift later corrections.
replacements = {
    103: [[original[103][0], original[103][1] + " " + original[104][0],
        "propulsion, système de traction, moteur avec engrenages et roues"]],
    104: [],
    168: [["Massenbeförderungsmittel", "public transport", "transports publics"]],
    249: [["Bedien- und Anzeigesystem", "control and display system", "système de commande et d'affichage"]],
    851: [["Getreide", "grain", "céréales"],
        ["Getriebe", "transmission, gear, gear box, gear", "transmission, engrenage, boîte de vitesses"]],
    1127: [["", original[1127][1], "transport combiné, transport intermodal"]],
    1128: [],
    1177: [[original[1177][0], original[1177][1].replace(" (i.e", ""), "bielle motrice"]],
    1242: [[original[1242][0], original[1242][1].replace(" (i.e", ""), "locomotive haut-le-pied"]],
    1460: [[original[1460][0], original[1460][1] + " " + original[1461][0],
        "locomotive à tender pour trains de voyageurs (par opposition à une locomotive pour trains express)"]],
    1461: [],
    1542: [[*original[1542], "bogie, (amér.) essieu monté"]],
    1923: [[original[1923][0], original[1923][1] + " " + original[1924][0],
        "déclivité, rampe, pente ascendante"]],
    1924: [],
    2020: [[original[2020][0], original[2020][1] + " Transport", "transport combiné non accompagné"]],
    2028: [[original[2028][0], original[2028][1] + " " + original[2029][0],
        "suivi d'un transport ou d'une expédition"]],
    2029: [],
    2048: [[*original[2048], "remorque pilote, motrice d'extrémité"]],
    2099: [["Umsatz in Geschäftsverantwortung", "revenue under operational responsibility", "chiffre d'affaires sous responsabilité opérationnelle"],
        ["Umsatz, im Namen und auf Rechnung von", "sales, in the name of and on the account of",
            "chiffre d'affaires, au nom et pour le compte de"]],
    2100: [["In Rechnung gestellt wurde", "Amount billed:", "montant facturé :"]],
    2301: [["Weichenstellbock", "switch stand", "appareil de manœuvre d'aiguillage"],
        ["Weichenstellen", "switching", "manœuvre des aiguillages"]],
}
unresolved = {}
output, review, corrections = [], [], []
for index, (row, fr) in enumerate(zip(original, translations)):
    de, en = row
    if index in split_columns:
        de, en = split_columns[index]
    de = german.get(index, de)
    if index == 168:
        en = "autom"
    fr = french.get(index, fr)
    fr = re.sub(r" \[.*\]$", "", fr)
    if not fr.strip():
        raise ValueError(f"Missing French at source row {index + 1}")
    result = replacements.get(index, [[de, en, fr]])
    output.extend(result)
    if index in replacements or [de, en] != row:
        corrections.append({"source_row": index + 1, "before": row, "after": result})
    if index in unresolved:
        review.append({"source_row": index + 1, "de_original": row[0], "en_original": row[1], "note": unresolved[index]})

target = ROOT / "Info_trans/reference-data/ws_D_E_F.csv"
with target.open("w", encoding="utf-8-sig", newline="") as stream:
    writer = csv.writer(stream, delimiter=";", lineterminator="\r\n")
    writer.writerow(["Deutsch", "English", "Français"])
    writer.writerows(output)
audit = ROOT / "docs/dictionary-review.json"
audit.write_text(json.dumps({"source": "Info_trans/ws_D_E.csv", "entries": len(output),
    "corrections": corrections, "unresolved": review}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Created {target.name}: {len(output)} entries, {len(corrections)} corrections, {len(review)} unresolved rows")
