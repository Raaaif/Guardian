using PNETGuard.Models;

namespace PNETGuard;

public sealed class SessionResultForm : Form
{
    public SessionResultForm(SessionInfo session, TimeSpan duration, IReadOnlyList<AntiCheatFinding> findings, string reportPath)
    {
        Text = "Guardian - Resultado da Sessão";
        Icon = AppBranding.GetAppIcon();
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 610);
        BackColor = PnetTheme.Background;
        ForeColor = PnetTheme.Text;

        bool clean = findings.Count == 0;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 5, BackColor = PnetTheme.Background };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var head = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, BackColor = PnetTheme.Background };
        head.Controls.Add(new PictureBox { Image = AppBranding.GetLogoImage(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(72,72) });
        head.Controls.Add(new Label { Text = "GUARDIAN\nRESULTADO DA SESSÃO", AutoSize = true, ForeColor = PnetTheme.Gold, Font = new Font("Segoe UI", 18, FontStyle.Bold), Margin = new Padding(12,8,0,0) });
        root.Controls.Add(head);

        root.Controls.Add(new Label { Text = clean ? "SESSÃO APROVADA" : "SESSÃO EM REVISÃO", AutoSize = true, ForeColor = clean ? PnetTheme.Green : PnetTheme.Red, Font = new Font("Segoe UI", 21, FontStyle.Bold), Margin = new Padding(0,18,0,8) });
        root.Controls.Add(new Label { AutoSize = true, ForeColor = PnetTheme.Text, Font = new Font("Segoe UI", 10.5f), Text = $"Jogador: {session.Nickname}\nSteamID: {session.SteamId}\nDuração protegida: {duration:hh\\:mm\\:ss}\nIrregularidades registradas: {findings.Count}\nID da sessão: {session.SessionId}" });

        var list = new ListBox { Dock = DockStyle.Fill, BackColor = PnetTheme.SurfaceAlt, ForeColor = PnetTheme.Text, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9.5f), Margin = new Padding(0,16,0,10) };
        if (clean) list.Items.Add("Nenhuma irregularidade foi detectada durante a sessão.");
        else foreach (var f in findings) list.Items.Add($"{f.Code} | {f.Summary}");
        root.Controls.Add(list);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = PnetTheme.Background };
        var copy = PnetTheme.CreateButton("COPIAR RESUMO", PnetTheme.GreenDark);
        copy.Click += (_,_) => Clipboard.SetText($"GUARDIAN - { (clean ? "SESSÃO APROVADA" : "SESSÃO EM REVISÃO") }\nJogador: {session.Nickname}\nSteamID: {session.SteamId}\nDuração: {duration:hh\\:mm\\:ss}\nIrregularidades: {findings.Count}\nID: {session.SessionId}");
        var open = PnetTheme.CreateButton("ABRIR RELATÓRIO", Color.FromArgb(148,111,24));
        open.Click += (_,_) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{reportPath}\"") { UseShellExecute = true });
        var close = PnetTheme.CreateButton("FECHAR", PnetTheme.RedDark); close.Click += (_,_) => Close();
        buttons.Controls.AddRange(new Control[]{copy,open,close}); root.Controls.Add(buttons);
        Controls.Add(root);
    }
}
