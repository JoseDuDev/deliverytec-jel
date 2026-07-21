// Semeia um ambiente de demonstração numa base Delify vazia, via a API pública.
// Idempotente: rodar de novo não duplica nada.
//
// Uso:  node scripts/seed-demo.mjs https://delify-api.onrender.com
//       node scripts/seed-demo.mjs                (default http://localhost:7000)
//
// Requer Node 18+ (fetch global). Sem dependências.

const API = (process.argv[2] ?? 'http://localhost:7000').replace(/\/$/, '');

// Credenciais do demo — troque se for expor publicamente por muito tempo.
const ADMIN = { email: 'admin@delify.com', password: 'Admin@123', fullName: 'Super Admin' };
const OWNER = { name: 'Dono Demo', email: 'demo@delify.com', password: 'Demo@123' };
const WAITER = { name: 'João Garçom', email: 'garcom@delify.com', password: 'Demo@123' };
const SLUG = 'delify-demo';
const NAME = 'Delify Burger Demo';
const LOGO = 'https://picsum.photos/seed/delify-logo/200/200';

const H = (t) => ({ 'Content-Type': 'application/json', ...(t ? { Authorization: `Bearer ${t}` } : {}) });
async function call(m, p, { token, body } = {}) {
  const r = await fetch(API + p, { method: m, headers: H(token), ...(body ? { body: JSON.stringify(body) } : {}) });
  const text = await r.text();
  let data; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  return { status: r.status, ok: r.ok, data };
}
async function must(m, p, opts) {
  const r = await call(m, p, opts);
  if (!r.ok) throw new Error(`${m} ${p} → ${r.status} ${JSON.stringify(r.data)}`);
  return r.data;
}
const img = (seed) => `https://picsum.photos/seed/${seed}/400/300`;

// ── Cardápio do demo (foto real via picsum; troque por fotos suas depois) ──────
const MENU = [
  { name: 'Destaques da casa', order: 0, products: [] }, // marcados abaixo
  { name: 'Burgers', order: 1, products: [
    { name: 'X-Bacon Duplo', price: 34.90, description: 'Dois hambúrgueres 180g, bacon crocante, cheddar', photoUrl: img('xbacon'), featured: 1 },
    { name: 'X-Salada', price: 28.90, description: 'Hambúrguer 180g, alface, tomate e maionese da casa', photoUrl: img('xsalada') },
    { name: 'Veggie Burger', price: 30.00, description: 'Hambúrguer de grão-de-bico, rúcula e tomate seco', photoUrl: img('veggie') },
  ]},
  { name: 'Acompanhamentos', order: 2, products: [
    { name: 'Batata Frita', price: 18.00, description: 'Porção 300g com alecrim', photoUrl: img('fries'), featured: 2 },
    { name: 'Onion Rings', price: 22.00, description: 'Anéis de cebola empanados', photoUrl: img('onion') },
  ]},
  { name: 'Bebidas', order: 3, products: [
    { name: 'Refrigerante Lata', price: 7.00, description: null, photoUrl: img('soda') },
    { name: 'Suco Natural', price: 12.00, description: 'Laranja, limão ou maracujá', photoUrl: img('juice') },
    { name: 'Cerveja Artesanal', price: 16.00, description: 'IPA local 500ml', photoUrl: img('beer') },
  ]},
];

async function run() {
  console.log(`Semeando demo em ${API}\n`);

  // 1. Super admin (one-time; 409 se já existe)
  const setup = await call('POST', '/admin/auth/setup', { body: ADMIN });
  console.log(setup.status === 409 ? '• admin já existia' : '• admin criado');
  const { token: adminToken } = await must('POST', '/admin/auth/login', { body: { email: ADMIN.email, password: ADMIN.password } });

  // 2. Estabelecimento (409 se o slug já existe)
  const est = await call('POST', '/admin/estabelecimentos/', {
    token: adminToken,
    body: { name: NAME, slug: SLUG, ownerName: OWNER.name, ownerEmail: OWNER.email, ownerPassword: OWNER.password },
  });
  console.log(est.status === 409 ? '• estabelecimento já existia' : `• estabelecimento "${SLUG}" criado`);

  // 3. Painel (dono)
  const { token } = await must('POST', '/painel/auth/login', { body: { email: OWNER.email, password: OWNER.password } });

  // 4. Abre + logo + taxa de serviço 10%
  const me = await must('GET', '/painel/me', { token });
  if (!me.isOpen) await must('PATCH', '/painel/me/status', { token });
  await must('PATCH', '/painel/me', {
    token,
    body: { name: NAME, description: 'Hambúrguer artesanal — demo do Delify', logoUrl: LOGO,
            deliveryFee: 8, serviceFeeEnabled: true, serviceFeePercent: 10 },
  });
  console.log('• aberto, logo e taxa de serviço 10% aplicados');

  // 5. Cardápio (pula se já houver categorias)
  const existing = await must('GET', '/painel/cardapio/', { token });
  if (!existing.categories?.length) {
    for (const cat of MENU) {
      if (!cat.products.length) continue; // "Destaques" é uma seção virtual, não categoria
      const created = await must('POST', '/painel/cardapio/categorias', { token, body: { name: cat.name, order: cat.order } });
      for (const p of cat.products) {
        const prod = await must('POST', `/painel/cardapio/categorias/${created.id}/produtos`, {
          token, body: { name: p.name, price: p.price, description: p.description ?? undefined, photoUrl: p.photoUrl },
        });
        if (p.featured) {
          await must('PATCH', `/painel/cardapio/produtos/${prod.id}`, {
            token,
            body: { name: p.name, price: p.price, isAvailable: true, description: p.description ?? undefined,
                    photoUrl: p.photoUrl, isFeatured: true, featuredOrder: p.featured },
          });
        }
      }
      console.log(`• categoria ${cat.name}: ${cat.products.length} produtos`);
    }
  } else {
    console.log('• cardápio já existia — mantido');
  }

  // 6. Mesas
  let mesas = await must('GET', '/painel/mesas/', { token });
  if (!mesas.length) {
    for (const n of ['1', '2', '3', '4', '5', '6']) await must('POST', '/painel/mesas/', { token, body: { number: n } });
    mesas = await must('GET', '/painel/mesas/', { token });
    console.log(`• ${mesas.length} mesas criadas`);
  } else {
    console.log(`• ${mesas.length} mesas já existiam`);
  }

  // 7. Garçom
  const garcons = await must('GET', '/painel/garcons/', { token });
  if (!garcons.length) {
    await must('POST', '/painel/garcons/', { token, body: WAITER });
    console.log('• garçom criado');
  } else {
    console.log('• garçom já existia');
  }

  // ── Resumo ──────────────────────────────────────────────────────────────────
  const front = process.env.FRONT_URL ?? 'http://localhost:3000';
  console.log('\n===== DEMO PRONTO =====');
  console.log(`Cardápio delivery : ${front}/${SLUG}`);
  console.log(`Painel do lojista : ${front}/painel/login   (${OWNER.email} / ${OWNER.password})`);
  console.log(`App do garçom     : ${front}/garcom/login    (${WAITER.email} / ${WAITER.password})`);
  console.log(`Admin             : ${front}/admin/login      (${ADMIN.email} / ${ADMIN.password})`);
  console.log('\nMesas (QR do cliente):');
  for (const m of mesas) console.log(`  Mesa ${m.number} → ${front}/${SLUG}/mesa/${m.qrToken}`);
}

run().catch((e) => { console.error('\nFALHOU:', e.message); process.exit(1); });
