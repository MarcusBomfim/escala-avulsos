import { useEffect, useState } from 'react';
import { horaNoPorto } from './datas';
import { Icone } from './Icone';
import estilos from './Relogio.module.css';

/** De quanto em quanto o relógio confere a hora: a cada minuto, ele está certo em até 15 segundos. */
const Intervalo = 15_000;

/**
 * O relógio do alto da tela, como no sistema de hoje, mas na hora do porto e
 * escrito como o resto do aplicativo — "Agora: 9h40" —, para comparar de
 * olho com "a habilitação fecha às 10h". Não é uma região viva: o leitor de
 * tela lê a hora quando passa por ela, e não a cada minuto.
 */
export function Relogio() {
  const [agora, definirAgora] = useState(() => new Date());

  useEffect(() => {
    const relogio = setInterval(() => {
      definirAgora(new Date());
    }, Intervalo);

    return () => {
      clearInterval(relogio);
    };
  }, []);

  return (
    <p className={estilos.relogio}>
      <Icone nome="relogio" />
      <span>
        Agora: <strong>{horaNoPorto(agora)}</strong>
      </span>
    </p>
  );
}
