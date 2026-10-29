import { useEffect, useRef } from 'react';
import { Navigate, NavLink, Outlet, useLocation } from 'react-router';
import { Icone } from '../comum/Icone';
import { Relogio } from '../comum/Relogio';
import { BotaoDaLetra, BotaoDoContraste } from '../leitura/AjustesDaLeitura';
import { AvisoDeConexao } from '../offline/AvisoDeConexao';
import { useSessao } from '../sessao/contexto';
import { entradasDoMenu } from './modulos';
import estilos from './Moldura.module.css';

/**
 * O que toda tela tem em volta, no desenho do sistema que o trabalhador já
 * conhece: a faixa azul com quem entrou e o "Sair" vermelho no canto; as
 * abas das áreas, cada uma com o ícone e a palavra; e a barra cinza, com o
 * relógio do porto e a letra, em cima de toda tela. No pé, o contraste, que
 * se escolhe uma vez. Sem sessão, manda para a entrada, e volta para cá
 * depois.
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
            <strong>{sessao.nome ?? 'Trabalhador'}</strong>
            <span>Matrícula {sessao.matricula}</span>
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
            {entradasDoMenu().map((entrada) => (
              <li key={entrada.caminho}>
                <NavLink to={entrada.caminho} end={entrada.caminho === '/'}>
                  <Icone nome={entrada.icone} />
                  {entrada.rotulo}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className={estilos.barra}>
          <Relogio />
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
