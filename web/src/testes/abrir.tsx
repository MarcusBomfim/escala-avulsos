import { render } from '@testing-library/react';
import { createMemoryRouter } from 'react-router';
import { Aplicacao, type Guarda } from '../aplicacao/Aplicacao';
import { criarConsultas } from '../aplicacao/consultas';
import { criarRotas } from '../aplicacao/rotas';
import { guardarToken } from '../sessao/token';
import { tokenDeTeste } from './tokens';

/**
 * O aplicativo inteiro aberto num endereço, como quem chega por um link: as
 * rotas de verdade, a sessão de verdade, só o roteador em memória. Com o
 * token do Antônio, a menos que o teste diga outro — ou nenhum. Sem guardar
 * nada no aparelho, a menos que o teste dê a guarda. Sem escutar os eventos
 * do servidor, a menos que o teste trate disso. Devolve também o cache das
 * consultas, para o teste saber o que ficou na memória.
 */
export function abrir(
  caminho: string,
  {
    token = tokenDeTeste(),
    guarda,
    tempoReal = false,
  }: { readonly token?: string | null; readonly guarda?: Guarda; readonly tempoReal?: boolean } = {},
) {
  if (token !== null) {
    guardarToken(token);
  }

  const roteador = createMemoryRouter(criarRotas(), { initialEntries: [caminho] });
  const consultas = criarConsultas({ tentativas: 0 });
  const tela = render(
    <Aplicacao roteador={roteador} consultas={consultas} guarda={guarda} tempoReal={tempoReal} />,
  );

  return { ...tela, roteador, consultas };
}
