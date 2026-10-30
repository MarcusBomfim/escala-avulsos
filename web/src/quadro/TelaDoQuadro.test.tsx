import { screen, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import { quadroComAHabilitacaoAberta, quadroDepoisDaEscala } from '../testes/exemplosDoQuadro';
import { servidor } from '../testes/servidor';
import { tokenDeTeste } from '../testes/tokens';
import { periodoDeAgora } from './periodo';

/** O token do escalador, feito na hora do teste: feito antes, venceria com o relógio adiantado para a quinta. */
const doEscalador = () => tokenDeTeste({ role: 'escalador', name: 'Escala do dia', matricula: undefined });

// Quinta, 05/11/2026, às 9h, em Santos: o quadro abre na manhã, que é o período de agora.
beforeEach(() => {
  vi.useFakeTimers({ now: new Date('2026-11-05T09:00:00-03:00'), toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

describe('o quadro', () => {
  test('o escalador entra direto no quadro, e ele mostra a escalação e o terno como no quadro de parede', async () => {
    servidor.use(
      http.get('/api/quadro/2026-11-05/Manha', () =>
        HttpResponse.json({ ...quadroComAHabilitacaoAberta, categorias: [] }),
      ),
      http.get('/api/quadro/2026-11-05/Tarde', () => HttpResponse.json(quadroComAHabilitacaoAberta)),
    );
    const { roteador } = abrir('/', { token: doEscalador() });

    expect(await screen.findByText('Nenhuma requisição neste período.')).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/quadro');
    expect(
      within(screen.getByRole('navigation', { name: 'Principal' }))
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual(['Quadro']);
    expect(screen.getByRole('banner')).toHaveTextContent('Escala do diaEscalador');

    await userEvent.selectOptions(screen.getByLabelText('Turno'), 'Tarde');

    const estiva = await screen.findByRole('region', { name: 'Estiva' });
    expect(estiva).toHaveTextContent('Habilitação aberta');
    expect(estiva).toHaveTextContent('6 habilitados. A habilitação fecha às 10h.');
    const terno = within(estiva).getByRole('article', { name: 'Atlântico Sul, terno 1' });
    expect(terno).toHaveTextContent('Aguardando a escala');
    expect(terno).toHaveTextContent('Terminal Cais Leste, berço 4 — Cais Forte Operações Portuárias');
    expect(terno).toHaveTextContent('Pedidas3Escaladas0Em aberto3');
    expect(within(terno).queryByText('Quem está em cada vaga')).not.toBeInTheDocument();
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('depois da escala, o terno diz quanto falta e quem está em cada vaga', async () => {
    servidor.use(http.get('/api/quadro/2026-11-05/Manha', () => HttpResponse.json(quadroDepoisDaEscala)));
    abrir('/quadro', { token: doEscalador() });

    const terno = await screen.findByRole('article', { name: 'Atlântico Sul, terno 1' });

    expect(within(screen.getByRole('region', { name: 'Estiva' })).getByText('Escalada')).toBeInTheDocument();
    expect(terno).toHaveTextContent('Falta 1');
    expect(terno).toHaveTextContent('Pedidas3Escaladas2Em aberto1');

    await userEvent.click(within(terno).getByText('Quem está em cada vaga'));

    expect(terno).toHaveTextContent('Sinaleiro (1 de 1): Antônio Ferreira, matrícula 4127');
    expect(terno).toHaveTextContent('Estivador (1 de 2): Benedito Lima, matrícula 4130');
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('o operador vê os ternos da empresa dele, sem o nome dela repetido em cada um', async () => {
    servidor.use(http.get('/api/quadro/2026-11-05/Manha', () => HttpResponse.json(quadroDepoisDaEscala)));
    abrir('/quadro', {
      token: tokenDeTeste({
        role: 'operador',
        cnpj: '11222333000181',
        name: 'Cais Forte',
        matricula: undefined,
      }),
    });

    const terno = await screen.findByRole('article', { name: 'Atlântico Sul, terno 1' });

    expect(
      screen.getByText('Os ternos que a sua empresa pediu para o período, e quem está em cada vaga.'),
    ).toBeInTheDocument();
    expect(terno).not.toHaveTextContent('Cais Forte Operações Portuárias');
    expect(screen.getByRole('banner')).toHaveTextContent('Cais ForteOperador portuário');
  });

  test('o trabalhador não tem quadro: volta para a escala dele', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([])));
    const { roteador } = abrir('/quadro');

    expect(await screen.findByRole('heading', { name: 'Minha escala', level: 1 })).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/');
  });
});

describe('o período de agora, no relógio do porto', () => {
  test('cada hora cai no seu turno, e a meia-noite e meia ainda é a noite de ontem', () => {
    expect(periodoDeAgora(new Date('2026-11-05T00:30:00-03:00'))).toEqual({
      dia: '2026-11-04',
      turno: 'Noite',
    });
    expect(periodoDeAgora(new Date('2026-11-05T01:00:00-03:00'))).toEqual({
      dia: '2026-11-05',
      turno: 'Madrugada',
    });
    expect(periodoDeAgora(new Date('2026-11-05T12:59:00-03:00'))).toEqual({
      dia: '2026-11-05',
      turno: 'Manha',
    });
    expect(periodoDeAgora(new Date('2026-11-05T13:00:00-03:00'))).toEqual({
      dia: '2026-11-05',
      turno: 'Tarde',
    });
    expect(periodoDeAgora(new Date('2026-11-05T19:00:00-03:00'))).toEqual({
      dia: '2026-11-05',
      turno: 'Noite',
    });
    expect(periodoDeAgora(new Date('2026-11-01T00:10:00-03:00'))).toEqual({
      dia: '2026-10-31',
      turno: 'Noite',
    });
  });
});
