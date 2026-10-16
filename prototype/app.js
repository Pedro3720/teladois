// TelaDois — Modo Descanso (protótipo)
// Parâmetros de URL para testes: ?dim=5 (segundos até escurecer), ?temp=23 (temperatura fixa, sem localização),
// ?intro=0 (pula as instruções).
(() => {
  'use strict';

  const params = new URLSearchParams(location.search);
  const DIM_SECONDS = Math.max(1, Number(params.get('dim')) || 60);
  const FIXED_TEMP = params.has('temp') ? Number(params.get('temp')) : null;
  const SKIP_INTRO = params.get('intro') === '0';
  const WEATHER_REFRESH_MS = 30 * 60 * 1000;

  const $ = (id) => document.querySelector(`[data-testid="${id}"]`);
  const root = document.documentElement;

  // ---------- Relógio e data ----------
  const weekdayFmt = new Intl.DateTimeFormat('pt-BR', { weekday: 'short' });
  const pad = (n) => String(n).padStart(2, '0');

  function renderClock(now = new Date()) {
    $('clock-hh').textContent = pad(now.getHours());
    $('clock-mm').textContent = pad(now.getMinutes());
    $('weekday').textContent = weekdayFmt.format(now).toUpperCase().replace(/\.?$/, '.');
    $('day').textContent = String(now.getDate());
  }

  function startClock() {
    renderClock();
    const msToNextSecond = 1000 - (Date.now() % 1000);
    setTimeout(() => { renderClock(); setInterval(renderClock, 1000); }, msToNextSecond);
  }

  // ---------- Bateria (simulada no protótipo) ----------
  function renderBattery(level) {
    const pct = Math.round(level * 100);
    $('battery-level').style.strokeDasharray = `${pct} 100`;
    $('battery-pct').textContent = String(pct);
  }

  // ---------- Clima (Open-Meteo, sem chave) ----------
  function showTemp(celsius) {
    const el = $('temp');
    el.textContent = `${Math.round(celsius)}°C`;
    el.hidden = false;
  }

  async function fetchTemp(lat, lon) {
    const url = `https://api.open-meteo.com/v1/forecast?latitude=${lat.toFixed(3)}&longitude=${lon.toFixed(3)}&current=temperature_2m`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Open-Meteo respondeu ${res.status}`);
    const data = await res.json();
    return data.current.temperature_2m;
  }

  function startWeather() {
    if (FIXED_TEMP !== null && !Number.isNaN(FIXED_TEMP)) { showTemp(FIXED_TEMP); return; }
    if (!('geolocation' in navigator)) return;
    const update = () => navigator.geolocation.getCurrentPosition(
      async ({ coords }) => {
        try { showTemp(await fetchTemp(coords.latitude, coords.longitude)); }
        catch { /* sem rede: a temperatura continua escondida */ }
      },
      () => { /* localização negada: a temperatura fica escondida */ },
      { maximumAge: WEATHER_REFRESH_MS, timeout: 15000 }
    );
    update();
    setInterval(update, WEATHER_REFRESH_MS);
  }

  // ---------- Música (simulada; no app virá do Spotify) ----------
  const TRACKS = [
    { title: 'Maré Alta', artist: 'Trio Litoral', length: 214, a: '#6b2f1e', b: '#2a1410', mark: 'MA' },
    { title: 'Avenida Paulista, 4h', artist: 'Neblina', length: 187, a: '#1f3f6b', b: '#0d1a2e', mark: 'AP' },
    { title: 'Café Coado', artist: 'Lúcia Dantas', length: 243, a: '#2f5a3a', b: '#10241a', mark: 'CC' },
  ];
  const music = { index: 0, elapsed: 97, playing: true };

  const fmtTime = (s) => `${Math.floor(s / 60)}:${pad(Math.floor(s % 60))}`;

  function renderMusic() {
    const t = TRACKS[music.index];
    root.style.setProperty('--cover-a', t.a);
    root.style.setProperty('--cover-b', t.b);
    document.querySelector('.cover-mark').textContent = t.mark;
    $('track-title').textContent = t.title;
    $('track-artist').textContent = t.artist;
    $('mini-title').textContent = t.title;
    $('mini-artist').textContent = t.artist;
    $('progress-fill').style.width = `${(music.elapsed / t.length) * 100}%`;
    $('elapsed').textContent = fmtTime(music.elapsed);
    $('remaining').textContent = `-${fmtTime(t.length - music.elapsed)}`;
    document.body.classList.toggle('is-paused', !music.playing);
    $('btn-toggle').setAttribute('aria-label', music.playing ? 'Pausar' : 'Tocar');
    $('mini-player').hidden = !music.playing;
  }

  function changeTrack(step) {
    music.index = (music.index + step + TRACKS.length) % TRACKS.length;
    music.elapsed = 0;
    renderMusic();
  }

  function tickMusic() {
    if (!music.playing) return;
    music.elapsed += 1;
    if (music.elapsed >= TRACKS[music.index].length) { changeTrack(1); return; }
    renderMusic();
  }

  // ---------- Páginas ----------
  const pages = $('pages');
  function goToPage(i) { pages.scrollTo({ left: i * pages.clientWidth, behavior: 'smooth' }); }
  pages.addEventListener('scroll', () => {
    const i = Math.round(pages.scrollLeft / Math.max(1, pages.clientWidth));
    document.querySelectorAll('.dot').forEach((d) => d.classList.toggle('is-active', Number(d.dataset.dot) === i));
  }, { passive: true });

  const actions = {
    'go-music': () => goToPage(1),
    prev: () => changeTrack(-1),
    next: () => changeTrack(1),
    toggle: () => { music.playing = !music.playing; renderMusic(); },
  };
  document.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-action]');
    if (btn && actions[btn.dataset.action]) actions[btn.dataset.action]();
  });

  // ---------- Escurecer após inatividade ----------
  let dimTimer = null;
  const isDimmed = () => document.body.classList.contains('is-dimmed');

  function armDimTimer() {
    clearTimeout(dimTimer);
    dimTimer = setTimeout(() => document.body.classList.add('is-dimmed'), DIM_SECONDS * 1000);
  }

  function wake() {
    document.body.classList.add('is-waking');
    document.body.classList.remove('is-dimmed');
    requestAnimationFrame(() => document.body.classList.remove('is-waking'));
    armDimTimer();
  }

  // Toque com a tela acordada: só reinicia a contagem.
  document.addEventListener('pointerdown', () => { if (!isDimmed()) armDimTimer(); }, true);
  // Toque com a tela preta: o "click" é capturado pela camada preta, então só acorda — não aciona botões.
  $('dim').addEventListener('click', (e) => { e.stopPropagation(); wake(); });

  // ---------- Manter a tela ligada (Safari 16.4+) ----------
  let wakeLock = null;
  async function keepScreenOn() {
    try { wakeLock = await navigator.wakeLock?.request('screen'); } catch { wakeLock = null; }
  }
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') { keepScreenOn(); wake(); }
  });

  // ---------- Início ----------
  function start() {
    $('intro').hidden = true;
    keepScreenOn();
    startWeather();
    armDimTimer();
  }

  $('intro-dim-seconds').textContent = String(DIM_SECONDS);
  $('intro-start').addEventListener('click', start);

  startClock();
  renderBattery(0.8);
  renderMusic();
  setInterval(tickMusic, 1000);
  if (SKIP_INTRO) start();

  // Ponto de inspeção para os testes automatizados.
  window.__rest = { music, dimSeconds: DIM_SECONDS, isDimmed, renderClock };
})();
