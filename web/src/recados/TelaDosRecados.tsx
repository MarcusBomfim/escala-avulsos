import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { Link } from 'react-router';
import { api, http, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { Atualizacao } from '../comum/Atualizacao';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import { hojeNoPorto, quando } from '../comum/datas';
import { Selo } from '../comum/Selo';
import { ReceberNoCelular } from './ReceberNoCelular';
import estilos from './TelaDosRecados.module.css';

type Recado = Esquemas['AvisoResposta'];

/** Para onde cada recado leva: a tela em que se resolve o que ele diz. */
const OndeResolver: Readonly<Record<Recado['tipo'], { readonly caminho: string; readonly rotulo: string }>> =
  {
    VaiTrabalhar: { caminho: '/', rotulo: 'Ver a minha escala' },
    FicouDeFora: { caminho: '/', rotulo: 'Ver a minha escala' },
    EntrouPelaEspera: { caminho: '/', rotulo: 'Ver a minha escala' },
    HabilitacaoVaiFechar: { caminho: '/habilitacao', rotulo: 'Habilitar-me agora' },
    ExameVaiVencer: { caminho: '/meus-dados', rotulo: 'Ver os meus dados' },
    ReciclagemVaiVencer: { caminho: '/meus-dados', rotulo: 'Ver os meus dados' },
  };

/**
 * Os recados, como na primeira tela do sistema de hoje — a escala que saiu,
 * a vaga que passou para ele, a habilitação que vai fechar, o exame que vai
 * vencer (ADR-0019) —, do mais novo para o mais antigo. Abrir a tela marca
 * os novos como lidos: a aba deixa de contar, e o "Novo" continua até ele
 * sair daqui.
 */
export function TelaDosRecados() {
  const consultas = useQueryClient();
  const recados = api.useQuery('get', '/api/eu/avisos');
  const naoLidos = recados.data?.naoLidos ?? 0;
  const hoje = hojeNoPorto();

  useEffect(() => {
    if (naoLidos === 0) {
      return;
    }

    void http.POST('/api/eu/avisos/lidos').then(({ response }) => {
      if (response.ok) {
        consultas.setQueriesData<Esquemas['MeusAvisosResposta']>(
          { queryKey: ['get', '/api/eu/avisos'] },
          (antes) => (antes ? { ...antes, naoLidos: 0 } : antes),
        );
      }
    });
  }, [naoLidos, consultas]);

  return (
    <section aria-labelledby="titulo-dos-recados">
      <h1 id="titulo-dos-recados">Recados</h1>
      <p className="subtitulo">O que mudou na sua escala, e o que precisa da sua atenção.</p>

      <ReceberNoCelular />

      {recados.isPending && <Carregando rotulo="Buscando os seus recados" />}
      {recados.isError && <AvisoDeProblema problema={comoProblema(recados.error)} />}

      {recados.data && (
        <>
          <Atualizacao consulta={recados} oQue="os recados" hoje={hoje} />

          {recados.data.avisos.length === 0 ? (
            <div className="vazio">
              <p>
                <strong>Nenhum recado por enquanto.</strong>
              </p>
              <p>Quando a escala sair, ou a habilitação estiver para fechar, o recado aparece aqui.</p>
            </div>
          ) : (
            <ul className={estilos.lista}>
              {recados.data.avisos.map((recado) => (
                <li key={`${recado.tipo}-${recado.criadoEm}`}>
                  <CartaoDoRecado recado={recado} hoje={hoje} />
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}

function CartaoDoRecado({ recado, hoje }: { readonly recado: Recado; readonly hoje: string }) {
  const resolver = OndeResolver[recado.tipo];

  return (
    <article
      className={recado.lido ? estilos.recado : `${estilos.recado ?? ''} ${estilos.novo ?? ''}`}
      aria-label={recado.titulo}
    >
      <div className={estilos.cabecalho}>
        <h2>{recado.titulo}</h2>
        {!recado.lido && <Selo tom="atencao">Novo</Selo>}
      </div>
      <p className={estilos.quando}>{comoFrase(quando(recado.criadoEm, hoje))}</p>
      <p>{recado.texto}</p>
      <Link to={resolver.caminho} className="botao secundario">
        {resolver.rotulo}
      </Link>
    </article>
  );
}

function comoFrase(texto: string): string {
  return `${texto.charAt(0).toUpperCase()}${texto.slice(1)}`;
}
