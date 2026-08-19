-- Guardian v7.1 - armazenamento de relatórios finais no Supabase
create table if not exists public.guard_reports (
  id uuid primary key default gen_random_uuid(),
  scan_id text not null unique,
  session_id text,
  nickname text not null,
  steam_id text not null,
  app_version text not null,
  result text not null check (result in ('approved','review','incomplete')),
  scan_report jsonb not null,
  session_report jsonb,
  created_at timestamptz not null default now()
);

alter table public.guard_reports enable row level security;

-- Permite que o Guardian insira relatórios usando apenas a chave pública anon.
-- A chave anon pode estar no aplicativo; a service_role nunca deve ser usada nele.
create policy "Guardian pode inserir relatórios"
on public.guard_reports
for insert
to anon
with check (true);

-- Não cria política de SELECT para anon. Assim o cliente envia, mas não consegue listar relatórios.
