/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  // Export every route as <route>/index.html. Vercel (framework: null, static
  // output) resolves directory indexes universally, so nested clean URLs like
  // /products/comply work reliably. Without this, the export emits
  // products/comply.html, which this deployment did not serve at the clean URL.
  trailingSlash: true,
};

module.exports = nextConfig;
