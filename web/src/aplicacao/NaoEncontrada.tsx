import { Link } from 'react-router';

export function NaoEncontrada() {
  return (
    <section aria-labelledby="titulo-nao-encontrada">
      <h1 id="titulo-nao-encontrada">Esta tela não existe</h1>
      <p>O endereço não leva a nenhuma tela do aplicativo.</p>
      <Link to="/" className="botao">
        Voltar para a minha escala
      </Link>
    </section>
  );
}
