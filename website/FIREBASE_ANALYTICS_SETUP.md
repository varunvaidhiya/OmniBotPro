# OhhO — Firebase Analytics setup

Firebase Analytics (Google Analytics 4) is **wired into the codebase and inert
until you add credentials**. With `NEXT_PUBLIC_FIREBASE_*` unset the site builds
and runs exactly as it does today and the Firebase SDK is never downloaded by
visitors — the whole integration switches on with four environment variables.

## What's already in place

| File | Role |
|---|---|
| `lib/firebase.ts` | Firebase provider — lazy SDK load, `isSupported()` gate, config validation. Never throws. |
| `lib/analytics.ts` | Vendor-agnostic `track()` / `pageview()` API the site already calls. Fans out to Firebase, Plausible and PostHog. |
| `components/analytics/FirebaseAnalytics.tsx` | Mounted once in the root layout. Warms the SDK and emits `page_view` on every route. |
| `app/layout.tsx` | Mounts the component above. |
| `lib/firebase.test.ts` | Unit tests for config validation and server-side safety. |

Call sites (`components/Hero.tsx`, `lib/experiments.ts`) already emit events
through `lib/analytics.ts`, so nothing else needs to change.

## 1. Create the Firebase project

1. Go to https://console.firebase.google.com → **Add project**.
2. Name it (e.g. `ohho-robotics`) and **keep "Enable Google Analytics" ON** —
   this is the part that actually collects data. Analytics is not optional here.
3. Pick or create a Google Analytics account when prompted. Firebase creates a
   linked GA4 property and a **web data stream** for it.

## 2. Register the web app

1. Project overview → the **`</>` (Web)** icon → register an app
   (nickname `ohho-website`). Firebase Hosting is **not** needed — the site
   deploys on Vercel.
2. You land on **SDK setup and configuration**. Copy the `firebaseConfig`
   object. It looks like:

   ```js
   const firebaseConfig = {
     apiKey: "AIza…",
     authDomain: "ohho-robotics.firebaseapp.com",
     projectId: "ohho-robotics",
     storageBucket: "ohho-robotics.appspot.com",
     messagingSenderId: "1234567890",
     appId: "1:1234567890:web:abcdef123456",
     measurementId: "G-XXXXXXXXXX",   // ← required for analytics
   };
   ```

   You can return to these values any time via **Project settings → General →
   Your apps**.

> **No `measurementId`?** That means the web app was created before Analytics
> was enabled, or Analytics wasn't enabled on the project. Fix it under
> **Project settings → Integrations → Google Analytics** (or the **Analytics**
> tab), then re-check the app config. Without `measurementId` this integration
> deliberately stays off — the SDK would otherwise initialise and report
> nowhere.

## 3. Set the environment variables

Only four are required — the rest of `firebaseConfig` belongs to Firebase
products this site doesn't use:

```bash
NEXT_PUBLIC_FIREBASE_API_KEY=AIza…
NEXT_PUBLIC_FIREBASE_PROJECT_ID=ohho-robotics
NEXT_PUBLIC_FIREBASE_APP_ID=1:1234567890:web:abcdef123456
NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID=G-XXXXXXXXXX
```

If **any** of the four is missing or blank, analytics stays off and no SDK is
loaded. See `.env.example` for the optional vars.

**Local dev** — copy them into `website/.env.local` (git-ignored), then
`npm run dev`.

**Production** — set them either in **Vercel → Project → Settings →
Environment Variables**, or in the root `vercel.json` under `build.env`
alongside the existing `NEXT_PUBLIC_SUPABASE_*` entries. Redeploy afterwards:
`NEXT_PUBLIC_*` values are inlined at **build** time, so changing them without a
rebuild has no effect.

> These four values are public by design — they ship in the client bundle and
> identify the project without authorising anything. That is how every Firebase
> web app works. Access is controlled by Firebase Security Rules, not by hiding
> the config. Do not put a service-account key or any server secret here.

## 4. Verify

1. Deploy (or run locally with `.env.local` populated).
2. Open the site with devtools → **Network**. You should see a request to
   `www.googletagmanager.com/gtag/js?id=G-…` and `…/g/collect` requests as you
   navigate.
3. Firebase console → **Analytics → Realtime** (or GA4 → Reports → Realtime)
   shows your session within ~30 seconds.
4. For event-level detail, set `NEXT_PUBLIC_FIREBASE_DEBUG=true` and watch
   **Analytics → DebugView**, which streams individual events with their
   parameters. Turn it off for production — debug traffic is excluded from
   normal reports.

Navigate between a few pages and confirm you get a `page_view` per route, not
just one on first load.

## Events collected

| Event | Fired when | Parameters |
|---|---|---|
| `page_view` | every route, including client-side navigation | `page`, `page_path`, `page_location`, `page_title` |
| `hero_view` | landing hero becomes visible | `variant` |
| `hero_cta_click` | primary hero CTA clicked | `cta`, `variant` |
| `github_click` | GitHub link clicked | `from` |
| `experiment_exposure` | visitor bucketed into an A/B variant | `experiment`, `variant` |

`hero_*` and `experiment_exposure` carry the A/B variant from
`lib/experiments.ts`, so hero-copy conversion can be compared per variant in
GA4 once data accumulates.

### Adding an event

1. Add the name to the `AnalyticsEvent` union in `lib/analytics.ts`.
2. Call `track("your_event", { … })` from the component.

It reaches Firebase and every other configured provider automatically. Keep
names `snake_case` and ≤40 characters (a GA4 constraint), and avoid GA4's
[reserved event names](https://support.google.com/analytics/answer/13316687).

## Design notes

- **Automatic page views are switched off.** `lib/firebase.ts` initialises with
  `send_page_view: false`. gtag's built-in page view only fires on a hard load,
  which in a client-routed Next.js app would miss every in-app navigation *and*
  double-count the landing page against our own event. The
  `FirebaseAnalytics` component is the single source of page views.
- **The SDK is lazily imported** so it is code-split into its own chunk
  (~64 KB) rather than added to the shared bundle. Visitors never download it
  when Firebase isn't configured.
- **Failures are swallowed.** Every entry point is server-safe and catches its
  own errors — an ad blocker or a browser without IndexedDB degrades to no
  analytics, never to a broken page.
- **`isSupported()` is checked** before initialising, so unsupported
  environments (some in-app webviews, cookie-less contexts) opt out cleanly.

## Turning it off

- `NEXT_PUBLIC_ANALYTICS=off` disables **all** providers (Firebase, Plausible,
  PostHog) while leaving the code in place.
- Removing the `NEXT_PUBLIC_FIREBASE_*` vars disables Firebase specifically.

## Privacy

Firebase Analytics sets cookies and is subject to GDPR/ePrivacy consent
requirements in the EU/UK. This integration does **not** ship a consent banner —
if you need one, gate `initAnalytics()` in
`components/analytics/FirebaseAnalytics.tsx` behind the user's choice, and use
the SDK's [`setConsent()`](https://firebase.google.com/docs/reference/js/analytics.md#setconsent)
for consent-mode signalling. Also update the site's privacy policy to disclose
Google Analytics. Plausible remains wired up as a cookieless alternative if you
would rather not take on that obligation — the two can run side by side.
