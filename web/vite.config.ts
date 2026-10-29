import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

/*
 * Em desenvolvimento, o Vite repassa /api para a API: o celular fala com uma
 * origem só, e a API não precisa de CORS. A porta da API vem do perfil http
 * dela (launchSettings.json); outra, pela variável API_ESCALA.
 */
const api = process.env.API_ESCALA ?? 'http://localhost:5300';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5400,
    strictPort: true,
    proxy: { '/api': api },
  },
  preview: {
    port: 5400,
    strictPort: true,
    proxy: { '/api': api },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/testes/preparacao.ts'],
    restoreMocks: true,
    unstubGlobals: true,
    // Num fuso que não é o do porto, de propósito: o aplicativo mostra a
    // hora do porto, e não a do aparelho — um trabalhador de férias em
    // Manaus vê a escala no horário de Santos.
    env: { TZ: 'America/Manaus' },
    // O cache de módulos em disco guarda a expansão do import.meta.glob pelo
    // conteúdo do arquivo que o usa: uma área nova da aplicação não entraria
    // nos testes até alguém apagar o cache. Mais lento, e sempre certo.
    fsModuleCache: false,
  },
});
