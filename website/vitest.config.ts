import { defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

export default defineConfig({
  // Resolve the "@/..." path alias (same as tsconfig paths) so lib modules that
  // import via "@/lib/..." work under vitest the same way they do under Next.
  resolve: {
    alias: [{ find: /^@\//, replacement: fileURLToPath(new URL("./", import.meta.url)) }],
  },
  // lib/vr/manifest.ts imports lib/products.tsx (JSX icons). Vite 8 transforms
  // with Oxc, which otherwise honours tsconfig's `jsx: "preserve"` and leaves
  // JSX as invalid JS. Force the automatic runtime so .tsx transforms under
  // vitest; non-JSX tests are unaffected and the icons are never rendered here
  // (vrProducts() strips them).
  oxc: { jsx: { runtime: "automatic" } },
  test: {
    environment: "node",
    include: ["lib/**/*.test.ts"],
  },
});
