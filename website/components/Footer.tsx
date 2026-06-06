"use client";

export default function Footer() {
  return (
    <footer style={{ borderTop: "1px solid rgba(255,255,255,0.07)", padding: "52px 40px 32px" }}>
      <div className="max-w-content mx-auto">
        <div
          className="grid gap-12 mb-10"
          style={{ gridTemplateColumns: "1fr auto" }}
        >
          {/* Left */}
          <div>
            <div className="font-display font-bold text-[19px] tracking-tight mb-[6px]">
              <span style={{ color: "var(--cyan)" }}>O</span>hh<span style={{ color: "var(--cyan)" }}>O</span>
            </div>
            <div className="text-[13px]" style={{ color: "rgba(255,255,255,0.52)" }}>
              Robotics, Operated.
            </div>
            <div className="font-mono text-[11px] leading-[1.7] mt-[14px]" style={{ color: "rgba(255,255,255,0.22)" }}>
              Built with ROS 2.&nbsp; Powered by embodied AI.
            </div>
          </div>

          {/* Right */}
          <div className="flex flex-col items-end gap-[22px]">
            <nav className="flex gap-[22px] flex-wrap">
              {["Products", "Pricing", "Docs", "GitHub", "Contact"].map((link) => (
                <a
                  key={link}
                  href={link === "Products" ? "#products" : link === "Pricing" ? "#pricing" : "#"}
                  className="text-[13px] transition-colors duration-200"
                  style={{ color: "rgba(255,255,255,0.52)" }}
                  onMouseEnter={(e) => (e.currentTarget.style.color = "#fff")}
                  onMouseLeave={(e) => (e.currentTarget.style.color = "rgba(255,255,255,0.52)")}
                >
                  {link}
                </a>
              ))}
            </nav>

            <div className="flex gap-[10px]">
              {/* GitHub */}
              <SocialLink title="GitHub">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4"/>
                  <path d="M9 18c-4.51 2-5-2-7-2"/>
                </svg>
              </SocialLink>
              {/* X */}
              <SocialLink title="X / Twitter">
                <svg viewBox="0 0 24 24" fill="currentColor">
                  <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.746l7.73-8.835L1.254 2.25H8.08l4.253 5.622zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                </svg>
              </SocialLink>
              {/* LinkedIn */}
              <SocialLink title="LinkedIn">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z"/>
                  <rect width="4" height="12" x="2" y="9"/>
                  <circle cx="4" cy="4" r="2"/>
                </svg>
              </SocialLink>
            </div>
          </div>
        </div>

        <div
          className="flex justify-between items-center pt-7 text-[11px]"
          style={{ borderTop: "1px solid rgba(255,255,255,0.07)", color: "rgba(255,255,255,0.22)" }}
        >
          <span>© 2026 OhhO. All rights reserved.</span>
          <span>OhhO — Built with ROS 2. Powered by embodied AI.</span>
        </div>
      </div>
    </footer>
  );
}

function SocialLink({ children, title }: { children: React.ReactNode; title: string }) {
  return (
    <a
      href="#"
      title={title}
      className="w-8 h-8 rounded-lg flex items-center justify-center transition-all duration-200"
      style={{
        border: "1px solid rgba(255,255,255,0.07)",
        color: "rgba(255,255,255,0.52)",
      }}
      onMouseEnter={(e) => {
        const el = e.currentTarget as HTMLElement;
        el.style.borderColor = "rgba(255,255,255,0.13)";
        el.style.color = "#fff";
        el.style.background = "rgba(255,255,255,.04)";
      }}
      onMouseLeave={(e) => {
        const el = e.currentTarget as HTMLElement;
        el.style.borderColor = "rgba(255,255,255,0.07)";
        el.style.color = "rgba(255,255,255,0.52)";
        el.style.background = "";
      }}
    >
      <svg className="w-[13px] h-[13px]">{(children as React.ReactElement).props.children}</svg>
    </a>
  );
}
