export default function ProductDashboard({ slug }: { slug: string }) {
  return (
    <div className="relative w-full aspect-video rounded-[14px] overflow-hidden" style={{ background: "#0A0E1A" }}>
      <video
        autoPlay
        muted
        loop
        playsInline
        poster={`/videos/products/${slug}.png`}
        className="w-full h-full object-cover"
      >
        <source src={`/videos/products/${slug}.mp4`} type="video/mp4" />
      </video>
    </div>
  );
}
