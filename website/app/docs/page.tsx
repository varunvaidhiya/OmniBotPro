import type { Metadata } from "next";
import Link from "next/link";
import Nav from "@/components/Nav";
import Footer from "@/components/Footer";
import { getDocGroups } from "@/lib/docs";

export const dynamic = "force-static";

export const metadata: Metadata = {
  title: "Docs — OhhO",
  description:
    "Documentation for the OhhO robotics platform — a reference for every product, feature and integration.",
};

export default function DocsIndexPage() {
  const groups = getDocGroups();

  return (
    <>
      <Nav />
      <main className="pt-[120px] pb-24 min-h-screen relative overflow-hidden">
        <div className="hero-grid" />
        <div className="hero-orb-1 opacity-40" />
        <div className="hero-orb-2 opacity-40" />

        <div className="max-w-4xl w-full mx-auto px-6 relative z-10">
          <div className="font-mono text-[10px] font-medium tracking-[0.14em] uppercase mb-[14px]" style={{ color: "var(--cyan)" }}>
            Documentation
          </div>
          <h1 className="font-display font-bold text-[clamp(30px,5vw,52px)] tracking-tight leading-[1.12] mb-4 legible">
            OhhO Docs
          </h1>
          <p className="text-[16px] leading-[1.7] max-w-[640px] mb-14 legible" style={{ color: "rgba(255,255,255,0.62)" }}>
            Everything that powers the OhhO platform, documented. Browse the
            reference below — it updates automatically as new documents are added.
          </p>

          {groups.length === 0 && (
            <p className="text-white/50">No documents found yet.</p>
          )}

          <div className="flex flex-col gap-14">
            {groups.map((group) => (
              <section key={group.categoryKey || "general"}>
                <div className="flex items-baseline justify-between mb-5 border-b border-white/10 pb-3">
                  <h2 className="font-display font-bold text-[22px] tracking-tight legible">
                    {group.index ? (
                      <Link href={group.index.href} className="hover:text-cyan transition-colors">
                        {group.category}
                      </Link>
                    ) : (
                      group.category
                    )}
                  </h2>
                  <span className="font-mono text-[11px] text-white/35">
                    {group.docs.length} {group.docs.length === 1 ? "doc" : "docs"}
                  </span>
                </div>

                {group.index?.description && (
                  <p className="text-[14px] leading-[1.6] mb-6 max-w-[640px]" style={{ color: "rgba(255,255,255,0.55)" }}>
                    {group.index.description}
                  </p>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {group.docs.map((doc) => (
                    <Link
                      key={doc.href}
                      href={doc.href}
                      className="group block rounded-xl border border-white/10 bg-white/[0.02] p-5 transition-all duration-200 hover:border-cyan/40 hover:bg-white/[0.04]"
                    >
                      <div className="font-display font-semibold text-[15px] mb-1.5 group-hover:text-cyan transition-colors legible">
                        {doc.title}
                      </div>
                      {doc.description && (
                        <div className="text-[13px] leading-[1.55] line-clamp-3" style={{ color: "rgba(255,255,255,0.5)" }}>
                          {doc.description}
                        </div>
                      )}
                    </Link>
                  ))}
                </div>
              </section>
            ))}
          </div>
        </div>
      </main>
      <Footer />
    </>
  );
}
