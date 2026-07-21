import withPWA from '@ducanh2912/next-pwa';
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // Acknowledge that next-pwa injects a webpack config while Turbopack is the default
  // bundler in Next.js 16. PWA service worker generation still runs at build time via
  // the webpack pipeline; Turbopack handles the dev/prod JS compilation.
  turbopack: {},
  async rewrites() {
    // O backend é alcançado por proxy server-side: o navegador chama a própria
    // origem do frontend e o Next repassa. Localhost no dev; em deploy, aponta
    // para a URL do backend via BACKEND_URL (ex.: https://delify-api.onrender.com).
    const backend = (process.env.BACKEND_URL ?? 'http://localhost:7000').replace(/\/$/, '');
    return [
      { source: '/bff/:path*', destination: `${backend}/bff/:path*` },
      { source: '/admin-api/:path*', destination: `${backend}/admin/:path*` },
      { source: '/painel-api/:path*', destination: `${backend}/painel/:path*` },
      { source: '/garcom-api/:path*', destination: `${backend}/garcom/:path*` },
    ];
  },
};

export default withPWA({ dest: 'public', disable: process.env.NODE_ENV === 'development' })(nextConfig);
