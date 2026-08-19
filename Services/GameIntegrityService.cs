using System.Security.Cryptography;
using PNETGuard.Models;

namespace PNETGuard.Services;

public sealed class GameIntegrityService
{
    private static readonly string[] CriticalRelativeFiles =
    {
        "hl.exe", "hw.dll", "sw.dll", "filesystem_stdio.dll",
        "cstrike\\cl_dlls\\client.dll", "cstrike\\dlls\\mp.dll"
    };

    public async Task<IReadOnlyList<AntiCheatFinding>> ValidateAsync(
        string csFolder,
        IEventSink sink,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var findings = new List<AntiCheatFinding>();
        foreach (string relative in CriticalRelativeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = Path.Combine(csFolder, relative);
            if (!File.Exists(path)) continue; // instalações variam; ausência não gera acusação.

            string hash = await ComputeSha256Async(path, cancellationToken);
            var info = new FileInfo(path);
            await sink.WriteAsync(new GuardEvent(
                "game_file_integrity", DateTimeOffset.UtcNow, sessionId,
                new { RelativePath = relative, info.Length, info.LastWriteTimeUtc, Sha256 = hash }),
                cancellationToken);
        }
        return findings;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
