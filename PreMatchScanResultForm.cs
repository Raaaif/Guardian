using PNETGuard.Models;

namespace PNETGuard;

public sealed class PreMatchScanResultForm : Form
{
    public PreMatchScanResultForm(SessionInfo session, PreMatchScanResult result)
    {
        Text = "Guardian - Resultado do Scan Pré-Partida";
        Icon = AppBranding.GetAppIcon();
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(840, 650);
        BackColor = PnetTheme.Background;
        ForeColor = PnetTheme.Text;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 6, BackColor = PnetTheme.Background };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var head = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, BackColor = PnetTheme.Background };
        head.Controls.Add(new PictureBox { Image = AppBranding.GetLogoImage(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(72,72) });
        head.Controls.Add(new Label { Text = "GUARDIAN\nSCAN PRÉ-PARTIDA", AutoSize = true, ForeColor = PnetTheme.Gold, Font = new Font("Segoe UI", 18, FontStyle.Bold), Margin = new Padding(12,8,0,0) });
        root.Controls.Add(head);

        root.Controls.Add(new Label
        {
            Text = result.IsClean ? "SCAN APROVADO — INSTALAÇÃO STEAM VALIDADA" : "SCAN EM REVISÃO",
            AutoSize = true,
            ForeColor = result.IsClean ? PnetTheme.Green : PnetTheme.Red,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            Margin = new Padding(0,18,0,6)
        });

        root.Controls.Add(new Label
        {
            Text = result.IsClean
                ? "A instalação oficial da Steam foi inventariada por completo e nenhuma irregularidade contemplada pelas regras foi detectada."
                : "Foram encontrados indicadores ou limitações de acesso que exigem análise da organização.",
            AutoSize = true, MaximumSize = new Size(780, 0), ForeColor = PnetTheme.Text,
            Font = new Font("Segoe UI", 10.5f)
        });

        root.Controls.Add(new Label
        {
            AutoSize = true, ForeColor = PnetTheme.Text, Font = new Font("Segoe UI", 10.5f), Margin = new Padding(0,14,0,0),
            Text = $"Jogador: {session.Nickname}\nSteamID: {session.SteamId}\nProcessos analisados: {result.ProcessesAnalyzed}\nMódulos do jogo analisados: {result.GameModulesAnalyzed}\nArquivos da instalação analisados: {result.GameFilesAnalyzed}\nInstalação Steam validada: {(result.SteamInstallationValidated ? "Sim" : "Não")}\nCounter-Strike aberto durante o scan: {(result.CounterStrikeDetected ? "Sim" : "Não")}\nAcesso limitado: {(result.AccessLimited ? "Sim" : "Não")}\nIrregularidades: {result.Findings.Count}\nDuração: {result.Duration:mm\\:ss}\nID do scan: {result.ScanId}"
        });

        var list = new ListBox { Dock = DockStyle.Fill, BackColor = PnetTheme.SurfaceAlt, ForeColor = PnetTheme.Text, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9.5f), Margin = new Padding(0,16,0,10) };
        if (result.IsClean) list.Items.Add("APROVADO: nenhuma irregularidade detectada no scan pré-partida.");
        else
        {
            if (result.AccessLimited) list.Items.Add("PNET-SCAN-ACCESS-004 | Acesso limitado durante parte da verificação.");
            foreach (var finding in result.Findings) list.Items.Add($"{finding.Code} | {finding.Summary}");
        }
        root.Controls.Add(list);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = PnetTheme.Background };
        var copy = PnetTheme.CreateButton("COPIAR COMPROVANTE", PnetTheme.GreenDark);
        copy.Click += (_,_) => Clipboard.SetText(BuildSummary(session, result));
        var open = PnetTheme.CreateButton("ABRIR RELATÓRIO", Color.FromArgb(148,111,24));
        open.Click += (_,_) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{result.ReportPath}\"") { UseShellExecute = true });
        var close = PnetTheme.CreateButton("FECHAR", PnetTheme.RedDark); close.Click += (_,_) => Close();
        buttons.Controls.AddRange(new Control[]{copy,open,close}); root.Controls.Add(buttons);
        Controls.Add(root);
    }

    private static string BuildSummary(SessionInfo session, PreMatchScanResult result) =>
        $"GUARDIAN - SCAN PRÉ-PARTIDA\n" +
        $"RESULTADO: {(result.IsClean ? "APROVADO - INSTALAÇÃO STEAM VALIDADA" : "REVISÃO NECESSÁRIA")}\n" +
        $"Jogador: {session.Nickname}\nSteamID: {session.SteamId}\n" +
        $"Processos: {result.ProcessesAnalyzed}\nMódulos do jogo: {result.GameModulesAnalyzed}\n" +
        $"Arquivos da instalação: {result.GameFilesAnalyzed}\nIrregularidades: {result.Findings.Count}\n" +
        $"ID do scan: {result.ScanId}\nData: {result.FinishedAt.LocalDateTime:dd/MM/yyyy HH:mm:ss}";
}
