"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { useScrollReveal } from "@/hooks/useScrollReveal";
import GlassCard from "@/components/GlassCard";
import {
  PRODUCTS,
  accentColor,
  productStatus,
  STATUS_META,
  type Product,
  type ProductStatus,
} from "@/lib/products";
import { productHref } from "@/lib/site";
import { PRODUCTS_INTRO } from "@/lib/copy";

// Available first, then beta, then roadmap — the grid leads with what's real.
const ORDERED_PRODUCTS = [...PRODUCTS].sort(
  (a, b) => STATUS_META[productStatus(a.slug)].order - STATUS_META[productStatus(b.slug)].order,
);

export default function Products() {
  const { ref: headRef, inView: headIn } = useScrollReveal();

  return (
    <section id="products" style={{ padding: "112px 24px" }}>
      <div className="max-w-content mx-auto">
        <div className="mb-[60px]">
          <div
            ref={headRef}
            className="transition-all duration-[650ms]"
            style={{ opacity: headIn ? 1 : 0, transform: headIn ? "none" : "translateY(22px)" }}
          >
            <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--cyan)" }}>
              {PRODUCTS_INTRO.eyebrow}
            </div>
            <h2 className="font-display font-bold text-[clamp(28px,4vw,46px)] tracking-tight leading-[1.12] mb-4 legible">
              {PRODUCTS_INTRO.headingTop}<br />{PRODUCTS_INTRO.headingBottom}
            </h2>
            <p className="text-[16px] leading-[1.7] max-w-[600px] legible" style={{ color: "rgba(255,255,255,0.62)" }}>
              {PRODUCTS_INTRO.body}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[18px]">
          {ORDERED_PRODUCTS.map((p, i) => (
            <ProductCard key={p.slug} product={p} delay={[0, 0.08, 0.16][i % 3]} />
          ))}
        </div>
      </div>
    </section>
  );
}

function StatusChip({ status }: { status: ProductStatus }) {
  const meta = STATUS_META[status];
  return (
    <span
      className="inline-flex items-center gap-1 font-mono text-[8.5px] font-semibold tracking-wider px-1.5 py-[2px] rounded-full uppercase"
      style={{ background: meta.bg, color: meta.color, border: `1px solid ${meta.border}` }}
    >
      {status === "available" && (
        <span className="badge-dot" style={{ background: meta.color, width: 5, height: 5 }} />
      )}
      {meta.label}
    </span>
  );
}

function ProductCard({ product, delay }: { product: Product; delay: number }) {
  const isCyan = product.accent === "cyan";
  const aColor = accentColor(product.accent);

  return (
    <Link href={productHref(product.slug)} className="block h-full">
      <GlassCard
        accent={isCyan ? "cyan" : "violet"}
        delay={delay}
        padding="28px"
        className="pc-card group flex flex-col gap-[14px] cursor-pointer h-full"
      >
        <div
          className="glass-pop w-[44px] h-[44px] rounded-[12px] flex items-center justify-center [&>svg]:w-[19px] [&>svg]:h-[19px]"
          style={{
            color: aColor,
            background: isCyan ? "rgba(0,212,255,.12)" : "rgba(124,58,237,.14)",
            border: isCyan ? "1px solid rgba(0,212,255,.24)" : "1px solid rgba(124,58,237,.26)",
            boxShadow: isCyan ? "inset 0 0 22px rgba(0,212,255,.18)" : "inset 0 0 22px rgba(124,58,237,.20)",
          }}
        >
          {product.icon}
        </div>

        <div>
          <div className="flex items-center gap-2">
            <div className="font-display text-[17px] font-semibold">{product.name}</div>
            <StatusChip status={productStatus(product.slug)} />
          </div>
          <div className="text-[12px] font-medium mt-[1px]" style={{ color: aColor }}>
            {product.tag}
          </div>
        </div>

        <p className="text-[13px] leading-[1.65] flex-1" style={{ color: "rgba(255,255,255,0.66)" }}>
          {product.desc}
        </p>

        <span
          className="inline-flex items-center gap-[5px] text-[12px] font-semibold font-mono tracking-[0.02em]"
          style={{ color: aColor }}
        >
          Learn more <ArrowRight size={12} strokeWidth={2.5} className="pc-link-arrow" />
        </span>
      </GlassCard>
    </Link>
  );
}
