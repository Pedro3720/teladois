import { defineConfig, devices } from '@playwright/test';

const PORT = 5174;

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  reporter: [['list']],
  use: {
    baseURL: `http://127.0.0.1:${PORT}`,
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
  },
  // WebKit é o mesmo motor do Safari do iPhone. Dois tamanhos: o menor e o maior iPhone suportados.
  projects: [
    { name: 'iPhone 15 (paisagem)', use: { ...devices['iPhone 15 landscape'] } },
    { name: 'iPhone 17 Pro Max (paisagem)', use: { ...devices['iPhone 17 Pro Max landscape'] } },
  ],
  webServer: {
    command: `node serve.mjs ${PORT}`,
    url: `http://127.0.0.1:${PORT}`,
    reuseExistingServer: true,
  },
});
