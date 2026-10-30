import type { Esquemas } from '../api/cliente';

/*
 * Os eventos do servidor (ADR-0020), lidos pelo fetch, e não pelo
 * EventSource: o EventSource não manda o token no cabeçalho, e token no
 * endereço iria parar nos registros de acesso. O formato é o do Server-Sent
 * Events: linhas "event:" e "data:", e uma linha vazia no fim de cada evento.
 */

export type DadosDoEvento = Esquemas['EventoResposta'];

export interface EventoDoServidor {
  /** "pronto" ao conectar, "mudou" a cada anúncio; "vivo" é só sinal de vida, e não chega aqui. */
  readonly tipo: 'pronto' | 'mudou';
  readonly dados: DadosDoEvento;
}

const Vazio: DadosDoEvento = { origem: null, categoria: null, dia: null, turno: null };

/** Os eventos inteiros do texto recebido até agora, e o pedaço do próximo, que ainda não chegou todo. */
export function lerEventos(texto: string): { readonly eventos: EventoDoServidor[]; readonly resto: string } {
  const blocos = texto.replaceAll('\r\n', '\n').split('\n\n');
  const resto = blocos.pop() ?? '';
  const eventos: EventoDoServidor[] = [];

  for (const bloco of blocos) {
    let tipo = 'message';
    const dados: string[] = [];

    for (const linha of bloco.split('\n')) {
      if (linha.startsWith('event:')) {
        tipo = linha.slice('event:'.length).trim();
      } else if (linha.startsWith('data:')) {
        dados.push(linha.slice('data:'.length).trimStart());
      }
    }

    if (tipo === 'pronto' || tipo === 'mudou') {
      eventos.push({ tipo, dados: dados.length > 0 ? ler(dados.join('\n')) : Vazio });
    }
  }

  return { eventos, resto };
}

function ler(json: string): DadosDoEvento {
  try {
    return { ...Vazio, ...(JSON.parse(json) as Partial<DadosDoEvento>) };
  } catch {
    return Vazio;
  }
}

/**
 * Escuta os eventos até a ligação acabar — o servidor caiu, o sinal foi
 * embora — ou o sinal de parar chegar. Quem chama abre de novo, se quiser.
 */
export async function escutar(
  token: string,
  aoReceber: (evento: EventoDoServidor) => void,
  parar: AbortSignal,
): Promise<void> {
  const resposta = await fetch('/api/eventos', {
    headers: { Authorization: `Bearer ${token}`, Accept: 'text/event-stream' },
    signal: parar,
  });

  if (!resposta.ok || !resposta.body) {
    throw new Error(`O servidor recusou os eventos: ${String(resposta.status)}.`);
  }

  const leitor = resposta.body.pipeThrough(new TextDecoderStream()).getReader();
  let pendente = '';

  for (;;) {
    const { done, value } = await leitor.read();

    if (done) {
      return;
    }

    const { eventos, resto } = lerEventos(pendente + value);
    pendente = resto;
    eventos.forEach(aoReceber);
  }
}
