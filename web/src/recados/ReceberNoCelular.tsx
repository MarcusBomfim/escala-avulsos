import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { api, http } from '../api/cliente';
import { Icone } from '../comum/Icone';
import { desinscrever, inscrever, inscricaoAtual, pushNoNavegador } from './push';
import estilos from './ReceberNoCelular.module.css';

type Estado = 'trabalhando' | 'negado' | 'falhou' | null;

/**
 * O recado no celular, mesmo com o aplicativo fechado: um toque para
 * receber, e o navegador pergunta se pode mostrar notificações (ADR-0020).
 * Se o servidor não manda push, ou o navegador não recebe, a tela diz o que
 * dá para fazer, e o resto continua igual: os recados ficam aqui.
 */
export function ReceberNoCelular() {
  const chave = api.useQuery('get', '/api/eu/avisos/push', {}, { retry: false });
  const noNavegador = pushNoNavegador();
  const atual = useQuery({
    queryKey: ['push', 'inscricao'],
    queryFn: inscricaoAtual,
    enabled: noNavegador && chave.isSuccess,
  });
  const [estado, definirEstado] = useState<Estado>(null);

  if (!chave.isSuccess) {
    return null;
  }

  if (!noNavegador) {
    return (
      <div className={estilos.celular}>
        <p>
          Este navegador não recebe os recados como notificação. No celular, abra o aplicativo pelo ícone da
          tela inicial: no iPhone, instale antes pelo botão Compartilhar, em &quot;Adicionar à Tela de
          Início&quot;.
        </p>
      </div>
    );
  }

  const inscrito = atual.data != null;

  async function receber(chavePublica: string) {
    definirEstado('trabalhando');

    try {
      const inscricao = await inscrever(chavePublica);

      if (!inscricao) {
        definirEstado('negado');

        return;
      }

      const { response } = await http.PUT('/api/eu/avisos/push', { body: inscricao });
      definirEstado(response.ok ? null : 'falhou');
    } catch {
      definirEstado('falhou');
    }

    await atual.refetch();
  }

  async function parar() {
    definirEstado('trabalhando');

    try {
      const endereco = await desinscrever();

      if (endereco) {
        await http.DELETE('/api/eu/avisos/push', { body: { endereco } });
      }

      definirEstado(null);
    } catch {
      definirEstado('falhou');
    }

    await atual.refetch();
  }

  return (
    <div className={estilos.celular}>
      <p role="status">
        {inscrito
          ? 'Os recados chegam neste celular, mesmo com o aplicativo fechado.'
          : 'Receba os recados como notificação no celular, mesmo com o aplicativo fechado.'}
      </p>
      {estado === 'negado' && (
        <p className={estilos.problema}>
          O celular não deixou mostrar as notificações. Para ligar, abra os ajustes do navegador, em
          Notificações, e permita este aplicativo.
        </p>
      )}
      {estado === 'falhou' && (
        <p className={estilos.problema}>Não deu certo agora. Tente de novo daqui a pouco.</p>
      )}
      {inscrito ? (
        <button
          type="button"
          className="secundario"
          disabled={estado === 'trabalhando'}
          onClick={() => void parar()}
        >
          Parar de receber no celular
        </button>
      ) : (
        <button
          type="button"
          disabled={estado === 'trabalhando'}
          onClick={() => void receber(chave.data.chavePublica)}
        >
          <Icone nome="recados" />
          Receber os recados no celular
        </button>
      )}
    </div>
  );
}
