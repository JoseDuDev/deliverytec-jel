import withPWA from '@ducanh2912/next-pwa';
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Acknowledge that next-pwa injects a webpack config while Turbopack is the default
  // bundler in Next.js 16. PWA service worker generation still runs at build time via
  // the webpack pipeline; Turbopack handles the dev/prod JS compilation.
  turbopack: {},
  async rewrites() {
    return [
      {
        source: '/bff/:path*',
        destination: 'http://localhost:7000/bff/:path*',
      },
      {
        source: '/admin/:path*',
        destination: 'http://localhost:7000/admin/:path*',
      },
    ];
  },
};

export default withPWA({ dest: 'public', disable: process.env.NODE_ENV === 'development' })(nextConfig);
