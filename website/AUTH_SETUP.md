# OhhO — Auth & Billing setup

The OhhO website is a **static export**, so auth runs client-side via
[Supabase](https://supabase.com) and payments run through **Stripe Checkout**,
with the Stripe webhook hosted as a **Supabase Edge Function**. Until you wire
the credentials below, the site still builds and deploys — sign-in shows a
"not configured" notice and the product consoles stay open. Once configured,
every console (`/build`, `/serve`, …) requires an active paid subscription.

## Flow

```
visitor → /login (email magic link · Google)               ← Supabase Auth
   ↓ signed in
clicks "Open the console" → ConsoleGate checks subscription
   ├─ active plan  → console opens
   └─ no plan      → /upgrade  → Stripe Checkout
                                    ↓ payment
                          stripe-webhook (Edge Fn) → subscriptions table
                                    ↓
                          console unlocks
```

## 1. Supabase project

1. Create a project at https://supabase.com.
2. **SQL** → run [`supabase/migrations/0001_auth_billing.sql`](./supabase/migrations/0001_auth_billing.sql)
   (creates `profiles` + `subscriptions`, RLS, and the signup trigger).
3. **Project Settings → API** → copy the project URL and the `anon` key.

## 2. Auth providers (Authentication → Providers)

The site uses **email magic-link + Google**. Email needs no external account;
Google needs a (free) Google Cloud OAuth credential.

- **Email**: enabled by default (magic link / OTP — no extra config).
- **Google**: in Google Cloud Console create an OAuth 2.0 Client ID (type: Web
  application), add the Supabase callback
  `https://YOUR-PROJECT.supabase.co/auth/v1/callback` as an authorized redirect
  URI, then paste the client id/secret into Supabase → Providers → Google and
  enable it. Surface the button by setting `NEXT_PUBLIC_OAUTH_PROVIDERS=google`
  (step 3). Leave that var empty to ship email-only until Google is ready.
- **URL Configuration** → add your site origin(s) to **Redirect URLs**:
  `https://ohho-robotics.com/auth/callback`, `https://*.vercel.app/auth/callback`
  (preview deploys), `http://localhost:3000/auth/callback`.

## 3. Website env vars

Set in Vercel → Project → Settings → Environment Variables for **all
environments** (Production + Preview + Development), and in `.env.local` for dev
(see [`.env.example`](./.env.example)). **Static export bakes these in at build
time — redeploy after changing them.**

```
NEXT_PUBLIC_SUPABASE_URL=https://YOUR-PROJECT.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=YOUR-ANON-KEY
NEXT_PUBLIC_SITE_URL=https://ohho-robotics.com
NEXT_PUBLIC_OAUTH_PROVIDERS=google      # or empty for email-only
```

## 4. Stripe

1. Create the products/prices in Stripe (recurring): **Builder** ($49/mo) and
   **Fleet** ($199/mo). Copy each `price_…` id.
2. Set Edge Function secrets:

   ```bash
   supabase secrets set \
     STRIPE_SECRET_KEY=sk_live_... \
     STRIPE_WEBHOOK_SECRET=whsec_... \
     STRIPE_PRICE_BUILDER=price_... \
     STRIPE_PRICE_FLEET=price_...
   ```

3. Deploy the functions:

   ```bash
   supabase functions deploy create-checkout-session
   supabase functions deploy create-portal-session
   supabase functions deploy stripe-webhook --no-verify-jwt
   ```

4. In Stripe → Developers → Webhooks, add an endpoint pointing at
   `https://YOUR-PROJECT.supabase.co/functions/v1/stripe-webhook` for events:
   `checkout.session.completed`, `customer.subscription.updated`,
   `customer.subscription.deleted`. Copy the signing secret into
   `STRIPE_WEBHOOK_SECRET` (step 2) and redeploy the webhook.

## 5. Verify

- Visit `/login`, sign in with email or Google.
- Click **Open the console** on a product → you're routed to `/upgrade`.
- Subscribe → Stripe Checkout → back to `/account?checkout=success`.
- The webhook writes your `subscriptions` row; consoles unlock automatically.
- **Manage billing** on `/account` opens the Stripe customer portal.

## Notes

- Console access = any active paid plan (Builder or Fleet). Forge is sales-led.
- The `subscriptions` table is written **only** by the webhook (service role);
  clients can read just their own row (RLS).
- To gate a new console, wrap its page in
  `<ConsoleGate product="…">…</ConsoleGate>`.
