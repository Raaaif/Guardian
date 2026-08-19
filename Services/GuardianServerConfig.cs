namespace PNETGuard.Services;

/// <summary>
/// CONFIGURAÇÃO EXCLUSIVA DOS DESENVOLVEDORES.
/// O jogador não possui tela ou arquivo externo para alterar estes valores.
///
/// Use somente a chave pública ANON com RLS configurado para INSERT.
/// Nunca coloque SERVICE_ROLE dentro do aplicativo.
/// </summary>
public static class GuardianServerConfig
{
    public const string SupabaseUrl = "https://fxxkjhwiwpyymgvojoqs.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZ4eGtqaHdpd3B5eW1ndm9qb3FzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODUxOTI2NzEsImV4cCI6MjEwMDc2ODY3MX0.Ble9Cr_-1ghQAAHp2ENGxP8GGk_hu8hicKYP01numBU";
    public const string EventsTable = "guardian_events";

    public static bool IsConfigured =>
        Uri.TryCreate(SupabaseUrl, UriKind.Absolute, out _) &&
        !SupabaseUrl.Contains("COLE_AQUI", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(SupabaseAnonKey) &&
        !SupabaseAnonKey.Contains("COLE_AQUI", StringComparison.OrdinalIgnoreCase);
}
