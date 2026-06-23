// Supabase Edge Function: sync-subscription
//
// Reconciles the signed-in user's `subscriptions` row with the LIVE Stripe
// subscription and returns the fresh state. The Stripe webhook is still the
// primary source of truth, but this lets the account page pull the current
// status directly from Stripe on load / when returning from the billing portal,
// so a cancellation shows up immediately even if a webhook is delayed or missed.
//
// Deploy:  supabase functions deploy sync-subscription
// Secrets: STRIPE_SECRET_KEY  (SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY are
//          provided automatically by the platform)

import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY")!, {
  apiVersion: "2024-06-20",
  httpClient: Stripe.createFetchHttpClient(),
});

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  try {
    // Service-role client scoped with the caller's JWT: getUser() validates the
    // token, and the service role lets us write the (webhook-only) row.
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
      { global: { headers: { Authorization: req.headers.get("Authorization") ?? "" } } },
    );
    const { data: { user } } = await supabase.auth.getUser();
    if (!user) return json({ error: "Not authenticated" }, 401);

    const { data: row } = await supabase
      .from("subscriptions")
      .select("plan, status, current_period_end, cancel_at_period_end, stripe_customer_id, stripe_subscription_id")
      .eq("user_id", user.id)
      .maybeSingle();

    // Nothing to reconcile against — return whatever we have (possibly null).
    if (!row?.stripe_subscription_id && !row?.stripe_customer_id) {
      return json({ subscription: row ?? null });
    }

    // Fetch the live subscription: by id when we have it, else the customer's
    // most recent one.
    let sub: Stripe.Subscription | undefined;
    if (row.stripe_subscription_id) {
      sub = await stripe.subscriptions.retrieve(row.stripe_subscription_id);
    } else {
      const list = await stripe.subscriptions.list({
        customer: row.stripe_customer_id as string,
        status: "all",
        limit: 1,
      });
      sub = list.data[0];
    }
    if (!sub) return json({ subscription: row });

    const plan = (sub.metadata?.plan as string) ?? sub.items.data[0]?.price?.lookup_key ?? row.plan ?? "builder";
    const next = {
      user_id: user.id,
      plan,
      status: sub.status,
      stripe_customer_id: sub.customer as string,
      stripe_subscription_id: sub.id,
      current_period_end: new Date(sub.current_period_end * 1000).toISOString(),
      cancel_at_period_end: sub.cancel_at_period_end,
      updated_at: new Date().toISOString(),
    };
    await supabase.from("subscriptions").upsert(next, { onConflict: "user_id" });

    return json({
      subscription: {
        plan: next.plan,
        status: next.status,
        current_period_end: next.current_period_end,
        cancel_at_period_end: next.cancel_at_period_end,
      },
    });
  } catch (e) {
    return json({ error: e instanceof Error ? e.message : "sync failed" }, 500);
  }
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
}
