"use client";

import { useState, useRef } from "react";
import { Volume2, VolumeX } from "lucide-react";
import { type Accent } from "./dashboardKit";

export default function ProductDashboard({ slug, accent }: { slug: string; accent?: Accent }) {
  const [isMuted, setIsMuted] = useState(true);
  const videoRef = useRef<HTMLVideoElement>(null);

  const toggleMute = (e: React.MouseEvent) => {
    e.preventDefault();
    if (videoRef.current) {
      const nextMuted = !videoRef.current.muted;
      videoRef.current.muted = nextMuted;
      setIsMuted(nextMuted);
    }
  };

  return (
    <div className="relative w-full aspect-video rounded-[14px] overflow-hidden group" style={{ background: "#0A0E1A" }}>
      <video
        ref={videoRef}
        autoPlay
        muted
        loop
        playsInline
        poster={`/videos/products/${slug}.png`}
        className="w-full h-full object-cover"
      >
        <source src={`/videos/products/${slug}.mp4`} type="video/mp4" />
      </video>

      <button
        onClick={toggleMute}
        className="absolute bottom-4 right-4 bg-black/60 hover:bg-black/80 text-white p-2.5 rounded-full backdrop-blur-md transition-all opacity-0 group-hover:opacity-100 z-20"
        aria-label={isMuted ? "Unmute" : "Mute"}
      >
        {isMuted ? <VolumeX size={18} /> : <Volume2 size={18} />}
      </button>
    </div>
  );
}
