import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { getAllDocs, getDocBySlug } from "@/lib/docs";

export const dynamic = "force-static";
// Only docs that exist at build time are served; a new file appears on redeploy.
export const dynamicParams = false;

export function generateStaticParams() {
  return getAllDocs().map((doc) => ({ slug: doc.slug }));
}

export function generateMetadata({ params }: { params: { slug: string[] } }): Metadata {
  const doc = getDocBySlug(params.slug);
  if (!doc) return { title: "Docs — OhhO" };
  return {
    title: `${doc.title} — OhhO Docs`,
    description: doc.description || undefined,
  };
}

export default function DocPage({ params }: { params: { slug: string[] } }) {
  const doc = getDocBySlug(params.slug);
  if (!doc) notFound();

  const crumbHref = doc.categoryKey ? `/docs#${doc.categoryKey}` : "/docs";

  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-30" />

        <div className="max-w-3xl w-full mx-auto px-6 relative z-10">
          <nav className="flex items-center gap-2 text-[12px] font-mono tracking-wide mb-8" style={{ color: "rgba(255,255,255,0.45)" }}>
            <Link href="/docs" className="hover:text-cyan transition-colors">Docs</Link>
            {doc.categoryKey && (
              <>
                <span>/</span>
                <Link href={crumbHref} className="hover:text-cyan transition-colors">{doc.category}</Link>
              </>
            )}
          </nav>

          <article className="doc-prose" dangerouslySetInnerHTML={{ __html: doc.html }} />

          <div className="mt-16 pt-8 border-t border-white/10">
            <Link
              href="/docs"
              className="inline-flex items-center gap-2 text-[13px] font-mono tracking-widest uppercase text-cyan hover:text-white transition-colors"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="m15 18-6-6 6-6" />
              </svg>
              All docs
            </Link>
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
