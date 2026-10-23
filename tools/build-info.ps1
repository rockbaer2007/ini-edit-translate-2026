$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$repoRoot = Split-Path $PSScriptRoot -Parent
foreach ($locale in @('de','en','fr')) {
    $document = New-Object System.Windows.Forms.RichTextBox
    try {
        $document.Font = New-Object System.Drawing.Font('Segoe UI', 10)
        $document.Text = [IO.File]::ReadAllText((Join-Path $repoRoot "docs/info/$locale.txt"), [Text.Encoding]::UTF8)
        $document.Select(0, $document.Lines[0].Length)
        $document.SelectionFont = New-Object System.Drawing.Font('Segoe UI', 13, [Drawing.FontStyle]::Bold)
        $document.SaveFile((Join-Path $repoRoot "Info.$locale.rtf"), [Windows.Forms.RichTextBoxStreamType]::RichText)
    } finally { $document.Dispose() }
}
