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

/*
 * O recado que chega com o aplicativo fechado (ADR-0020): o servidor manda,
 * cifrado, o título, o texto, a tela que o toque abre e de que fato ele é. A
 * notificação nova do mesmo fato substitui a antiga, e toca de novo.
 */
self.addEventListener('push', (evento) => {
  let recado = {
    titulo: 'Escala dos Avulsos',
    texto: 'Há recado novo para você.',
    endereco: '/recados',
    etiqueta: 'recado',
  };

  try {
    recado = { ...recado, ...evento.data.json() };
  } catch {
    // Sem conteúdo que se leia, o aviso genérico: abrir os recados resolve.
  }

  evento.waitUntil(
    self.registration.showNotification(recado.titulo, {
      body: recado.texto,
      tag: recado.etiqueta,
      renotify: true,
      icon: '/icone-192.png',
      badge: '/icone-192.png',
      lang: 'pt-BR',
      data: { endereco: recado.endereco },
    }),
  );
});

// O toque na notificação abre a tela do recado: na janela do aplicativo que já estiver aberta, ou numa nova.
self.addEventListener('notificationclick', (evento) => {
  evento.notification.close();
  const endereco = new URL(evento.notification.data?.endereco ?? '/recados', self.location.origin).href;

  evento.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(async (janelas) => {
      const aberta = janelas.find((janela) => new URL(janela.url).origin === self.location.origin);

      if (aberta) {
        await aberta.focus();

        return aberta.navigate(endereco);
      }

      return self.clients.openWindow(endereco);
    }),
  );
});
