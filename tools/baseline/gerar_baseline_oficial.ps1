param(
    [string]$CsPath = "",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

function Select-Folder([string]$Description) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = $Description
    $dialog.ShowNewFolderButton = $false
    if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        return $dialog.SelectedPath
    }
    return ""
}

if ([string]::IsNullOrWhiteSpace($CsPath)) {
    $CsPath = Select-Folder "Selecione a pasta oficial do Counter-Strike 1.6 instalada pela Steam"
}

if ([string]::IsNullOrWhiteSpace($CsPath)) {
    throw "Nenhuma pasta foi selecionada."
}

$CsPath = [System.IO.Path]::GetFullPath($CsPath)

if (-not (Test-Path (Join-Path $CsPath "hl.exe"))) {
    throw "hl.exe não encontrado. Selecione a pasta raiz do Counter-Strike 1.6."
}

if (-not (Test-Path (Join-Path $CsPath "cstrike"))) {
    throw "Pasta cstrike não encontrada."
}

if ($CsPath.ToLowerInvariant() -notmatch "\\steamapps\\common\\") {
    throw "A pasta selecionada não parece pertencer a uma biblioteca oficial da Steam."
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $OutputDir = Join-Path $PSScriptRoot "output\baseline_$timestamp"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host " GUARDIAN - BASELINE OFICIAL CS 1.6" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Pasta: $CsPath"
Write-Host "Saída: $OutputDir"
Write-Host ""

$files = Get-ChildItem -LiteralPath $CsPath -File -Recurse -Force |
    Sort-Object FullName

$records = New-Object System.Collections.Generic.List[object]
$total = $files.Count
$index = 0

foreach ($file in $files) {
    $index++
    $relative = $file.FullName.Substring($CsPath.Length).TrimStart('\')
    Write-Progress -Activity "Calculando hashes oficiais" -Status "$index de $total - $relative" -PercentComplete (($index / [Math]::Max($total, 1)) * 100)

    try {
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $records.Add([PSCustomObject]@{
            relative_path = $relative.Replace('\', '/')
            sha256 = $hash
            size_bytes = $file.Length
            extension = $file.Extension.ToLowerInvariant()
            last_write_utc = $file.LastWriteTimeUtc.ToString("o")
            status = "approved"
            source = "steam_clean_install"
        })
    }
    catch {
        $records.Add([PSCustomObject]@{
            relative_path = $relative.Replace('\', '/')
            sha256 = ""
            size_bytes = $file.Length
            extension = $file.Extension.ToLowerInvariant()
            last_write_utc = $file.LastWriteTimeUtc.ToString("o")
            status = "read_error"
            source = $_.Exception.Message
        })
    }
}

Write-Progress -Activity "Calculando hashes oficiais" -Completed

$manifest = [PSCustomObject]@{
    schema_version = 1
    generated_at_utc = (Get-Date).ToUniversalTime().ToString("o")
    game = "Counter-Strike 1.6"
    platform = "Steam"
    installation_path = $CsPath
    file_count = $records.Count
    manifest_sha256 = ""
    files = $records
}

$jsonPath = Join-Path $OutputDir "approved_game_files.json"
$csvPath = Join-Path $OutputDir "approved_game_files.csv"
$sqlPath = Join-Path $OutputDir "approved_game_files_import.sql"
$summaryPath = Join-Path $OutputDir "RESUMO.txt"

$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
$records | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8

$jsonHash = (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash.ToLowerInvariant()

$sqlLines = New-Object System.Collections.Generic.List[string]
$sqlLines.Add("-- Guardian - importação da baseline oficial do CS 1.6 Steam")
$sqlLines.Add("-- Gere esta baseline somente após validar os arquivos pela Steam.")
$sqlLines.Add("begin;")
foreach ($r in $records) {
    if ($r.status -ne "approved") { continue }
    $pathEscaped = $r.relative_path.Replace("'", "''")
    $sqlLines.Add(
        "insert into approved_game_files (game, platform, relative_path, sha256, size_bytes, source, active) values ('Counter-Strike 1.6', 'Steam', '$pathEscaped', '$($r.sha256)', $($r.size_bytes), 'steam_clean_install', true) on conflict (game, platform, relative_path, sha256) do nothing;"
    )
}
$sqlLines.Add("commit;")
$sqlLines | Set-Content -LiteralPath $sqlPath -Encoding UTF8

@"
GUARDIAN - BASELINE OFICIAL

Jogo: Counter-Strike 1.6
Plataforma: Steam
Pasta analisada: $CsPath
Arquivos inventariados: $($records.Count)
Manifesto SHA-256: $jsonHash
Gerado em UTC: $((Get-Date).ToUniversalTime().ToString("o"))

ARQUIVOS GERADOS
- approved_game_files.json
- approved_game_files.csv
- approved_game_files_import.sql

ANTES DE CONFIAR NESTA BASE
1. Na Steam, abra Propriedades do Counter-Strike.
2. Vá em Arquivos instalados.
3. Clique em Verificar integridade dos arquivos.
4. Não instale mods, skins, DLLs ou configurações de terceiros.
5. Gere a baseline novamente após qualquer atualização do jogo.

Esta baseline comprova quais arquivos estavam presentes nesta instalação limpa.
Ela não constitui uma lista de todos os cheats existentes.
"@ | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host ""
Write-Host "BASELINE GERADA COM SUCESSO" -ForegroundColor Green
Write-Host ""
Write-Host "Arquivos: $($records.Count)"
Write-Host "Manifesto: $jsonPath"
Write-Host "CSV:       $csvPath"
Write-Host "SQL:       $sqlPath"
Write-Host "SHA-256:   $jsonHash"
Write-Host ""
Start-Process explorer.exe $OutputDir
