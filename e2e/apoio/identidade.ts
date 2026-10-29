import { createHmac } from 'node:crypto';

/*
 * O provedor de identidade dos testes: uma chave simétrica que a API aceita
 * por configuração — como aceita a do `dotnet user-jwts` em desenvolvimento
 * — e com que os testes assinam os seus tokens. Não é segredo: só vale na
 * API do ensaio.
 */

export const Emissor = 'escala-avulsos-e2e';

export const Audiencia = 'escala-avulsos-e2e';

export const ChaveEmBase64 = Buffer.from('escala-avulsos-e2e: chave dos testes de ponta a ponta').toString(
  'base64',
);

/** A configuração do JWT da API, em variáveis de ambiente: emissor, audiência e chave. */
export const ConfiguracaoDoJwt = {
  Authentication__Schemes__Bearer__ValidIssuer: Emissor,
  Authentication__Schemes__Bearer__ValidAudiences__0: Audiencia,
  Authentication__Schemes__Bearer__SigningKeys__0__Issuer: Emissor,
  Authentication__Schemes__Bearer__SigningKeys__0__Value: ChaveEmBase64,
};

const emBase64Url = (texto: string) => Buffer.from(texto).toString('base64url');

/** Um JWT HS256 assinado de verdade, que a API confere como confere qualquer outro. Vale um ano: o relógio do ensaio anda. */
function assinar(carga: Record<string, unknown>): string {
  const agora = Math.floor(Date.now() / 1000);
  const umAno = 365 * 24 * 60 * 60;
  const cabecalho = emBase64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const corpo = emBase64Url(
    JSON.stringify({
      ...carga,
      iss: Emissor,
      aud: Audiencia,
      nbf: agora - 5,
      iat: agora,
      exp: agora + umAno,
    }),
  );
  const assinatura = createHmac('sha256', Buffer.from(ChaveEmBase64, 'base64'))
    .update(`${cabecalho}.${corpo}`)
    .digest('base64url');

  return `${cabecalho}.${corpo}.${assinatura}`;
}

/** O token do trabalhador: o papel, a matrícula e o nome. */
export function tokenDoTrabalhador(matricula: number, nome: string): string {
  return assinar({
    sub: `trabalhador-${matricula}`,
    role: 'trabalhador',
    matricula: String(matricula),
    name: nome,
  });
}

/** O token do órgão gestor. */
export function tokenDoEscalador(): string {
  return assinar({ sub: 'helena.escalacao', role: 'escalador', name: 'Helena Martins' });
}

/** O token do operador portuário: o CNPJ e o nome. */
export function tokenDoOperador(): string {
  return assinar({
    sub: 'cais-forte',
    role: 'operador',
    cnpj: '11.222.333/0001-81',
    name: 'Cais Forte Operações Portuárias',
  });
}
