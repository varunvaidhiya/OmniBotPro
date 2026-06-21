// Supabase Edge Function: create-donation-session
//
// Creates a one-time Stripe Checkout Session (payment mode) for a coffee donation.
// Accepts { amount: 500 | 1000 | 1500, origin } — amount is in cents.
// No subscription or plan required; the user does not need to be signed in.
//
// Deploy:  supabase functions deploy create-donation-session
// Secrets: STRIPE_SECRET_KEY
//          (SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY are provided automatically)

import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY")!, {
  apiVersion: "2024-06-20",
  httpClient: Stripe.createFetchHttpClient(),
});

const VALID_AMOUNTS = new Set([500, 1000, 1500]);

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  try {
    const { amount, origin } = await req.json();

    if (!VALID_AMOUNTS.has(Number(amount))) {
      return json({ error: "Invalid donation amount. Choose $5, $10, or $15." }, 400);
    }

    const base = (origin as string) ?? Deno.env.get("SITE_URL") ?? "";
    const dollars = (amount as number) / 100;

    const session = await stripe.checkout.sessions.create({
      mode: "payment",
      line_items: [
        {
          quantity: 1,
          price_data: {
            currency: "usd",
            unit_amount: amount as number,
            product_data: {
              name: `OhhO Coffee Donation — $${dollars}`,
              description:
                "Support the open-source OhhO robotics platform. ☕ Every coffee keeps the servers running.",
              images: [],
            },
          },
        },
      ],
      success_url: `${base}/upgrade?donation=success`,
      cancel_url: `${base}/upgrade`,
      metadata: { donation: "true", amount: String(amount) },
    });

    return json({ url: session.url });
  } catch (e) {
    return json({ error: e instanceof Error ? e.message : "Donation checkout failed." }, 500);
  }
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...cors, "Content-Type": "application/json" },
  });
}
