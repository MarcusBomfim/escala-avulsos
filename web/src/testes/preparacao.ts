import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { servidor } from './servidor';

// O jsdom não tem matchMedia, que todo navegador tem: aqui, um celular sem preferência nenhuma.
Object.defineProperty(globalThis, 'matchMedia', {
  value: (consulta: string) => ({ matches: false, media: consulta }),
});

// Pedido que nenhum teste previu é erro: a tela pediu o que não devia.
beforeAll(() => {
  servidor.listen({ onUnhandledFrame: 'error' });
});

afterEach(() => {
  cleanup();
  servidor.resetHandlers();
  localStorage.clear();
  sessionStorage.clear();
  delete document.documentElement.dataset.letra;
  delete document.documentElement.dataset.contraste;
});

afterAll(() => {
  servidor.close();
});
