import { useEffect, useRef } from 'react';
import { Navigate, NavLink, Outlet, useLocation } from 'react-router';
import { Icone } from '../comum/Icone';
import { Relogio } from '../comum/Relogio';
import { BotaoDaLetra, BotaoDoContraste } from '../leitura/AjustesDaLeitura';
import { AvisoDeConexao } from '../offline/AvisoDeConexao';
import { useSessao } from '../sessao/contexto';
import type { Sessao } from '../sessao/token';
import { LigacaoAoVivo } from '../tempoReal/LigacaoAoVivo';
import { entradasDoMenu } from './modulos';
import estilos from './Moldura.module.css';

const Papeis: Readonly<Record<Sessao['papel'], string>> = {
  trabalhador: 'Trabalhador',
  escalador: 'Escalador',
  operador: 'Operador portuário',
};

/** Embaixo do nome: a matrícula do trabalhador, ou o papel de quem vê o quadro. */
function quemE(sessao: Sessao): string {
  return sessao.papel === 'trabalhador' ? `Matrícula ${String(sessao.matricula)}` : Papeis[sessao.papel];
}

/**
 * O que toda tela tem em volta, no desenho do sistema que o trabalhador já
 * conhece: a faixa azul com quem entrou e o "Sair" vermelho no canto; as
 * abas das áreas, cada uma com o ícone e a palavra; e a barra cinza, com o
 * relógio do porto, a ligação ao vivo e a letra, em cima de toda tela. No
 * pé, o contraste, que se escolhe uma vez. Sem sessão, manda para a entrada,
 * e volta para cá depois. O escalador e o operador têm só o quadro: o resto
 * é do trabalhador.
 */
export function Moldura() {
  const { sessao, sair } = useSessao();
  const local = useLocation();
  const conteudo = useRef<HTMLElement>(null);
  const primeira = useRef(true);

  // Trocando de tela, o leitor de tela começa pela tela nova, e não pelo menu de novo.
  useEffect(() => {
    if (primeira.current) {
      primeira.current = false;

      return;
    }

    conteudo.current?.focus();
  }, [local.pathname]);

  if (!sessao) {
    return <Navigate to={`/entrar?de=${encodeURIComponent(local.pathname + local.search)}`} replace />;
  }

  const doQuadro = local.pathname === '/quadro' || local.pathname.startsWith('/quadro/');

  if (sessao.papel !== 'trabalhador' && !doQuadro) {
    return <Navigate to="/quadro" replace />;
  }

  if (sessao.papel === 'trabalhador' && doQuadro) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className={estilos.moldura}>
      <a href="#conteudo" className={estilos.pular}>
        Pular para a tela
      </a>

      <header className={estilos.topo}>
        <div className={estilos.faixa}>
          <span className={estilos.marca}>
            <Icone nome="marca" />
          </span>
          <p className={estilos.quem}>
            <strong>{sessao.nome ?? Papeis[sessao.papel]}</strong>
            <span>{quemE(sessao)}</span>
          </p>
          <button
            type="button"
            className={estilos.sair}
            onClick={() => {
              sair();
            }}
          >
            <Icone nome="sair" />
            {/* O espaço fica fora do trecho escondido: dentro, ele some do nome do botão. */}
            Sair <span className="somente-leitor">do aplicativo</span>
          </button>
        </div>

        <nav aria-label="Principal" className={estilos.abas}>
          <ul className={estilos.menu}>
            {entradasDoMenu(sessao.papel).map((entrada) => (
              <li key={entrada.caminho}>
                <NavLink to={entrada.caminho} end={entrada.caminho === '/'}>
                  <Icone nome={entrada.icone} />
                  {entrada.rotulo}
                  {entrada.Contagem && <entrada.Contagem />}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className={estilos.barra}>
          <div className={estilos.horas}>
            <Relogio />
            <LigacaoAoVivo />
          </div>
          <BotaoDaLetra />
        </div>
      </header>

      <main id="conteudo" ref={conteudo} tabIndex={-1} className={estilos.conteudo}>
        <AvisoDeConexao />
        <Outlet />
      </main>

      <footer className={estilos.pe}>
        <p className={estilos.nome}>Escala dos Avulsos</p>
        <BotaoDoContraste />
      </footer>
    </div>
  );
}
