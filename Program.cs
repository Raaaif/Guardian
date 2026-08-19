namespace PNETGuard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Guardian");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "Guardian_Error.log"), $"[{DateTime.Now:O}] {ex}\n\n");
            }
            catch { }
            MessageBox.Show("O Guardian encontrou um erro ao iniciar. Consulte Guardian_Error.log em AppData Local.", "Guardian", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
