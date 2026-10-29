import { createAsyncStoragePersister } from '@tanstack/query-async-storage-persister';
import type { Query } from '@tanstack/react-query';
import type { PersistQueryClientOptions } from '@tanstack/react-query-persist-client';

/*
 * A escala que o trabalhador já viu fica guardada no aparelho: no cais, sem
 * sinal, ele abre o aplicativo e ela está lá, com a hora em que foi lida.
 * Só a escala — o que ele precisa para chegar ao trabalho —, e não o
 * cadastro, que tem o CPF (ADR-0017).
 *
 * Sair apaga o que ficou guardado: o próximo a entrar no aparelho não vê a
 * escala do anterior.
 */

const Chave = 'escala-avulsos.consultas';

/** Uma semana: depois disso, a escala guardada já é de dias que passaram. */
const UmaSemana = 7 * 24 * 60 * 60 * 1000;

/** A escala da semana, lida com sucesso. */
function guardavel(consulta: Query): boolean {
  const [metodo, caminho] = consulta.queryKey;

  return metodo === 'get' && caminho === '/api/eu/escalas' && consulta.state.status === 'success';
}

export function criarGuarda(): Omit<PersistQueryClientOptions, 'queryClient'> {
  return {
    persister: createAsyncStoragePersister({ storage: globalThis.localStorage, key: Chave }),
    maxAge: UmaSemana,
    dehydrateOptions: { shouldDehydrateQuery: guardavel },
  };
}

export function esquecerOGuardado(): void {
  try {
    localStorage.removeItem(Chave);
  } catch {
    // Nada guardado, nada a esquecer.
  }
}
