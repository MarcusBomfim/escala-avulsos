import { render, screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { describe, expect, test, vi } from 'vitest';
import { AjustesDaLeitura } from './AjustesDaLeitura';
import { lerPreferencias } from './preferencias';
import { ProvedorDaLeitura } from './ProvedorDaLeitura';

function abrirAjustes() {
  render(
    <ProvedorDaLeitura>
      <AjustesDaLeitura />
    </ProvedorDaLeitura>,
  );
}

const raiz = () => document.documentElement.dataset;

describe('a letra e o contraste', () => {
  test('a letra aumenta com um toque, até a maior, e volta à normal', async () => {
    abrirAjustes();

    await userEvent.click(screen.getByRole('button', { name: 'Letra: normal' }));
    expect(raiz().letra).toBe('grande');
    expect(screen.getByRole('status')).toHaveTextContent('Letra grande.');

    await userEvent.click(screen.getByRole('button', { name: 'Letra: grande' }));
    expect(raiz().letra).toBe('muito-grande');

    await userEvent.click(screen.getByRole('button', { name: 'Letra: muito grande' }));
    expect(raiz().letra).toBe('normal');
  });

  test('o contraste alto liga e desliga, e o botão diz se está ligado', async () => {
    abrirAjustes();
    const contraste = screen.getByRole('button', { name: 'Contraste alto' });

    await userEvent.click(contraste);
    expect(contraste).toHaveAttribute('aria-pressed', 'true');
    expect(raiz().contraste).toBe('alto');

    await userEvent.click(contraste);
    expect(contraste).toHaveAttribute('aria-pressed', 'false');
    expect(raiz().contraste).toBe('normal');
  });

  test('a escolha fica no aparelho para a próxima vez', async () => {
    abrirAjustes();

    await userEvent.click(screen.getByRole('button', { name: 'Letra: normal' }));
    await userEvent.click(screen.getByRole('button', { name: 'Contraste alto' }));

    expect(lerPreferencias()).toEqual({ letra: 'grande', contraste: 'alto' });
  });

  test('sem escolha guardada, segue o celular que já pede mais contraste', () => {
    vi.stubGlobal('matchMedia', (consulta: string) => ({
      matches: consulta === '(prefers-contrast: more)',
    }));

    expect(lerPreferencias()).toEqual({ letra: 'normal', contraste: 'alto' });
  });

  test('o que estiver estragado no aparelho vira a preferência de fábrica', () => {
    localStorage.setItem('escala-avulsos.leitura', '{"letra":"gigante","contraste":7}');

    expect(lerPreferencias()).toEqual({ letra: 'normal', contraste: 'normal' });

    localStorage.setItem('escala-avulsos.leitura', 'não é json');

    expect(lerPreferencias()).toEqual({ letra: 'normal', contraste: 'normal' });
  });
});
