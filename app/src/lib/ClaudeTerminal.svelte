<!--
  ClaudeTerminal — "Terminal" görünümü: HAM interaktif `claude` bir PTY'de.

  ⚠️ Bu, güvenli Panel görünümünden AYRI ve DAHA ZAYIF bir duruştur: interaktif modda
  uygulamanın fail-closed hook+sandbox kilidi TUTMAZ (ölçüldü) — klasör dışına yazma
  GARANTİ ALTINDA DEĞİL. Tek koruma claude'un KENDİ onay promptlarıdır. Bu gerçek büyük
  sarı bir bantla açıkça söylenir; sessiz-yanlış-garanti bırakmayız.

  <b>Bu bileşen yalnızca bir PENCEREDİR:</b> canlı oturum (xterm + PTY)
  `claude-terminal.svelte.ts` modülünde yaşar. Sekmeler arasında gezinmek oturumu
  ÖLDÜRMEZ — mount'ta ekran geri takılır, unmount'ta yalnızca sökülür.
-->
<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { claude, setClaudeRoot, refreshEngine } from '$lib/claude-session.svelte';
  import { settings } from '$lib/settings.svelte';
  import { pickFolder } from '$lib/batch';
  import {
    terminalState,
    attachTerminal,
    detachTerminal,
    startTerminal,
    stopTerminal,
    syncSize,
  } from '$lib/claude-terminal.svelte';

  let termEl: HTMLDivElement | null = $state(null);
  let resizeObs: ResizeObserver | null = null;

  const engineReady = $derived(claude.engine?.ready === true);

  // Terminal görünümü ilk açılışsa motor durumunu tazele (Panel'e uğramadan).
  $effect(() => {
    if (!claude.engine) void refreshEngine(settings.claudeUseSystemBinary);
  });

  onMount(() => {
    if (termEl) {
      void attachTerminal(termEl);
      resizeObs = new ResizeObserver(() => void syncSize());
      resizeObs.observe(termEl);
    }
  });

  onDestroy(() => {
    resizeObs?.disconnect();
    resizeObs = null;
    detachTerminal(); // ⚠️ ÖLDÜRME — oturum sekme değişiminde yaşamalı
  });

  async function chooseFolder(): Promise<void> {
    const f = await pickFolder();
    if (f) setClaudeRoot(f);
  }

  async function basla(): Promise<void> {
    if (!claude.root || !termEl) return;
    await attachTerminal(termEl);
    await startTerminal(claude.root, settings.claudeUseSystemBinary);
  }
</script>

<div class="terminal-mode">
  <!-- DÜRÜST uyarı: bu görünüm Panel'den zayıf; garanti değil, gizleme. -->
  <div class="warn">
    <b>⚠️ Ham terminal — kendi güvenliğiyle.</b> Bu görünüm gerçek <code>claude</code> arayüzünü
    çalıştırır. <b>Klasör kilidi burada GEÇERLİ DEĞİL</b> — komutlar klasör dışına çıkabilir. Tek
    koruma <b>claude'un kendi onay promptlarıdır</b> (aşağıda görürsün). Güvenli, onay-kapılı
    deneyim için <b>Panel</b> görünümünü kullan.
  </div>

  <div class="term-bar">
    <button class="pick" onclick={chooseFolder} disabled={terminalState.calisiyor}>📁 Klasör</button>
    {#if claude.root}
      <code class="root" title={claude.root}>{claude.root}</code>
    {/if}
    {#if terminalState.calisiyor}
      <span class="live" title="Oturum açık — sekme değiştirsen de sürer">● canlı</span>
      <button class="stop" onclick={stopTerminal}>■ Kapat</button>
    {:else}
      <button
        class="start"
        onclick={basla}
        disabled={!claude.root || !engineReady}
        title={!engineReady ? 'Önce Panel görünümünden motoru kur' : ''}
      >
        ▶ Terminali başlat
      </button>
    {/if}
  </div>

  {#if !engineReady}
    <p class="hint">Motor hazır değil — önce <b>Panel</b> görünümünden kur.</p>
  {/if}
  {#if terminalState.hata}
    <p class="err">{terminalState.hata}</p>
  {/if}
  {#if terminalState.bitti}
    <p class="hint">Oturum bitti. Yeniden başlatmak için “▶ Terminali başlat”.</p>
  {/if}

  <div class="term-host" bind:this={termEl}></div>
</div>

<style>
  .terminal-mode {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    gap: 6px;
  }
  .warn {
    background: #fff3cd;
    border: 1px solid #ffe08a;
    color: #7a5b00;
    border-radius: 6px;
    padding: 8px 10px;
    font-size: 12px;
    line-height: 1.45;
    flex-shrink: 0;
  }
  .term-bar {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
    flex-shrink: 0;
  }
  .pick,
  .start,
  .stop {
    padding: 4px 10px;
    border-radius: 5px;
    border: 1px solid #ccc;
    background: #f7f7f7;
    cursor: pointer;
    font-size: 12px;
    white-space: nowrap;
  }
  .start {
    background: #1a73e8;
    color: #fff;
    border-color: #1a73e8;
  }
  .start:disabled {
    background: #b0c4de;
    cursor: default;
  }
  .stop {
    background: #fdecf4;
    color: #b3268a;
    border-color: #e0a;
  }
  .live {
    font-size: 11px;
    color: #1a7f37;
    white-space: nowrap;
  }
  .root {
    flex: 1;
    min-width: 60px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 11px;
    color: #555;
  }
  .hint {
    font-size: 12px;
    color: #666;
    margin: 0;
    flex-shrink: 0;
  }
  .err {
    font-size: 12px;
    color: #b3261e;
    background: #fdecea;
    border-radius: 5px;
    padding: 6px 9px;
    margin: 0;
    flex-shrink: 0;
  }
  .term-host {
    flex: 1;
    min-height: 0;
    background: #1e1e1e;
    border-radius: 6px;
    padding: 6px;
    overflow: hidden;
  }
  :global(html.dark) .warn {
    background: #3a3000;
    border-color: #5a4a10;
    color: #e8cf7a;
  }
  :global(html.dark) .pick,
  :global(html.dark) .stop {
    background: #333;
    color: #ddd;
    border-color: #555;
  }
</style>
