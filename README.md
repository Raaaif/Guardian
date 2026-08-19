# Guardian v7 — Steam Full Scan

Cliente anti-cheat defensivo para partidas oficiais de Counter-Strike 1.6.

## Requisitos
- Windows 10/11 x64
- Steam aberta
- CS 1.6 oficial instalado em uma biblioteca `steamapps\common`
- Executar preferencialmente como administrador

## Scan pré-partida
- valida formato de SteamID;
- exige Steam aberta e instalação oficial;
- analisa processos ativos por indicadores cadastrados;
- analisa módulos carregados no `hl.exe` quando o jogo estiver aberto;
- percorre **todos os arquivos** da pasta do CS;
- calcula SHA-256 e gera um manifesto JSONL completo;
- registra nomes suspeitos e pontos de redirecionamento;
- gera relatório local aprovado ou em revisão.

## Sessão protegida
- acompanha abertura/fechamento do `hl.exe`;
- acompanha novos processos com indicadores;
- registra novas DLLs carregadas no jogo;
- observa criação, alteração, exclusão e renomeação dentro da pasta do CS;
- sinaliza criação/alteração de executáveis, scripts, DLLs e drivers durante a sessão;
- mantém heartbeat e relatório local.

## Relatórios
`%LOCALAPPDATA%\PNETGuard\Scans` e `%LOCALAPPDATA%\PNETGuard\Sessions`

## Supabase
A pasta `supabase` contém o esquema inicial. O envio está desativado até existir uma Edge Function/API segura. Não coloque `service_role` no cliente.

## Limite técnico
"Aprovado" significa que nenhuma irregularidade contemplada pelas regras atuais foi detectada. Nenhum scanner isolado garante detecção de 100% de todos os cheats, especialmente cheats inéditos ou de kernel.

## Configuração comercial do banco (v7.1)

A conexão com Supabase pode ser configurada pela interface em **CONFIGURAR BANCO**.
O cliente informa nome da organização, URL do projeto, chave pública `anon` e tabela.
Há botão para testar a conexão. O relatório do scan é enviado somente após a conclusão;
não há transmissão contínua durante a sessão.

Nunca use a chave `service_role` no aplicativo. Execute `supabase/schema.sql` no projeto do cliente.
