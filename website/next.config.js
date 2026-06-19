/** @type {import('next').NextConfig} */
const nextConfig = {
  // ESLint is configured (.eslintrc.json) for `npm run lint`, but pre-existing
  // apostrophe-in-JSX warnings across older pages shouldn't block production
  // builds. Lint manually with `npm run lint`.
  eslint: { ignoreDuringBuilds: true },
};

module.exports = nextConfig;
