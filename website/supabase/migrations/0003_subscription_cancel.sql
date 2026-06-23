-- OhhO — track Stripe "cancel at period end".
--
-- When a user cancels in the Stripe billing portal, Stripe keeps the
-- subscription `status = active` until the end of the paid period and sets
-- `cancel_at_period_end = true`. Without persisting that flag, the account page
-- couldn't tell a renewing plan from a cancelling one and wrongly showed
-- "renews on <date>". Storing it lets the UI show "ends on <date>" instead.

alter table public.subscriptions
  add column if not exists cancel_at_period_end boolean not null default false;
