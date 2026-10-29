/*
 * O service worker do aplicativo: guarda a casca — a página e os arquivos
 * que ela carrega — para o aplicativo abrir sem sinal, no cais (ADR-0017).
 *
 * A casca é lida da própria página: os arquivos compilados têm o conteúdo
 * no nome, e a página diz quais são. A cada vez que o aplicativo abre com
 * sinal, a casca é lida de novo — e a de uma versão antiga sai do aparelho.
 *
 * Os dados não passam por aqui: o que vai para /api segue direto para a
 * rede, e a escala que o trabalhador já viu volta do cache das consultas,
 * guardado pelo próprio aplicativo.
 */

const Casca = 'escala-avulsos-casca';

// O script da página pede com Origin, e a casca foi guardada sem: o Vary da resposta não pode separar os dois.
const Guardado = { ignoreVary: true };

const SempreGuardados = ['/', '/manifest.webmanifest', '/icone.svg', '/icone-192.png', '/icone-512.png'];

async function guardarACasca() {
  const pagina = await fetch('/', { cache: 'no-cache' });

  if (!pagina.ok) {
    return;
  }

  const html = await pagina.clone().text();
  const daPagina = [...html.matchAll(/(?:src|href)="(\/assets\/[^"]+)"/g)].map((achado) => achado[1]);
  const arquivos = [...SempreGuardados, ...daPagina];
  const cache = await caches.open(Casca);

  await cache.addAll(arquivos.filter((arquivo) => arquivo !== '/'));
  await cache.put('/', pagina);

  for (const guardado of await cache.keys()) {
    if (!arquivos.includes(new URL(guardado.url).pathname)) {
      await cache.delete(guardado);
    }
  }
}

self.addEventListener('install', (evento) => {
  evento.waitUntil(guardarACasca().then(() => self.skipWaiting()));
});

self.addEventListener('activate', (evento) => {
  evento.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (evento) => {
  const pedido = evento.request;
  const endereco = new URL(pedido.url);

  if (
    pedido.method !== 'GET' ||
    endereco.origin !== self.location.origin ||
    endereco.pathname.startsWith('/api/')
  ) {
    return;
  }

  // A página vem da rede, se houver rede — e a casca se renova; sem rede, a guardada.
  if (pedido.mode === 'navigate') {
    evento.respondWith(
      fetch(pedido)
        .then((resposta) => {
          evento.waitUntil(guardarACasca().catch(() => undefined));

          return resposta;
        })
        .catch(async () => (await caches.match('/', Guardado)) ?? Response.error()),
    );

    return;
  }

  evento.respondWith(caches.match(pedido, Guardado).then((guardado) => guardado ?? fetch(pedido)));
});
