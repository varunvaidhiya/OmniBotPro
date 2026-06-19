-- OhhO Garage — user robots schema
-- Run with: supabase db push (or paste into the Supabase SQL editor)
--
-- user_robots : one row per robot in a user's garage. Users can add any robot
--               type from the catalog, give it a name, and configure it.

create table if not exists public.user_robots (
  id                uuid primary key default gen_random_uuid(),
  user_id           uuid not null references auth.users (id) on delete cascade,
  name              text not null,
  robot_type_id      text not null,          -- references lib/garage/robot-catalog RobotType.id
  hardware_model_id  text not null,          -- references HardwareModel.id
  status            text not null default 'draft',  -- active | draft | simulated | offline
  config            jsonb not null default '{}',     -- per-robot configuration (cameras, ports, etc.)
  created_at        timestamptz not null default now(),
  updated_at        timestamptz not null default now()
);

create index if not exists user_robots_user_idx on public.user_robots (user_id);
create index if not exists user_robots_type_idx on public.user_robots (robot_type_id);

alter table public.user_robots enable row level security;

-- users can read their own robots
drop policy if exists "user_robots read own" on public.user_robots;
create policy "user_robots read own"
  on public.user_robots for select
  using (auth.uid() = user_id);

-- users can insert their own robots
drop policy if exists "user_robots insert own" on public.user_robots;
create policy "user_robots insert own"
  on public.user_robots for insert
  with check (auth.uid() = user_id);

-- users can update their own robots
drop policy if exists "user_robots update own" on public.user_robots;
create policy "user_robots update own"
  on public.user_robots for update
  using (auth.uid() = user_id);

-- users can delete their own robots
drop policy if exists "user_robots delete own" on public.user_robots;
create policy "user_robots delete own"
  on public.user_robots for delete
  using (auth.uid() = user_id);

-- auto-update updated_at
create or replace function public.update_updated_at()
returns trigger
language plpgsql
security definer set search_path = public
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

drop trigger if exists on_user_robot_update on public.user_robots;
create trigger on_user_robot_update
  before update on public.user_robots
  for each row execute function public.update_updated_at();
