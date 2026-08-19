using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PNETGuard.Services;

public sealed record SteamCsValidation(bool IsValid, string Message, string? HlExePath);

public static class SteamCsValidator
{
    private static readonly Regex SteamIdLegacy = new(@"^STEAM_[0-5]:[01]:\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SteamId64 = new(@"^7656119\d{10}$", RegexOptions.Compiled);

    public static bool IsSteamIdFormatValid(string value) =>
        SteamIdLegacy.IsMatch(value.Trim()) || SteamId64.IsMatch(value.Trim());

    public static SteamCsValidation ValidateFolder(string folder, bool requireSteamRunning)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return new(false, "A pasta selecionada não existe.", null);

        string full;
        try { full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar); }
        catch { return new(false, "O caminho selecionado é inválido.", null); }

        string hl = Path.Combine(full, "hl.exe");
        string cstrike = Path.Combine(full, "cstrike");
        string normalized = full.Replace('/', '\\').ToLowerInvariant();

        if (!File.Exists(hl) || !Directory.Exists(cstrike))
            return new(false, "Selecione a pasta principal do Half-Life/CS 1.6, contendo hl.exe e a pasta cstrike.", null);

        if (!normalized.Contains("\\steamapps\\common\\"))
            return new(false, "A instalação precisa pertencer a uma biblioteca oficial da Steam (steamapps\\common).", hl);

        if (requireSteamRunning && Process.GetProcessesByName("steam").Length == 0)
            return new(false, "A Steam precisa estar aberta para executar o scan oficial.", hl);

        return new(true, "Instalação oficial da Steam validada.", hl);
    }

    public static bool IsOfficialRunningGame(Process game, string expectedHlPath)
    {
        try
        {
            string? running = game.MainModule?.FileName;
            return !string.IsNullOrWhiteSpace(running) &&
                   string.Equals(Path.GetFullPath(running), Path.GetFullPath(expectedHlPath), StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
