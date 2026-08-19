using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class PreMatchScanner
{
public event Action<int, string>? ProgressChanged;

    public async Task<PreMatchScanResult> ScanAsync(
        SessionInfo session,
        IEventSink sink,
        CancellationToken token)
    {
        string scanId = Guid.NewGuid().ToString("N");
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        var findings = new List<AntiCheatFinding>();
        int processCount = 0;
        int fileCount = 0;
        bool accessLimited = false;

        SteamCsValidation steam = SteamCsValidator.ValidateFolder(session.CsFolder ?? "", true);

        await sink.WriteAsync(new GuardEvent(
            "scan_started",
            startedAt,
            session.SessionId,
            new
            {
                ScanId = scanId,
                session.Nickname,
                session.SteamId,
                session.CsFolder,
                SteamValid = steam.IsValid,
                SteamMessage = steam.Message
            }), token);

        if (!steam.IsValid)
        {
            findings.Add(Finding(
                "GUARDIAN-STEAM-001",
                "steam",
                "A pasta selecionada não corresponde a uma instalação Steam válida.",
                steam.Message));
        }

        ProgressChanged?.Invoke(10, "Comparando arquivos críticos com a baseline oficial...");

        BaselineComparison baseline = await BaselineService.CompareCriticalFilesAsync(
            session.CsFolder ?? "", token);

        if (!baseline.BaselineInstalled)
        {
            // Falta de baseline é erro de configuração, não cheat.
            throw new InvalidOperationException(
                "A baseline oficial não está incluída nesta instalação do Guardian.");
        }

        foreach (string path in baseline.MissingFiles)
        {
            findings.Add(Finding(
                "GUARDIAN-CRITICAL-MISSING-002",
                "integrity",
                "Arquivo crítico original não encontrado.",
                $"Arquivo={path}"));
        }

        foreach (string path in baseline.ChangedFiles)
        {
            findings.Add(Finding(
                "GUARDIAN-CRITICAL-CHANGED-003",
                "integrity",
                "Arquivo crítico não corresponde ao original validado pela Steam.",
                $"Arquivo={path}"));
        }

        ProgressChanged?.Invoke(35, baseline.Message);
        ProgressChanged?.Invoke(40, "Procurando palavras-chave fortes em processos...");

        foreach (Process process in Process.GetProcesses())
        {
            token.ThrowIfCancellationRequested();
            processCount++;

            try
            {
                string? keyword =
                    SuspiciousKeywordService.Find(process.ProcessName);

                if (keyword is null)
                    continue;

                string? path = null;
                string? hash = null;

                try
                {
                    path = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path))
                        hash = await TryHashAsync(path, token);
                }
                catch { accessLimited = true; }

                findings.Add(Finding(
                    "GUARDIAN-KEYWORD-PROCESS-004",
                    "process",
                    "Processo com palavra-chave forte relacionada a cheat.",
                    $"Processo={process.ProcessName}; PID={process.Id}; Palavra={keyword}; Caminho={path}; SHA256={hash}"));
            }
            catch
            {
                accessLimited = true;
            }
            finally
            {
                process.Dispose();
            }
        }

        ProgressChanged?.Invoke(60, "Procurando palavras-chave fortes na pasta do jogo...");

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(session.CsFolder!, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            throw new IOException("Não foi possível ler a pasta do Counter-Strike.", ex);
        }

        foreach (string path in files)
        {
            token.ThrowIfCancellationRequested();
            fileCount++;

            string relative = Path.GetRelativePath(session.CsFolder!, path);
            string? keyword =
                SuspiciousKeywordService.Find(relative);

            if (keyword is null)
                continue;

            string? hash = await TryHashAsync(path, token);
            findings.Add(Finding(
                "GUARDIAN-KEYWORD-FILE-005",
                "game_file",
                "Arquivo com palavra-chave forte relacionada a cheat.",
                $"Arquivo={relative}; Palavra={keyword}; SHA256={hash}"));
        }

        ProgressChanged?.Invoke(85, "Enviando resultado do scan ao servidor...");

        string reportFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Guardian", "Scans");
        Directory.CreateDirectory(reportFolder);

        DateTimeOffset finishedAt = DateTimeOffset.UtcNow;
        string reportPath = Path.Combine(
            reportFolder,
            $"scan_{DateTime.Now:yyyyMMdd_HHmmss}_{scanId[..8]}.json");

        var report = new
        {
            ScanId = scanId,
            session.SessionId,
            session.Nickname,
            session.SteamId,
            session.CsFolder,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            SteamValidated = steam.IsValid,
            Baseline = new
            {
                baseline.CriticalChecked,
                baseline.CriticalMatched,
                baseline.MissingFiles,
                baseline.ChangedFiles
            },
            ProcessesAnalyzed = processCount,
            FilesAnalyzed = fileCount,
            AccessLimited = accessLimited,
            Findings = findings,
            Approved = findings.Count == 0 && steam.IsValid
        };

        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            token);

        foreach (AntiCheatFinding finding in findings)
        {
            await sink.WriteAsync(new GuardEvent(
                "scan_finding",
                finding.Timestamp,
                session.SessionId,
                new { ScanId = scanId, Finding = finding }), token);
        }

        bool approved = findings.Count == 0 && steam.IsValid;

        await sink.WriteAsync(new GuardEvent(
            "scan_finished",
            finishedAt,
            session.SessionId,
            new
            {
                ScanId = scanId,
                session.Nickname,
                session.SteamId,
                Approved = approved,
                SteamValidated = steam.IsValid,
                CriticalChecked = baseline.CriticalChecked,
                CriticalMatched = baseline.CriticalMatched,
                MissingCriticalFiles = baseline.MissingFiles,
                ChangedCriticalFiles = baseline.ChangedFiles,
                ProcessesAnalyzed = processCount,
                FilesAnalyzed = fileCount,
                Findings = findings,
                ReportPath = reportPath
            }), token);

        ProgressChanged?.Invoke(
            100,
            approved
                ? "Scan aprovado: arquivos críticos originais e nenhuma palavra-chave encontrada."
                : "Scan não aprovado: verifique as informações enviadas ao servidor.");

        return new PreMatchScanResult(
            scanId,
            startedAt,
            finishedAt,
            processCount,
            0,
            fileCount,
            Process.GetProcessesByName("hl").Length > 0,
            steam.IsValid,
            accessLimited,
            findings,
            reportPath,
            BaselineService.InstalledBaselinePath,
            approved ? 100 : 0,
            approved ? "approved" : "review",
            findings.Select(f => $"{f.Code}: {f.Summary}").ToList());
    }

    private static AntiCheatFinding Finding(
        string code,
        string category,
        string summary,
        string evidence) =>
        new(code, "review", category, summary, evidence, DateTimeOffset.UtcNow);

    public static async Task<string?> TryHashAsync(
        string path,
        CancellationToken token)
    {
        try
        {
            if (!File.Exists(path)) return null;

            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            byte[] hash = await SHA256.HashDataAsync(stream, token);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
}
