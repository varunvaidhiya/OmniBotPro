// Supabase Edge Function: create-checkout-session
//
// Creates a Stripe Checkout Session (subscription mode) for the signed-in user
// and returns its URL. Called from the /upgrade page via supabase.functions.invoke.
//
// Deploy:  supabase functions deploy create-checkout-session
// Secrets: STRIPE_SECRET_KEY, STRIPE_PRICE_BUILDER, STRIPE_PRICE_FLEET
//          (SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY are provided automatically)

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

const PRICES: Record<string, string | undefined> = {
  builder: Deno.env.get("STRIPE_PRICE_BUILDER"),
  fleet: Deno.env.get("STRIPE_PRICE_FLEET"),
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  try {
    const { plan, next, origin } = await req.json();
    const priceId = PRICES[plan];
    if (!priceId) return json({ error: `Unknown or unpriced plan: ${plan}` }, 400);

    // identify the caller from their Supabase JWT
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
      { global: { headers: { Authorization: req.headers.get("Authorization") ?? "" } } },
    );
    const { data: { user }, error: userErr } = await supabase.auth.getUser();
    if (userErr || !user) return json({ error: "Not authenticated" }, 401);

    // reuse an existing Stripe customer if we have one, else create + remember it
    const { data: existing } = await supabase
      .from("subscriptions")
      .select("stripe_customer_id")
      .eq("user_id", user.id)
      .maybeSingle();

    let customerId = existing?.stripe_customer_id as string | undefined;
    if (!customerId) {
      const customer = await stripe.customers.create({
        email: user.email ?? undefined,
        metadata: { user_id: user.id },
      });
      customerId = customer.id;
    }

    const base = (origin as string) ?? Deno.env.get("SITE_URL") ?? "";
    const session = await stripe.checkout.sessions.create({
      mode: "subscription",
      customer: customerId,
      line_items: [{ price: priceId, quantity: 1 }],
      success_url: `${base}/account?checkout=success`,
      cancel_url: `${base}/upgrade${next ? `?next=${encodeURIComponent(next)}` : ""}`,
      allow_promotion_codes: true,
      subscription_data: { metadata: { user_id: user.id, plan } },
      metadata: { user_id: user.id, plan },
    });

    return json({ url: session.url });
  } catch (e) {
    return json({ error: e instanceof Error ? e.message : "checkout failed" }, 500);
  }
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
}
