# INI Edit Translate 2026

[Deutsch](README.md) · [English](README.en.md) · [Documentation](https://opensource.ugso-software.de/fr/eep/ini-edit-translate-2026/)

Éditeur Windows et traducteur ChatGPT pour les fichiers INI de modèles EEP et
`Installation.eep`, développé par Uwe Grimm / UGSo Software. Version **2.1.35.0**.

![Interface française sans données personnelles](docs/images/editor-fr.png)

## Fonctions

- Modifier les noms et descriptions en anglais, allemand, français et polonais.
- Gérer les axes de mouvement, fonctions des signaux, références PDF et métadonnées.
- Modifier les sections d’installation et comparer l’original à l’aperçu du résultat.
- Traduire directement depuis la langue source vers les trois autres langues avec OpenAI.
- Annuler une traduction ; rejeter les réponses incomplètes et les espaces réservés modifiés,
  tout en conservant les textes existants.
- Sauvegarder automatiquement les fichiers INI existants avant leur remplacement.
- Exporter les tableaux complets en UTF-8 avec BOM et séparateurs de tabulation.
- Textes d’interface allemands, anglais et français révisés.

**La mise à jour est désactivée**, aussi bien la connexion automatique que le menu.
Google, LEO, Linguee et DeepL ne sont plus proposés comme services de traduction.

## Téléchargement

[Télécharger le ZIP Windows v2.1.35](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/download/v2.1.35/INI-Edit-Translate-2026-v2.1.35-windows.zip) · [Version GitHub et SHA-256](https://github.com/rockbaer2007/ini-edit-translate-2026/releases/tag/v2.1.35)

Extraire tout le ZIP et lancer `INI Edit Translate 2026.exe`. Windows 11 et
.NET Framework 4.8 suffisent ; Visual Studio sert uniquement à compiler les sources.
Conserver les DLL et dossiers de langues ensemble. Aucun installateur nécessaire.

## Compilation et utilisation

Windows 11, Visual Studio 2026, développement de bureau .NET et Developer Pack .NET Framework 4.8.

```powershell
nuget restore "INI Edit Translate 2026.sln"
MSBuild.exe "INI Edit Translate 2026.sln" /t:Build /p:Configuration=Release
```

Lancer `Ini Edit - Translate/bin/Release/INI Edit Translate 2026.exe`, en conservant
les DLL et dossiers de langues à côté de l’exécutable. Aucun installateur prêt à l’emploi
n’est inclus dans ce dépôt.

Ouvrir **À propos → Paramètres → Traduction OpenAI**, puis saisir sa propre clé API et
le modèle (`gpt-4.1-mini` par défaut). Commencer avec une copie de sauvegarde en mode
**Lecture seule**. Utiliser ensuite **Lire et traduire** ou les boutons de traduction.
Vérifier la langue source et l’aperçu avant d’enregistrer.

OpenAI nécessite Internet et une facturation API distincte. Un abonnement ChatGPT
ne remplace pas cette facturation. L’accès réel à l’API et la qualité des traductions
produites restent à vérifier avec son propre compte.

## Données et vérifications

La configuration est conservée sous `%LOCALAPPDATA%/UGSo/IET2026/openai.json`. La clé
est chiffrée par Windows DPAPI pour le compte Windows actuel. Les variables
`IET_OPENAI_API_KEY` et `OPENAI_API_KEY` sont aussi prises en charge. Les sauvegardes
conservent le nom d’origine sous `%LOCALAPPDATA%/UGSo/IET2026/backups/`.
Les textes sélectionnés sont envoyés à OpenAI avec `store: false`. Les données de
l’ancienne application ne sont ni migrées ni remplacées automatiquement.

30 contrôles locaux couvrent les réponses API simulées, l’annulation, les erreurs,
les espaces réservés, la restauration, l’export et les interfaces DE/EN/FR rendues.
Aucun appel API réel. Exécuter `MSBuild.exe tests/IET.LocalTests.csproj /t:Build`,
puis `tests/bin/IET.LocalTests.exe`. Tous les schémas INI possibles ne sont pas pris en charge.

## Licence

Suite de [IET-Release-Nov19_02](https://github.com/rockbaer2007/IET-Release-Nov19_02),
sous [licence MIT](LICENSE). Le nouveau dépôt n’importe ni l’historique Git ancien,
ni les accès serveur, fichiers d’activation ou bases personnelles.
EEP appartient à son titulaire respectif ; cet outil est un développement indépendant.
