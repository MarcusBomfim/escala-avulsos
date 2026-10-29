import { expect, request, type APIRequestContext } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { tokenDoEscalador, tokenDoOperador, tokenDoTrabalhador } from './identidade';
import { Api } from './pilha';

/*
 * A API usada como cada um a usaria, do seu lado: o órgão gestor altera com
 * a versão lida, o operador cria com chave de idempotência, o trabalhador
 * sem nenhuma das duas (ADR-0014, ADR-0015). É assim que a tarde de quinta
 * chega ao banco antes de o celular abrir.
 */

export type Lado = APIRequestContext;

async function lado(token: string): Promise<Lado> {
  return request.newContext({ baseURL: Api, extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
}

export const doEscalador = () => lado(tokenDoEscalador());

export const doOperador = () => lado(tokenDoOperador());

export const doTrabalhador = (matricula: number, nome: string) => lado(tokenDoTrabalhador(matricula, nome));

/** Acerta o relógio da API do ensaio: a partir daqui, a API acha que são estas horas. */
export async function acertarORelogio(agora: string): Promise<void> {
  const ensaio = await request.newContext({ baseURL: Api });
  const resposta = await ensaio.put('/ensaio/relogio', { data: { agora } });

  expect(resposta.status(), `PUT /ensaio/relogio: ${await resposta.text()}`).toBe(204);
  await ensaio.dispose();
}

/** Manda o pedido e confere o status; devolve a resposta. */
export async function enviar(
  api: Lado,
  metodo: 'GET' | 'POST' | 'PUT' | 'DELETE',
  caminho: string,
  {
    corpo,
    versao,
    chave,
    status = [200, 201],
  }: { corpo?: unknown; versao?: string; chave?: boolean; status?: number[] } = {},
) {
  const resposta = await api.fetch(caminho, {
    method: metodo,
    ...(corpo === undefined ? {} : { data: corpo }),
    headers: {
      ...(versao ? { 'If-Match': versao } : {}),
      ...(chave ? { 'Idempotency-Key': randomUUID() } : {}),
    },
  });

  expect(status, `${metodo} ${caminho}: ${resposta.status()} ${await resposta.text()}`).toContain(
    resposta.status(),
  );

  return resposta;
}

/** Um CPF válido para cada matrícula, como nos testes da API: o da 4127 é 529.984.127-26. */
export function cpfDaMatricula(matricula: number): string {
  let digitos = String(529_980_000 + matricula);

  for (let vez = 0; vez < 2; vez += 1) {
    const soma = [...digitos].reduce(
      (total, digito, posicao) => total + Number(digito) * (digitos.length + 1 - posicao),
      0,
    );
    const resto = soma % 11;
    digitos += resto < 2 ? '0' : String(11 - resto);
  }

  return `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-${digitos.slice(9)}`;
}

/** O órgão gestor cadastra um estivador, qualificado desde 1994, com o exame válido até 30/11/2026. */
export async function cadastrarEstivador(
  escalador: Lado,
  matricula: number,
  nome: string,
  { inscricao = 'Registrado', outrasFuncoes = [] }: { inscricao?: string; outrasFuncoes?: string[] } = {},
): Promise<void> {
  const criado = await enviar(escalador, 'POST', '/api/trabalhadores', {
    corpo: { matricula, nome, cpf: cpfDaMatricula(matricula), categoria: 'Estiva', inscricao },
  });
  let versao = criado.headers().etag ?? '';

  for (const funcao of ['Estivador', ...outrasFuncoes]) {
    const qualificado = await enviar(escalador, 'POST', `/api/trabalhadores/${matricula}/qualificacoes`, {
      corpo: { funcao, desde: '1994-03-01' },
      versao,
    });
    versao = qualificado.headers().etag ?? '';
  }

  await enviar(escalador, 'PUT', `/api/trabalhadores/${matricula}/aso`, {
    corpo: { emitidoEm: '2025-12-01', validoAte: '2026-11-30' },
    versao,
  });
}
