using System.Text.Json;

namespace PNETGuard.Services;

public sealed record BaselineFileEntry(
    string RelativePath,
    string Sha256,
    long SizeBytes,
    string Status,
    string Source);

public sealed record BaselineComparison(
    bool BaselineInstalled,
    int CriticalChecked,
    int CriticalMatched,
    IReadOnlyList<string> MissingFiles,
    IReadOnlyList<string> ChangedFiles,
    string Message)
{
    public bool CriticalFilesValid =>
        BaselineInstalled &&
        MissingFiles.Count == 0 &&
        ChangedFiles.Count == 0 &&
        CriticalChecked > 0;
}

public static class BaselineService
{
    private static readonly string IntelligenceFolder =
        Path.Combine(AppContext.BaseDirectory, "intelligence");

    public static string InstalledBaselinePath =>
        Path.Combine(IntelligenceFolder, "approved_game_files.json");

    // Somente arquivos de execução e bibliotecas críticas são obrigatórios.
    // Configs, mapas, sons, demos e downloads não são comparados rigidamente.
    private static readonly string[] CriticalCandidates =
    {
        "hl.exe",
        "hw.dll",
        "sw.dll",
        "filesystem_stdio.dll",
        "steam_api.dll",
        "cstrike/cl_dlls/client.dll",
        "cstrike\\cl_dlls\\client.dll"
    };

    public static bool IsInstalled() => File.Exists(InstalledBaselinePath);

    public static async Task<BaselineComparison> CompareCriticalFilesAsync(
        string csFolder,
        CancellationToken token)
    {
        if (!IsInstalled())
            return new(false, 0, 0, Array.Empty<string>(), Array.Empty<string>(),
                "Baseline oficial ainda não instalada.");

        Dictionary<string, BaselineFileEntry> entries = LoadEntries();
        var missing = new List<string>();
        var changed = new List<string>();
        int checkedCount = 0;
        int matched = 0;

        foreach (string candidate in CriticalCandidates
                     .Select(Normalize)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();

            if (!entries.TryGetValue(candidate, out BaselineFileEntry? expected))
                continue;

            checkedCount++;
            string localPath = Path.Combine(csFolder,
                candidate.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(localPath))
            {
                missing.Add(candidate);
                continue;
            }

            string? actual = await PreMatchScanner.TryHashAsync(localPath, token);
            if (actual is null ||
                !actual.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                changed.Add(candidate);
                continue;
            }

            matched++;
        }

        string message = checkedCount == 0
            ? "A baseline não contém os arquivos críticos esperados."
            : missing.Count == 0 && changed.Count == 0
                ? $"{matched}/{checkedCount} arquivos críticos conferem com a instalação limpa."
                : $"{missing.Count} ausente(s) e {changed.Count} alterado(s).";

        return new(true, checkedCount, matched, missing, changed, message);
    }

    private static Dictionary<string, BaselineFileEntry> LoadEntries()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(InstalledBaselinePath));
        var result = new Dictionary<string, BaselineFileEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement item in document.RootElement.GetProperty("files").EnumerateArray())
        {
            string path = GetString(item, "relative_path", "RelativePath");
            string hash = GetString(item, "sha256", "Sha256");
            long size = GetInt64(item, "size_bytes", "SizeBytes");
            string status = GetString(item, "status", "Status");
            string source = GetString(item, "source", "Source");

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(hash))
                continue;

            result[Normalize(path)] = new(path, hash, size, status, source);
        }

        return result;
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/').ToLowerInvariant();

    private static string GetString(JsonElement item, string snake, string pascal)
    {
        if (item.TryGetProperty(snake, out JsonElement a)) return a.GetString() ?? "";
        if (item.TryGetProperty(pascal, out JsonElement b)) return b.GetString() ?? "";
        return "";
    }

    private static long GetInt64(JsonElement item, string snake, string pascal)
    {
        if (item.TryGetProperty(snake, out JsonElement a) && a.TryGetInt64(out long av)) return av;
        if (item.TryGetProperty(pascal, out JsonElement b) && b.TryGetInt64(out long bv)) return bv;
        return 0;
    }
}
