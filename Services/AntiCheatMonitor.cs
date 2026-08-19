using System.Diagnostics;
using PNETGuard.Models;

namespace PNETGuard.Services;

/// <summary>
/// A cada 5 segundos:
/// - verifica se hl.exe está aberto;
/// - procura palavras-chave fortes nos processos;
/// - procura arquivos suspeitos dentro da pasta do CS;
/// - envia o estado da sessão ao banco.
/// </summary>
public sealed class AntiCheatMonitor : IAsyncDisposable
{
    private readonly IEventSink _sink;
    private readonly SessionInfo _session;
    private readonly TimeSpan _interval;
    private readonly HashSet<string> _reported =
        new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<bool>? GameStateChanged;
    public event Action<int>? ModuleCountChanged;
    public event Action<AntiCheatFinding>? FindingDetected;

    public AntiCheatMonitor(
        IEventSink sink,
        SessionInfo session,
        TimeSpan? interval = null)
    {
        _sink = sink;
        _session = session;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    public void Start()
    {
        if (_loop is not null)
            return;

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using Process? game = FindGameProcess();
                bool running = game is not null;
                int modules = 0;

                if (game is not null)
                {
                    try
                    {
                        modules = game.Modules.Count;
                    }
                    catch
                    {
                        modules = -1;
                    }
                }

                GameStateChanged?.Invoke(running);
                ModuleCountChanged?.Invoke(Math.Max(modules, 0));

                int processFindings =
                    await CheckProcessesAsync(token);
                int fileFindings =
                    await CheckGameFolderAsync(token);

                await _sink.WriteAsync(new GuardEvent(
                    "secure_session_check",
                    DateTimeOffset.UtcNow,
                    _session.SessionId,
                    new
                    {
                        _session.Nickname,
                        _session.SteamId,
                        GameRunning = running,
                        GameProcessId = game?.Id,
                        ModuleCount = modules,
                        SuspiciousProcessesFound = processFindings,
                        SuspiciousFilesFound = fileFindings,
                        CheckedAtUtc = DateTimeOffset.UtcNow
                    }), token);

                await Task.Delay(_interval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await _sink.WriteAsync(new GuardEvent(
                    "secure_session_error",
                    DateTimeOffset.UtcNow,
                    _session.SessionId,
                    new { ex.Message }), token);

                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
    }

    private async Task<int> CheckProcessesAsync(
        CancellationToken token)
    {
        int found = 0;

        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                string? keyword =
                    SuspiciousKeywordService.Find(process.ProcessName);

                if (keyword is null)
                    continue;

                found++;

                string uniqueKey =
                    $"process:{process.Id}:{process.ProcessName}:{keyword}";

                if (!_reported.Add(uniqueKey))
                    continue;

                string? path = null;
                string? hash = null;

                try
                {
                    path = process.MainModule?.FileName;

                    if (!string.IsNullOrWhiteSpace(path))
                        hash = await PreMatchScanner.TryHashAsync(path, token);
                }
                catch
                {
                    // Alguns processos não permitem leitura.
                }

                var finding = new AntiCheatFinding(
                    "GUARDIAN-SESSION-PROCESS-001",
                    "review",
                    "process",
                    "Processo com palavra-chave relacionada a cheat.",
                    $"Processo={process.ProcessName}; PID={process.Id}; " +
                    $"Palavra={keyword}; Caminho={path}; SHA256={hash}",
                    DateTimeOffset.UtcNow);

                FindingDetected?.Invoke(finding);

                await _sink.WriteAsync(new GuardEvent(
                    "secure_session_keyword_finding",
                    finding.Timestamp,
                    _session.SessionId,
                    finding), token);
            }
            catch
            {
                // Continua analisando os demais processos.
            }
            finally
            {
                process.Dispose();
            }
        }

        return found;
    }

    private async Task<int> CheckGameFolderAsync(
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_session.CsFolder) ||
            !Directory.Exists(_session.CsFolder))
            return 0;

        int found = 0;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(
                _session.CsFolder,
                "*",
                SearchOption.AllDirectories);
        }
        catch
        {
            return 0;
        }

        foreach (string path in files)
        {
            token.ThrowIfCancellationRequested();

            string relative =
                Path.GetRelativePath(_session.CsFolder, path);

            string? keyword =
                SuspiciousKeywordService.Find(relative);

            if (keyword is null)
                continue;

            found++;

            DateTime lastWrite;
            try
            {
                lastWrite = File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                lastWrite = DateTime.MinValue;
            }

            string uniqueKey =
                $"file:{relative}:{lastWrite.Ticks}";

            if (!_reported.Add(uniqueKey))
                continue;

            string? hash =
                await PreMatchScanner.TryHashAsync(path, token);

            var finding = new AntiCheatFinding(
                "GUARDIAN-SESSION-FILE-002",
                "review",
                "game_file",
                "Arquivo com palavra-chave relacionada a cheat encontrado durante a sessão.",
                $"Arquivo={relative}; Palavra={keyword}; " +
                $"AlteradoEmUtc={lastWrite:O}; SHA256={hash}",
                DateTimeOffset.UtcNow);

            FindingDetected?.Invoke(finding);

            await _sink.WriteAsync(new GuardEvent(
                "secure_session_file_finding",
                finding.Timestamp,
                _session.SessionId,
                finding), token);
        }

        return found;
    }

    private static Process? FindGameProcess()
    {
        Process[] games = Process.GetProcessesByName("hl");

        if (games.Length == 0)
            return null;

        Process selected = games[0];

        for (int i = 1; i < games.Length; i++)
            games[i].Dispose();

        return selected;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();

        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }
}
