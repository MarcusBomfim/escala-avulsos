import { useState, type SubmitEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { Icone } from '../comum/Icone';
import { AjustesDaLeitura } from '../leitura/AjustesDaLeitura';
import { useSessao } from './contexto';
import { destinoSeguro } from './destino';
import estilos from './TelaDeEntrada.module.css';

/**
 * Em produção, a entrada é a do provedor de identidade, com a matrícula e a
 * senha. Em desenvolvimento, cola-se aqui o token gerado pelo `dotnet
 * user-jwts`, com a matrícula de quem se quer ser (ADR-0017). A letra e o
 * contraste já valem aqui: é a primeira tela que se lê. O cartão é o da
 * entrada do sistema de hoje: a marca em cima, o título, o campo e o botão
 * azul da largura toda.
 */
export function TelaDeEntrada() {
  const { entrar, aviso } = useSessao();
  const navegar = useNavigate();
  const [parametros] = useSearchParams();
  const [token, definirToken] = useState('');
  const [erro, definirErro] = useState<string | null>(null);

  function enviar(evento: SubmitEvent<HTMLFormElement>) {
    evento.preventDefault();

    const motivo = entrar(token);

    if (motivo) {
      definirErro(motivo);

      return;
    }

    void navegar(destinoSeguro(parametros.get('de')), { replace: true });
  }

  return (
    <main className={estilos.pagina}>
      <AjustesDaLeitura />

      <form className={estilos.cartao} onSubmit={enviar} aria-labelledby="titulo-entrada">
        <p className={estilos.marca}>
          <span className={estilos.simbolo}>
            <Icone nome="marca" />
          </span>
          Escala dos Avulsos
        </p>
        <h1 id="titulo-entrada">Minha escala</h1>
        <p className={estilos.chamada}>A escala do trabalhador portuário avulso.</p>

        {aviso && (
          <p className={estilos.aviso} role="status">
            {aviso}
          </p>
        )}

        <label htmlFor="token">Token de acesso</label>
        <textarea
          id="token"
          rows={4}
          spellCheck={false}
          autoComplete="off"
          value={token}
          onChange={(evento) => {
            definirToken(evento.target.value);
            definirErro(null);
          }}
          aria-invalid={erro !== null}
          aria-describedby={erro ? 'erro-do-token' : 'ajuda-do-token'}
        />

        {erro && (
          <p id="erro-do-token" className={estilos.erro} role="alert">
            {erro}
          </p>
        )}

        <button type="submit" disabled={token.trim() === ''}>
          Entrar
        </button>

        <p id="ajuda-do-token" className={estilos.ajuda}>
          Em desenvolvimento, gere o token com a matrícula de quem quiser ser:
          <code>
            dotnet user-jwts create --project src/EscalaAvulsos.Api --role trabalhador --claim matricula=4127
            --name &quot;Antônio Ferreira&quot;
          </code>
        </p>
      </form>
    </main>
  );
}
