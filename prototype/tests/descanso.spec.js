import { test, expect } from '@playwright/test';

// Quarta-feira, 14/10/2026, 10:51 em São Paulo — mesma hora do print de referência do StandBy.
const START = new Date('2026-10-14T10:51:30-03:00');

async function open(page, query = 'intro=0&temp=23') {
  // Relógio falso parado em START: o tempo só anda com clock.runFor, então os testes não dependem da velocidade da máquina.
  await page.clock.install({ time: new Date(START.getTime() - 60_000) });
  await page.clock.pauseAt(START);
  await page.goto(`/?${query}`);
  await page.evaluate(() => document.fonts.ready);
}

const box = (page, id) => page.getByTestId(id).boundingBox();
const isDimmed = (page) => page.evaluate(() => document.body.classList.contains('is-dimmed'));

async function showMusicPage(page) {
  await page.getByTestId('pages').evaluate((el) => { el.style.scrollBehavior = 'auto'; el.scrollLeft = el.clientWidth; });
}

test('relógio mostra hora, dia da semana e temperatura', async ({ page }) => {
  await open(page);
  await expect(page.getByTestId('clock-hh')).toHaveText('10');
  await expect(page.getByTestId('clock-mm')).toHaveText('51');
  await expect(page.getByTestId('weekday')).toHaveText('QUA.');
  await expect(page.getByTestId('day')).toHaveText('14');
  await expect(page.getByTestId('temp')).toHaveText('23°C');

  await page.clock.runFor(31_000);
  await expect(page.getByTestId('clock-mm')).toHaveText('52');
});

test('relógio e coluna da direita cabem na tela sem se sobrepor', async ({ page }) => {
  await open(page);
  const vp = page.viewportSize();
  const clock = await box(page, 'clock');
  const battery = await box(page, 'battery');
  const date = await box(page, 'weekday');

  for (const b of [clock, battery, date]) {
    expect(b.x).toBeGreaterThanOrEqual(0);
    expect(b.y).toBeGreaterThanOrEqual(0);
    expect(b.x + b.width).toBeLessThanOrEqual(vp.width);
    expect(b.y + b.height).toBeLessThanOrEqual(vp.height);
  }
  expect(clock.x + clock.width).toBeLessThan(date.x);
  // Dígitos ocupam boa parte da altura, como no StandBy.
  expect(clock.height).toBeGreaterThan(vp.height * 0.6);
});

test('anel da bateria fica longe do canto arredondado da tela', async ({ page }) => {
  // Os cantos do iPhone têm raio de ~55 pt; elementos colados no canto são cortados.
  // O WebKit de teste não simula a área segura, então exigimos a folga mínima mesmo sem ela.
  await open(page);
  const vp = page.viewportSize();
  const battery = await box(page, 'battery');
  expect(vp.width - (battery.x + battery.width)).toBeGreaterThanOrEqual(44);
  expect(battery.y).toBeGreaterThanOrEqual(24);
});

test('sem localização a temperatura fica escondida', async ({ page }) => {
  await open(page, 'intro=0');
  await expect(page.getByTestId('temp')).toBeHidden();
});

test('mini player aparece só enquanto a música toca e leva à página de música', async ({ page }) => {
  await open(page);
  await expect(page.getByTestId('mini-title')).toHaveText('Maré Alta');
  await page.getByTestId('mini-player').click();
  await expect.poll(() => page.getByTestId('pages').evaluate((el) => el.scrollLeft)).toBeGreaterThan(0);

  await page.getByTestId('btn-toggle').click();
  await expect(page.getByTestId('mini-player')).toBeHidden();
});

test('página de música avança o tempo e troca de faixa', async ({ page }) => {
  await open(page);
  await showMusicPage(page);
  await expect(page.getByTestId('elapsed')).toHaveText('1:37');
  await page.clock.runFor(3_000);
  await expect(page.getByTestId('elapsed')).toHaveText('1:40');

  await page.getByTestId('btn-next').click();
  await expect(page.getByTestId('track-title')).toHaveText('Avenida Paulista, 4h');
  await expect(page.getByTestId('elapsed')).toHaveText('0:00');
  await page.getByTestId('btn-prev').click();
  await expect(page.getByTestId('track-title')).toHaveText('Maré Alta');
});

test('escurece depois de 60 s sem toque', async ({ page }) => {
  await open(page);
  await page.clock.runFor(59_000);
  expect(await isDimmed(page)).toBe(false);
  await page.clock.runFor(2_000);
  expect(await isDimmed(page)).toBe(true);
});

test('um toque com a tela acordada reinicia a contagem', async ({ page }) => {
  await open(page);
  await page.clock.runFor(50_000);
  await page.mouse.click(10, 10);
  await page.clock.runFor(30_000);
  expect(await isDimmed(page)).toBe(false);
  await page.clock.runFor(31_000);
  expect(await isDimmed(page)).toBe(true);
});

test('toque na tela preta só acorda — não aperta o botão embaixo', async ({ page }) => {
  await open(page);
  await showMusicPage(page);
  const toggle = await box(page, 'btn-toggle');
  await page.clock.runFor(61_000);
  expect(await isDimmed(page)).toBe(true);

  // Toque de verdade (touchstart/touchend + click sintetizado), como no iPhone.
  await page.touchscreen.tap(toggle.x + toggle.width / 2, toggle.y + toggle.height / 2);
  expect(await isDimmed(page)).toBe(false);
  expect(await page.evaluate(() => window.__rest.music.playing)).toBe(true);
});

test('instruções mostram o tempo configurado e somem ao tocar em Começar', async ({ page }) => {
  await open(page, 'dim=5&temp=23');
  await expect(page.getByTestId('intro')).toBeVisible();
  await expect(page.getByTestId('intro-dim-seconds')).toHaveText('5');
  await page.getByTestId('intro-start').click();
  await expect(page.getByTestId('intro')).toBeHidden();
  await page.clock.runFor(6_000);
  expect(await isDimmed(page)).toBe(true);
});

test('em retrato pede para girar o iPhone', async ({ page }) => {
  const vp = page.viewportSize();
  await page.setViewportSize({ width: vp.height, height: vp.width });
  await open(page);
  await expect(page.getByText('Vire o iPhone na horizontal')).toBeVisible();
  await expect(page.getByTestId('clock')).toBeHidden();
});
