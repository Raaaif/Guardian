using PNETGuard.Models;
using PNETGuard.Services;

namespace PNETGuard;

public sealed class MainForm : Form
{
    private readonly TextBox _nickname = new();
    private readonly TextBox _steamId = new();
    private readonly TextBox _csFolder = new();
    private Button _browse = null!, _scan = null!, _start = null!, _stop = null!;
    private readonly Label _scanStatus = new(), _sessionStatus = new(), _connectionStatus = new(), _gameStatus = new(), _integrityStatus = new(), _moduleStatus = new(), _findingStatus = new();
    private readonly ProgressBar _scanProgress = new();
    private readonly TextBox _log = new();
    private LocalJsonEventSink? _localSink;
    private IEventSink? _sink;
    private AntiCheatMonitor? _monitor;
    private HeartbeatService? _heartbeat;
    private SessionInfo? _session;
    private PreMatchScanResult? _lastScan;
    private DateTimeOffset _startedAt;
    private readonly List<AntiCheatFinding> _findings = new();
    private bool _gameRunning;
    private int _moduleCount;

    public MainForm()
    {
        Text = "Guardian";
        Icon = AppBranding.GetAppIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 790);
        Size = new Size(980, 850);
        BackColor = PnetTheme.Background;
        ForeColor = PnetTheme.Text;
        Font = new Font("Segoe UI", 9.5f);
        BuildLayout();
        FormClosing += OnFormClosing;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(26), ColumnCount=1, RowCount=7, BackColor=PnetTheme.Background };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(BuildHeader());
        root.Controls.Add(BuildPlayerPanel());
        root.Controls.Add(BuildActions());
        root.Controls.Add(BuildScanProgress());
        root.Controls.Add(BuildStatus());
        root.Controls.Add(BuildLog());
        root.Controls.Add(BuildFooter());
        Controls.Add(root);
        
    }

    private Control BuildHeader()
    {
        var p=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Margin=new Padding(0,0,0,18),BackColor=PnetTheme.Background};
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,94));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        p.Controls.Add(new PictureBox{Image=AppBranding.GetLogoImage(),SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(82,82)},0,0);
        var t=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0,8,0,0),BackColor=PnetTheme.Background};
        t.Controls.Add(new Label{Text="GUARDIAN",AutoSize=true,ForeColor=PnetTheme.Gold,Font=new Font("Segoe UI",24,FontStyle.Bold)});
        t.Controls.Add(new Label{Text="Scan pré-partida e monitoramento de Counter-Strike 1.6",AutoSize=true,ForeColor=PnetTheme.Muted,Font=new Font("Segoe UI",10.5f)});
        p.Controls.Add(t,1,0);
        return p;
    }

    private Control BuildPlayerPanel()
    {
        var outer=Surface();
        var c=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=3,Padding=new Padding(18)};
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));
        var s=PnetTheme.CreateSectionTitle("IDENTIFICAÇÃO DO JOGADOR");
        c.Controls.Add(s,0,0); c.SetColumnSpan(s,3);
        AddField(c,1,"Nickname",_nickname);
        AddField(c,2,"SteamID",_steamId);
        AddField(c,3,"Pasta do CS",_csFolder);
        _browse=PnetTheme.CreateButton("PROCURAR",PnetTheme.SurfaceAlt);
        _browse.MinimumSize=new Size(100,34);
        _browse.Click+=(_,_)=>Browse();
        c.Controls.Add(_browse,2,3);
        outer.Controls.Add(c);
        return outer;
    }

    private Control BuildActions()
    {
        var p=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,16,0,8),BackColor=PnetTheme.Background};
        _scan=PnetTheme.CreateButton("EXECUTAR SCAN PRÉ-PARTIDA",Color.FromArgb(148,111,24));
        _scan.Click+=async(_,_)=>await RunPreMatchScanAsync();
        _start=PnetTheme.CreateButton("INICIAR SESSÃO PROTEGIDA",PnetTheme.GreenDark);
        _start.Enabled=false;
        _start.Click+=async(_,_)=>await StartAsync();
        _stop=PnetTheme.CreateButton("FINALIZAR SESSÃO",PnetTheme.RedDark);
        _stop.Enabled=false;
        _stop.Click+=async(_,_)=>await StopAsync("user_finished",true);
        p.Controls.AddRange(new Control[]{_scan,_start,_stop});
        return p;
    }

    private Control BuildScanProgress()
    {
        var panel = new TableLayoutPanel { Dock=DockStyle.Top, AutoSize=true, ColumnCount=1, Margin=new Padding(0,2,0,8), BackColor=PnetTheme.Background };
        _scanProgress.Dock=DockStyle.Top;
        _scanProgress.Height=12;
        _scanProgress.Minimum=0;
        _scanProgress.Maximum=100;
        _scanProgress.Value=0;
        panel.Controls.Add(_scanProgress);
        return panel;
    }

    private Control BuildStatus()
    {
        var outer=Surface();
        var c=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,Padding=new Padding(18)};
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var s=PnetTheme.CreateSectionTitle("STATUS DO GUARDIAN");
        c.Controls.Add(s,0,0); c.SetColumnSpan(s,2);
        Configure(_scanStatus,"● Scan pré-partida pendente");
        Configure(_sessionStatus,"● Sessão inativa");
        Configure(_connectionStatus,"● Registro local inativo");
        Configure(_gameStatus,"● Counter-Strike não detectado");
        Configure(_integrityStatus,"● Integridade não validada");
        Configure(_moduleStatus,"● Módulos do jogo: 0");
        Configure(_findingStatus,"● Nenhuma irregularidade registrada");
        c.Controls.Add(_scanStatus,0,1);
        c.Controls.Add(_sessionStatus,1,1);
        c.Controls.Add(_connectionStatus,0,2);
        c.Controls.Add(_gameStatus,1,2);
        c.Controls.Add(_integrityStatus,0,3);
        c.Controls.Add(_moduleStatus,1,3);
        c.Controls.Add(_findingStatus,0,4); c.SetColumnSpan(_findingStatus,2);
        outer.Controls.Add(c);
        return outer;
    }

    private Control BuildLog()
    {
        var outer=Surface();
        var c=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),RowCount=2};
        c.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        c.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        c.Controls.Add(PnetTheme.CreateSectionTitle("EVENTOS E VERIFICAÇÕES"));
        _log.Multiline=true;
        _log.ReadOnly=true;
        _log.ScrollBars=ScrollBars.Vertical;
        _log.Dock=DockStyle.Fill;
        _log.BackColor=Color.FromArgb(8,9,10);
        _log.ForeColor=PnetTheme.Green;
        _log.Font=new Font("Consolas",9.5f);
        _log.BorderStyle=BorderStyle.FixedSingle;
        c.Controls.Add(_log,0,1);
        outer.Controls.Add(c);
        return outer;
    }

    private Control BuildFooter()=>new Label
    {
        AutoSize=true,
        MaximumSize=new Size(920,0),
        ForeColor=PnetTheme.Muted,
        Margin=new Padding(2,12,2,0),
        Text="Fluxo: execute o scan pré-partida, aguarde a validação dos arquivos críticos e das palavras-chave e, se aprovado, inicie a sessão segura. Durante a sessão, o Guardian envia o estado ao servidor a cada 5 segundos. O jogador não pode alterar o destino dos dados."
    };

    private Panel Surface()=>new(){Dock=DockStyle.Top,AutoSize=true,BackColor=PnetTheme.Surface,Padding=new Padding(1),Margin=new Padding(0,0,0,6),BorderStyle=BorderStyle.FixedSingle};
    private static void Configure(Label l,string text){l.Text=text;l.AutoSize=true;l.ForeColor=PnetTheme.Muted;l.Font=new Font("Segoe UI",10,FontStyle.Bold);l.Margin=new Padding(0,6,10,6);}
    private static void AddField(TableLayoutPanel p,int row,string label,TextBox box){p.Controls.Add(new Label{Text=label,AutoSize=true,ForeColor=PnetTheme.Text,Anchor=AnchorStyles.Left,Margin=new Padding(0,8,8,8)},0,row);PnetTheme.StyleTextBox(box);box.Dock=DockStyle.Fill;box.Margin=new Padding(0,4,8,4);p.Controls.Add(box,1,row);}
    private void Browse(){using var d=new FolderBrowserDialog{Description="Selecione a pasta principal do Counter-Strike 1.6"};if(d.ShowDialog(this)==DialogResult.OK){_csFolder.Text=d.SelectedPath;InvalidatePreviousScan();}}

    private bool ValidatePlayerData()
    {
        if(string.IsNullOrWhiteSpace(_nickname.Text)||string.IsNullOrWhiteSpace(_steamId.Text)||string.IsNullOrWhiteSpace(_csFolder.Text)||!Directory.Exists(_csFolder.Text))
        {
            MessageBox.Show(this,"Preencha nickname, SteamID e selecione a pasta oficial do CS 1.6 na Steam.","Guardian",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return false;
        }
        if(!SteamCsValidator.IsSteamIdFormatValid(_steamId.Text))
        {
            MessageBox.Show(this,"Informe um SteamID válido no formato STEAM_0:0:123456 ou SteamID64.","Guardian",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return false;
        }
        SteamCsValidation validation=SteamCsValidator.ValidateFolder(_csFolder.Text,true);
        if(!validation.IsValid)
        {
            MessageBox.Show(this,validation.Message,"Guardian - Steam obrigatória",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private async Task RunPreMatchScanAsync()
    {
        if(!ValidatePlayerData()) return;
        ToggleInputs(false);
        _scan.Enabled=false;
        _start.Enabled=false;
        _scanProgress.Value=0;
        _scanStatus.Text="● Scan pré-partida em andamento";
        _scanStatus.ForeColor=PnetTheme.Gold;
        _findingStatus.Text="● Analisando ambiente do jogador";
        _findingStatus.ForeColor=PnetTheme.Gold;
        Log("Scan pré-partida iniciado.");

        string sessionId=Guid.NewGuid().ToString("N");
        var scanSession=new SessionInfo(sessionId,_nickname.Text.Trim(),_steamId.Text.Trim(),_csFolder.Text.Trim(),DateTimeOffset.UtcNow);
        await using var localSink=new LocalJsonEventSink(sessionId);
        await using var sink=new CompositeEventSink(localSink,new GuardianRemoteEventSink());
        var scanner=new PreMatchScanner();
        scanner.ProgressChanged+=(value,message)=>BeginInvoke(()=>
        {
            _scanProgress.Value=Math.Clamp(value,0,100);
            Log(message);
        });

        try
        {
            _lastScan=await scanner.ScanAsync(scanSession,sink,CancellationToken.None);
            if(_lastScan.IsClean)
            {
                _scanStatus.Text="● Scan aprovado — ambiente limpo";
                _scanStatus.ForeColor=PnetTheme.Green;
                _findingStatus.Text="● Nenhuma irregularidade no scan pré-partida";
                _findingStatus.ForeColor=PnetTheme.Green;
                _start.Enabled=true;
                Log("Scan aprovado. Sessão protegida liberada.");
            }
            else
            {
                _scanStatus.Text="● Scan em revisão";
                _scanStatus.ForeColor=PnetTheme.Red;
                _findingStatus.Text=$"● Revisão necessária ({_lastScan.Findings.Count} indicador(es))";
                _findingStatus.ForeColor=PnetTheme.Red;
                _start.Enabled=false;
                Log("Scan não aprovado automaticamente. Encaminhar relatório à organização.");
            }
            using var resultForm=new PreMatchScanResultForm(scanSession,_lastScan);
            resultForm.ShowDialog(this);
        }
        catch(Exception ex)
        {
            _scanStatus.Text="● Falha ao concluir o scan";
            _scanStatus.ForeColor=PnetTheme.Red;
            Log($"Erro no scan: {ex.Message}");
            MessageBox.Show(this,"Não foi possível concluir o scan pré-partida. Execute novamente como administrador.","Guardian",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        finally
        {
            ToggleInputs(true);
            _scan.Enabled=true;
        }
    }

    private async Task StartAsync()
    {
        if(!ValidatePlayerData()) return;
        if(_lastScan is null || !_lastScan.IsClean)
        {
            MessageBox.Show(this,"É necessário passar por um scan pré-partida aprovado antes de iniciar a sessão protegida.","Guardian",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return;
        }

        _findings.Clear();
        _startedAt=DateTimeOffset.UtcNow;
        string id=Guid.NewGuid().ToString("N");
        _session=new SessionInfo(id,_nickname.Text.Trim(),_steamId.Text.Trim(),_csFolder.Text.Trim(),_startedAt);
        _localSink=new LocalJsonEventSink(id);
        _sink=new CompositeEventSink(_localSink,new GuardianRemoteEventSink());
        await _sink.WriteAsync(new GuardEvent("session_started",DateTimeOffset.UtcNow,id,new{Session=_session,PreMatchScanId=_lastScan.ScanId}));
        _sessionStatus.Text="● Sessão protegida ativa";
        _sessionStatus.ForeColor=PnetTheme.Green;
        _connectionStatus.Text=GuardianServerConfig.IsConfigured?"● Envio ao servidor ativo (5 segundos)":"● Servidor não configurado no build";
        _connectionStatus.ForeColor=GuardianServerConfig.IsConfigured?PnetTheme.Green:PnetTheme.Red;
        ToggleSession(true);
        Log($"Sessão protegida iniciada: {id}");

        _integrityStatus.Text="● Validando integridade...";
        var integrity=new GameIntegrityService();
        await integrity.ValidateAsync(_session.CsFolder!,_sink,id,CancellationToken.None);
        _integrityStatus.Text="● Integridade registrada";
        _integrityStatus.ForeColor=PnetTheme.Green;
        Log("Hashes dos arquivos críticos registrados.");

        _monitor=new AntiCheatMonitor(_sink,_session);
        _monitor.GameStateChanged+=running=>BeginInvoke(()=>
        {
            _gameRunning=running;
            _gameStatus.Text=running?"● Counter-Strike detectado":"● Aguardando Counter-Strike";
            _gameStatus.ForeColor=running?PnetTheme.Green:PnetTheme.Gold;
            Log(running?"hl.exe detectado. Monitoramento ativo.":"Aguardando abertura do hl.exe.");
        });
        _monitor.ModuleCountChanged+=count=>BeginInvoke(()=>
        {
            _moduleCount=count;
            _moduleStatus.Text=$"● Módulos do jogo: {count}";
        });
        _monitor.FindingDetected+=f=>BeginInvoke(()=>
        {
            _findings.Add(f);
            _findingStatus.Text=$"● Sessão em revisão ({_findings.Count})";
            _findingStatus.ForeColor=PnetTheme.Red;
            Log($"{f.Code}: irregularidade registrada para revisão.");
        });
        _monitor.Start();
        _heartbeat=new HeartbeatService(_sink,_session,()=>new{GameRunning=_gameRunning,ModuleCount=_moduleCount,Findings=_findings.Count,PreMatchScanId=_lastScan.ScanId,AppVersion=Application.ProductVersion});
        _heartbeat.Start();
    }

    private async Task StopAsync(string reason,bool showResult)
    {
        if(_session is null)return;
        ToggleSession(false);
        if(_heartbeat is not null){await _heartbeat.DisposeAsync();_heartbeat=null;}
        if(_monitor is not null){await _monitor.DisposeAsync();_monitor=null;}
        await _sink!.WriteAsync(new GuardEvent("session_finished",DateTimeOffset.UtcNow,_session.SessionId,new{Reason=reason,Findings=_findings.Count,PreMatchScanId=_lastScan?.ScanId}));
        string path=_localSink!.FilePath;
        await _sink.DisposeAsync();
        var endedSession=_session;
        var duration=DateTimeOffset.UtcNow-_startedAt;
        _sink=null;_localSink=null;_session=null;
        _sessionStatus.Text="● Sessão inativa";
        _sessionStatus.ForeColor=PnetTheme.Muted;
        _connectionStatus.Text="● Registro local inativo";
        _connectionStatus.ForeColor=PnetTheme.Muted;
        _gameStatus.Text="● Counter-Strike não detectado";
        _gameStatus.ForeColor=PnetTheme.Muted;
        Log("Sessão finalizada. Nenhum dado está sendo coletado.");
        if(showResult)using(var f=new SessionResultForm(endedSession,duration,_findings.ToList(),path))f.ShowDialog(this);
        _scan.Enabled=true;
        _start.Enabled=_lastScan?.IsClean==true;
        ToggleInputs(true);
    }

    private void InvalidatePreviousScan()
    {
        _lastScan=null;
        _start.Enabled=false;
        _scanStatus.Text="● Scan pré-partida pendente";
        _scanStatus.ForeColor=PnetTheme.Muted;
    }

    private void ToggleInputs(bool enabled){_nickname.Enabled=enabled;_steamId.Enabled=enabled;_csFolder.Enabled=enabled;_browse.Enabled=enabled;}
    private void ToggleSession(bool active){_scan.Enabled=!active;_start.Enabled=!active&&_lastScan?.IsClean==true;_stop.Enabled=active;ToggleInputs(!active);}
    private void Log(string m)=>_log.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}{Environment.NewLine}");
    private async void OnFormClosing(object? s,FormClosingEventArgs e){if(_session is null)return;e.Cancel=true;await StopAsync("application_closed",false);e.Cancel=false;Close();}
}
