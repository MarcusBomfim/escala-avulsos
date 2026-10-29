import { Link } from 'react-router';
import { api } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { Atualizacao } from '../comum/Atualizacao';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import { hojeNoPorto } from '../comum/datas';
import { CartaoDaEscala } from './CartaoDaEscala';
import estilos from './TelaDaMinhaEscala.module.css';

/**
 * A primeira tela, e a mais usada: a escala da semana, de hoje em diante,
 * um cartão por período em que ele se habilitou, com a hora em que foi
 * lida e o botão de ler de novo.
 */
export function TelaDaMinhaEscala() {
  const escalas = api.useQuery('get', '/api/eu/escalas');
  const agora = new Date();
  const hoje = hojeNoPorto(agora);

  return (
    <section aria-labelledby="titulo-da-escala">
      <h1 id="titulo-da-escala">Minha escala</h1>
      {/* A semana é a do relógio do porto, na API (ADR-0016): o dia do celular pode estar errado. */}
      <p className="subtitulo">Hoje e os próximos seis dias.</p>

      {escalas.isPending && <Carregando rotulo="Buscando a sua escala" />}
      {escalas.isError && <AvisoDeProblema problema={comoProblema(escalas.error)} />}

      {/* Com o que já foi lido, a tela continua de pé mesmo quando a leitura de agora falha. */}
      {escalas.data && (
        <>
          <Atualizacao consulta={escalas} oQue="a escala" hoje={hoje} />

          {escalas.data.length === 0 ? (
            <div className="vazio">
              <p>
                <strong>Nenhuma escala nestes dias.</strong>
              </p>
              <p>Para entrar na escala de um período, habilite-se antes de a habilitação fechar.</p>
              <Link to="/habilitacao" className="botao">
                Ver onde posso me habilitar
              </Link>
            </div>
          ) : (
            <div className={estilos.lista}>
              {escalas.data.map((minha) => (
                <CartaoDaEscala key={minha.escala} minha={minha} hoje={hoje} agora={agora.getTime()} />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}
