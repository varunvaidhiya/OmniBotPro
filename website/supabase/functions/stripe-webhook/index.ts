// Supabase Edge Function: stripe-webhook
//
// The source of truth for subscription state. Stripe calls this on checkout and
// subscription lifecycle events; we upsert the user's row in public.subscriptions
// using the service role (bypasses RLS). The website reads that row to gate
// console access.
//
// Deploy:  supabase functions deploy stripe-webhook --no-verify-jwt
// Secrets: STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET
// Then register the function URL as a Stripe webhook endpoint for events:
//   checkout.session.completed, customer.subscription.updated,
//   customer.subscription.deleted

import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2.45.0";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY")!, {
  apiVersion: "2024-06-20",
  httpClient: Stripe.createFetchHttpClient(),
});
const webhookSecret = Deno.env.get("STRIPE_WEBHOOK_SECRET")!;

const admin = createClient(
  Deno.env.get("SUPABASE_URL")!,
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
);

Deno.serve(async (req) => {
  const sig = req.headers.get("stripe-signature");
  const body = await req.text();
  let event: Stripe.Event;
  try {
    event = await stripe.webhooks.constructEventAsync(body, sig!, webhookSecret);
  } catch (e) {
    return new Response(`Webhook signature error: ${e instanceof Error ? e.message : e}`, { status: 400 });
  }

  try {
    switch (event.type) {
      case "checkout.session.completed": {
        const session = event.data.object as Stripe.Checkout.Session;
        if (session.subscription) {
          const sub = await stripe.subscriptions.retrieve(session.subscription as string);
          await upsert(sub, session.metadata?.user_id);
        }
        break;
      }
      case "customer.subscription.updated":
      case "customer.subscription.deleted": {
        const sub = event.data.object as Stripe.Subscription;
        await upsert(sub, sub.metadata?.user_id);
        break;
      }
    }
  } catch (e) {
    return new Response(`Handler error: ${e instanceof Error ? e.message : e}`, { status: 500 });
  }

  return new Response(JSON.stringify({ received: true }), { headers: { "Content-Type": "application/json" } });
});

async function upsert(sub: Stripe.Subscription, metaUserId?: string) {
  // resolve the user: subscription metadata → customer metadata fallback
  let userId = metaUserId;
  if (!userId) {
    const customer = await stripe.customers.retrieve(sub.customer as string);
    if (customer && !customer.deleted) userId = (customer.metadata?.user_id as string) || undefined;
  }
  if (!userId) return;

  const plan = (sub.metadata?.plan as string) ?? sub.items.data[0]?.price?.lookup_key ?? "builder";
  await admin.from("subscriptions").upsert(
    {
      user_id: userId,
      plan,
      status: sub.status,
      stripe_customer_id: sub.customer as string,
      stripe_subscription_id: sub.id,
      current_period_end: new Date(sub.current_period_end * 1000).toISOString(),
      updated_at: new Date().toISOString(),
    },
    { onConflict: "user_id" },
  );
}
