"use client";

/*
 * ContactModal — a small popup with Varun's direct contact details.
 *
 * Tap the email row to open the user's mail client (Gmail app on mobile,
 * default mail app on desktop) with a pre-filled subject; tap the phone row
 * to open the dialer on mobile (tel: link, ignored gracefully on desktop).
 * Closes on backdrop click, the X, or Escape.
 */

import { useEffect } from "react";
import { Mail, Phone, X } from "lucide-react";

interface Props {
  email: string;
  phone: string;
  onClose: () => void;
}

export default function ContactModal({ email, phone, onClose }: Props) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [onClose]);

  const phoneHref = `tel:${phone.replace(/[^\d+]/g, "")}`;
  const emailHref = `mailto:${email}?subject=${encodeURIComponent("Joining OhhO")}`;

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/70 backdrop-blur-sm" onClick={onClose} />

      <div
        role="dialog"
        aria-modal="true"
        aria-label="Contact Varun"
        className="relative w-full max-w-md flex flex-col rounded-2xl"
        style={{ background: "var(--surf)", border: "1px solid var(--border-med)", boxShadow: "0 24px 64px rgba(0,0,0,0.5)" }}
      >
        {/* header */}
        <div className="flex items-center justify-between px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
          <div>
            <h2 className="font-display font-bold text-[17px] tracking-tight">Get in touch</h2>
            <p className="text-[12px] font-mono mt-0.5" style={{ color: "var(--muted)" }}>
              Reach Varun directly
            </p>
          </div>
          <button onClick={onClose} className="p-1.5 rounded-lg hover:bg-white/5 transition-colors shrink-0" style={{ color: "var(--muted)" }} aria-label="Close">
            <X size={18} />
          </button>
        </div>

        {/* contact rows */}
        <div className="p-5 flex flex-col gap-2.5">
          <p className="text-[12.5px] leading-[1.6] mb-1" style={{ color: "var(--muted)" }}>
            Interested in joining OhhO or working together? Tap a row below to reach out — it opens your mail or phone app.
          </p>

          <a
            href={emailHref}
            className="flex items-center gap-3 p-3.5 rounded-xl transition-all hover:-translate-y-px"
            style={{ background: "rgba(0,212,255,.06)", border: "1px solid rgba(0,212,255,.22)" }}
          >
            <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ background: "rgba(0,212,255,.14)", color: "var(--cyan)" }}>
              <Mail size={17} />
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Email</div>
              <div className="text-[14px] font-medium truncate" style={{ color: "#fff" }}>{email}</div>
            </div>
            <svg className="w-4 h-4 opacity-50 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 17L17 7M17 7H7M17 7v10" /></svg>
          </a>

          <a
            href={phoneHref}
            className="flex items-center gap-3 p-3.5 rounded-xl transition-all hover:-translate-y-px"
            style={{ background: "rgba(124,58,237,.06)", border: "1px solid rgba(124,58,237,.22)" }}
          >
            <div className="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" style={{ background: "rgba(124,58,237,.14)", color: "var(--violet)" }}>
              <Phone size={17} />
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Phone</div>
              <div className="text-[14px] font-medium truncate" style={{ color: "#fff" }}>{phone}</div>
            </div>
            <svg className="w-4 h-4 opacity-50 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 17L17 7M17 7H7M17 7v10" /></svg>
          </a>
        </div>
      </div>
    </div>
  );
}
