import { act, render, screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { describe, expect, test } from 'vitest';
import { instalarVoz } from '../testes/voz';
import { Ouvir } from './Ouvir';

describe('ouvir em voz alta', () => {
  test('um toque lê o texto em português, mais devagar; outro para', async () => {
    const voz = instalarVoz();
    render(<Ouvir texto="Hoje, quinta-feira, 5 de novembro. Você vai trabalhar." />);

    await userEvent.click(screen.getByRole('button', { name: 'Ouvir' }));

    expect(voz.ditas.map((fala) => [fala.text, fala.lang, fala.rate, fala.voice?.lang])).toEqual([
      ['Hoje, quinta-feira, 5 de novembro. Você vai trabalhar.', 'pt-BR', 0.9, 'pt-BR'],
    ]);

    await userEvent.click(screen.getByRole('button', { name: 'Parar de ouvir' }));

    expect(voz.cancelar).toHaveBeenCalledTimes(2);
    expect(screen.getByRole('button', { name: 'Ouvir' })).toBeInTheDocument();
  });

  test('quando a fala termina, o botão volta a ser "Ouvir"', async () => {
    const voz = instalarVoz();
    render(<Ouvir texto="Você ficou de fora." rotulo="Ouvir a escala" />);

    await userEvent.click(screen.getByRole('button', { name: 'Ouvir a escala' }));
    act(() => {
      voz.terminar();
    });

    expect(screen.getByRole('button', { name: 'Ouvir a escala' })).toBeInTheDocument();
  });

  test('sem voz no aparelho, não promete o que não tem', () => {
    render(<Ouvir texto="Você vai trabalhar." />);

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
