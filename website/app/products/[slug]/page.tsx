import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowRight, ArrowLeft } from "lucide-react";

import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import GlassCard from "@/components/GlassCard";
import ProductDashboard from "@/components/products/ProductDashboard";
import { ProductHeroCta, ProductPlanSection, ProductFinalCta } from "@/components/products/ProductPricingAware";
import {
  getProduct,
  relatedProducts,
  accentColor,
  PRODUCT_SLUGS,
  type Product,
} from "@/lib/products";
import { PRODUCTS_HREF, productHref } from "@/lib/site";

// Static export: pre-render one page per product, nothing else.
export const dynamicParams = false;
export function generateStaticParams() {
  return PRODUCT_SLUGS.map((slug) => ({ slug }));
}

export function generateMetadata({ params }: { params: { slug: string } }): Metadata {
  const p = getProduct(params.slug);
  if (!p) return { title: "Product | OhhO" };
  return {
    title: `${p.name} — ${p.tag} | OhhO`,
    description: p.hero,
  };
}

export default function ProductPage({ params }: { params: { slug: string } }) {
  const product = getProduct(params.slug);
  if (!product) notFound();

  const accent = product.accent;
  const aColor = accentColor(accent);
  const related = relatedProducts(product.slug);

  return (
    <>
      <Nav />
      <main className="pt-[104px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-60" />
        <div className="hero-orb-2 opacity-60" />

        <div className="max-w-5xl w-full mx-auto px-6 relative z-10">
          {/* breadcrumb */}
          <Link
            href={PRODUCTS_HREF}
            className="inline-flex items-center gap-2 text-[12px] font-mono tracking-widest uppercase transition-colors hover:text-white"
            style={{ color: aColor }}
          >
            <ArrowLeft size={14} /> All products
          </Link>

          {/* ── HERO ─────────────────────────────────────────────────────── */}
          <header className="mt-8 grid md:grid-cols-[1fr_auto] gap-10 items-start">
            <div>
              <div className="flex items-center gap-3 mb-5">
                <span
                  className="w-12 h-12 rounded-[14px] flex items-center justify-center [&>svg]:w-6 [&>svg]:h-6"
                  style={{
                    color: aColor,
                    background: accent === "cyan" ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.16)",
                    border: `1px solid ${accent === "cyan" ? "rgba(0,212,255,.26)" : "rgba(124,58,237,.30)"}`,
                  }}
                >
                  {product.icon}
                </span>
                <span
                  className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase px-2.5 py-1 rounded-full"
                  style={{ color: aColor, background: "rgba(255,255,255,.04)", border: "1px solid rgba(255,255,255,.1)" }}
                >
                  {product.category}
                </span>
              </div>

              <h1 className="font-display font-bold text-[clamp(34px,5.4vw,60px)] tracking-tight leading-[1.05] mb-3 legible">
                {product.name}
              </h1>
              <p className="text-[17px] md:text-[19px] font-medium mb-5" style={{ color: aColor }}>
                {product.tag}
              </p>
              <p className="text-[15px] md:text-[16px] leading-[1.7] max-w-[560px]" style={{ color: "rgba(255,255,255,0.72)" }}>
                {product.hero}
              </p>

              <ProductHeroCta product={product} />
            </div>

            {/* highlights */}
            <GlassCard accent={accent} interactive={false} padding="24px" radius={18} className="w-full md:w-[260px]">
              <div className="font-mono text-[10px] tracking-[0.12em] uppercase mb-4" style={{ color: aColor }}>
                At a glance
              </div>
              <ul className="flex flex-col gap-[13px]">
                {product.highlights.map((h) => (
                  <li key={h} className="flex items-start gap-2.5 text-[13px] leading-[1.5]" style={{ color: "rgba(255,255,255,0.78)" }}>
                    <span className="mt-[5px] w-1.5 h-1.5 rounded-full flex-shrink-0" style={{ background: aColor }} />
                    {h}
                  </li>
                ))}
              </ul>
            </GlassCard>
          </header>

          {/* ── DASHBOARD MOCKUP (the embedded "image") ──────────────────── */}
          <figure className="mt-16">
            {product.app ? (
              <div className="block group">
                <GlassCard accent={accent} interactive={false} padding="16px" radius={22} className="relative">
                  <span
                    className="absolute top-5 right-5 z-10 inline-flex items-center gap-1.5 font-mono text-[10px] font-semibold px-2.5 py-1 rounded-full pointer-events-none"
                    style={{ background: aColor, color: "var(--bg)" }}
                  >
                    <span className="badge-dot" style={{ background: "var(--bg)" }} /> LIVE
                  </span>
                  <div className="rounded-[14px] overflow-hidden">
                    <ProductDashboard slug={product.slug} accent={accent} />
                  </div>
                </GlassCard>
              </div>
            ) : (
              <GlassCard accent={accent} interactive={false} padding="16px" radius={22}>
                <div className="rounded-[14px] overflow-hidden">
                  <ProductDashboard slug={product.slug} accent={accent} />
                </div>
              </GlassCard>
            )}
            <figcaption className="text-center text-[12.5px] mt-4 font-mono" style={{ color: "rgba(255,255,255,0.4)" }}>
              {product.app ? (
                <Link href={product.app.href} className="inline-flex items-center gap-1.5 transition-colors hover:text-white" style={{ color: aColor }}>
                  This isn&apos;t a mockup — {product.app.label.toLowerCase()} <ArrowRight size={13} strokeWidth={2.5} />
                </Link>
              ) : (
                product.dashboardCaption
              )}
            </figcaption>
          </figure>

          {/* ── OVERVIEW ─────────────────────────────────────────────────── */}
          <Section kicker="Overview" title="What you get" accent={aColor}>
            <div className="max-w-[760px] flex flex-col gap-5">
              {product.overview.map((para, i) => (
                <p
                  key={i}
                  className="leading-[1.8]"
                  style={{ color: "rgba(255,255,255,0.74)", fontSize: i === 0 ? "18px" : "16px" }}
                >
                  {para}
                </p>
              ))}
            </div>
          </Section>

          {/* ── FEATURES ─────────────────────────────────────────────────── */}
          <Section kicker="Capabilities" title="Built to do the hard parts for you" accent={aColor}>
            <div className="grid sm:grid-cols-2 gap-[18px]">
              {product.features.map((f, i) => (
                <GlassCard key={f.title} accent={accent} delay={(i % 2) * 0.06} padding="24px" radius={16}>
                  <div className="font-display text-[16px] font-semibold mb-2">{f.title}</div>
                  <p className="text-[13.5px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.66)" }}>
                    {f.body}
                  </p>
                </GlassCard>
              ))}
            </div>
          </Section>

          {/* ── HOW IT WORKS ─────────────────────────────────────────────── */}
          <Section kicker="Workflow" title="How it works" accent={aColor}>
            <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-[18px]">
              {product.how.map((step, i) => (
                <div key={step.title} className="relative">
                  <div
                    className="w-9 h-9 rounded-full flex items-center justify-center font-mono text-[13px] font-semibold mb-4"
                    style={{ color: aColor, background: "rgba(255,255,255,.04)", border: `1px solid ${aColor}` }}
                  >
                    {i + 1}
                  </div>
                  <div className="font-display text-[15px] font-semibold mb-1.5">{step.title}</div>
                  <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.62)" }}>
                    {step.body}
                  </p>
                </div>
              ))}
            </div>
          </Section>

          {/* ── SPECS ────────────────────────────────────────────────────── */}
          <Section kicker="Under the hood" title="Specifications" accent={aColor}>
            <GlassCard interactive={false} padding="6px 26px" radius={16}>
              <dl>
                {product.specs.map((s, i) => (
                  <div
                    key={s.label}
                    className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-6 py-[15px]"
                    style={{ borderTop: i === 0 ? "none" : "1px solid rgba(255,255,255,0.07)" }}
                  >
                    <dt className="sm:w-[34%] text-[13px] font-mono tracking-wide" style={{ color: aColor }}>
                      {s.label}
                    </dt>
                    <dd className="flex-1 text-[14px]" style={{ color: "rgba(255,255,255,0.82)" }}>
                      {s.value}
                    </dd>
                  </div>
                ))}
              </dl>
            </GlassCard>
          </Section>

          {/* ── PLAN CHOOSER ─────────────────────────────────────────────── */}
          <ProductPlanSection product={product} />

          {/* ── FAQ ──────────────────────────────────────────────────────── */}
          {product.faq.length > 0 && (
            <Section kicker="FAQ" title="Common questions" accent={aColor}>
              <div className="flex flex-col gap-3 max-w-[820px]">
                {product.faq.map((f) => (
                  <GlassCard key={f.q} interactive={false} padding="20px 24px" radius={14}>
                    <div className="font-display text-[15px] font-semibold mb-2">{f.q}</div>
                    <p className="text-[13.5px] leading-[1.65]" style={{ color: "rgba(255,255,255,0.66)" }}>
                      {f.a}
                    </p>
                  </GlassCard>
                ))}
              </div>
            </Section>
          )}

          {/* ── RELATED ──────────────────────────────────────────────────── */}
          {related.length > 0 && (
            <Section kicker="The platform" title="Works better together" accent={aColor}>
              <div className="grid sm:grid-cols-3 gap-[18px]">
                {related.map((r) => (
                  <RelatedCard key={r.slug} product={r} />
                ))}
              </div>
            </Section>
          )}

          {/* ── FINAL CTA ────────────────────────────────────────────────── */}
          <div className="mt-[96px]">
            <GlassCard accent={accent} interactive={false} padding="48px 32px" radius={24} className="text-center">
              <h2 className="font-display font-bold text-[clamp(26px,4vw,40px)] tracking-tight leading-[1.1] mb-3 legible">
                Ready to build with {product.name}?
              </h2>
              <p className="text-[15px] mb-8 max-w-[440px] mx-auto leading-[1.6]" style={{ color: "rgba(255,255,255,0.66)" }}>
                Start free and simulate first — no hardware required. Upgrade when you're ready to deploy.
              </p>
              <ProductFinalCta product={product} />
            </GlassCard>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}

// ── Local presentational helpers (server components) ───────────────────────────

function SectionHead({ kicker, title, accent }: { kicker: string; title: string; accent: string }) {
  return (
    <div className="mb-8">
      <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-3" style={{ color: accent }}>
        {kicker}
      </div>
      <h2 className="font-display font-bold text-[clamp(24px,3.4vw,36px)] tracking-tight leading-[1.15] legible">
        {title}
      </h2>
    </div>
  );
}

function Section({
  kicker,
  title,
  accent,
  children,
}: {
  kicker: string;
  title: string;
  accent: string;
  children: React.ReactNode;
}) {
  return (
    <section className="mt-[88px]">
      <SectionHead kicker={kicker} title={title} accent={accent} />
      {children}
    </section>
  );
}

function RelatedCard({ product }: { product: Product }) {
  const aColor = accentColor(product.accent);
  return (
    <Link href={productHref(product.slug)} className="block group">
      <GlassCard accent={product.accent} padding="22px" radius={16} className="h-full flex flex-col gap-3">
        <span
          className="w-10 h-10 rounded-[11px] flex items-center justify-center [&>svg]:w-5 [&>svg]:h-5"
          style={{
            color: aColor,
            background: product.accent === "cyan" ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.16)",
            border: `1px solid ${product.accent === "cyan" ? "rgba(0,212,255,.24)" : "rgba(124,58,237,.28)"}`,
          }}
        >
          {product.icon}
        </span>
        <div>
          <div className="font-display text-[15px] font-semibold">{product.name}</div>
          <div className="text-[12px] mt-0.5" style={{ color: aColor }}>
            {product.tag}
          </div>
        </div>
        <span className="inline-flex items-center gap-1.5 text-[12px] font-semibold font-mono mt-auto" style={{ color: aColor }}>
          Learn more <ArrowRight size={12} strokeWidth={2.5} className="transition-transform group-hover:translate-x-1" />
        </span>
      </GlassCard>
    </Link>
  );
}
