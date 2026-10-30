import { api } from '../api/cliente';

/**
 * Quantos recados ele ainda não leu, no canto da aba: o número em destaque,
 * e a palavra, para o leitor de tela — "Recados, 2 novos". Sem recado novo,
 * nada.
 */
export function ContagemDosRecados() {
  const recados = api.useQuery('get', '/api/eu/avisos');
  const novos = recados.data?.naoLidos ?? 0;

  if (novos === 0) {
    return null;
  }

  return (
    <>
      {/* Os espaços ficam fora do trecho escondido — dentro, somem do nome —: a aba se lê "Recados 2 novos". */}{' '}
      <span className="contagem">
        {novos} <span className="somente-leitor">{novos === 1 ? 'novo' : 'novos'}</span>
      </span>
    </>
  );
}
