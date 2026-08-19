using System.Text.Json;
using PNETGuard.Models;

namespace PNETGuard.Services;

public static class DatabaseSettingsService
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Guardian");
    private static readonly string FilePath = Path.Combine(Folder, "database-settings.json");

    public static DatabaseSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new DatabaseSettings();
            return JsonSerializer.Deserialize<DatabaseSettings>(File.ReadAllText(FilePath)) ?? new DatabaseSettings();
        }
        catch { return new DatabaseSettings(); }
    }

    public static void Save(DatabaseSettings settings)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
