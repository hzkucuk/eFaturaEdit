<!--
  ClaudeTerminal — Faz D "Terminal" sekmesi: HAM interaktif `claude` bir PTY'de.

  ⚠️ Bu, ana Claude Code panelinden AYRI ve DAHA ZAYIF bir güvenlik duruşudur:
  interaktif modda uygulamanın fail-closed hook+sandbox kilidi TUTMAZ (ölçüldü) —
  bu yüzden klasör dışına yazma GARANTİ ALTINDA DEĞİL. Tek koruma claude'un KENDİ
  interaktif izin promptlarıdır (kullanıcı terminalde görür ve onaylar). Bu gerçek
  büyük sarı bir bantla açıkça söylenir; sessiz-yanlış-garanti bırakmayız.
-->
<script lang="ts">
  import { onDestroy } from 'svelte';
  import { invoke } from '@tauri-apps/api/core';
  import { listen, type UnlistenFn } from '@tauri-apps/api/event';
  import '@xterm/xterm/css/xterm.css';
  import { claude, setClaudeRoot, refreshEngine } from '$lib/claude-session.svelte';
  import { settings } from '$lib/settings.svelte';
  import { pickFolder } from '$lib/batch';

  // Terminal sekmesi ilk açılışsa (Claude Code sekmesine uğramadan) motor durumunu tazele.
  $effect(() => {
    if (!claude.engine) void refreshEngine(settings.claudeUseSystemBinary);
  });

  let termEl: HTMLDivElement | null = $state(null);
  // xterm nesneleri any: türler dinamik import'tan geliyor, sabit tip gerekmiyor.
  let term: { write: (d: Uint8Array) => void; onData: (cb: (d: string) => void) => void; open: (el: HTMLElement) => void; dispose: () => void; cols: number; rows: number; loadAddon: (a: unknown) => void } | null = null;
  let fit: { fit: () => void } | null = null;
  let unlistenOut: UnlistenFn | null = null;
  let unlistenExit: UnlistenFn | null = null;
  let resizeObs: ResizeObserver | null = null;

  let started = $state(false);
  let bitti = $state(false);
  let hata = $state('');

  const engineReady = $derived(claude.engine?.ready === true);

  async function chooseFolder(): Promise<void> {
    const f = await pickFolder();
    if (f) setClaudeRoot(f);
  }

  async function startTerminal(): Promise<void> {
    if (!claude.root || started || !termEl) return;
    hata = '';
    bitti = false;
    // Dinamik import — xterm window/document ister, SSR'de yok.
    const [{ Terminal }, { FitAddon }] = await Promise.all([
      import('@xterm/xterm'),
      import('@xterm/addon-fit'),
    ]);
    term = new Terminal({
      fontSize: 13,
      fontFamily: 'ui-monospace, Menlo, Consolas, monospace',
      cursorBlink: true,
      theme: { background: '#1e1e1e', foreground: '#e0e0e0' },
    }) as unknown as typeof term;
    fit = new FitAddon() as unknown as typeof fit;
    term!.loadAddon(fit);
    term!.open(termEl);
    fit!.fit();

    unlistenOut = await listen<string>('claude-terminal-output', (e) => {
      // base64 → bayt → xterm (ANSI/UTF-8 ham akış).
      const bytes = Uint8Array.from(atob(e.payload), (c) => c.charCodeAt(0));
      term?.write(bytes);
    });
    unlistenExit = await listen('claude-terminal-exit', () => {
      bitti = true;
    });
    term!.onData((d: string) => void invoke('claude_terminal_write', { data: d }));

    try {
      await invoke('claude_terminal_start', {
        kok: claude.root,
        sistemIkili: settings.claudeUseSystemBinary,
        cols: term!.cols,
        rows: term!.rows,
      });
      started = true;
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
      return;
    }

    resizeObs = new ResizeObserver(() => {
      if (!fit || !term) return;
      fit.fit();
      void invoke('claude_terminal_resize', { cols: term.cols, rows: term.rows });
    });
    resizeObs.observe(termEl);
  }

  async function stopTerminal(): Promise<void> {
    resizeObs?.disconnect();
    resizeObs = null;
    unlistenOut?.();
    unlistenExit?.();
    unlistenOut = unlistenExit = null;
    await invoke('claude_terminal_kill').catch(() => {});
    term?.dispose();
    term = null;
    fit = null;
    started = false;
    bitti = false;
  }

  onDestroy(() => {
    resizeObs?.disconnect();
    unlistenOut?.();
    unlistenExit?.();
    void invoke('claude_terminal_kill').catch(() => {});
    term?.dispose();
  });
</script>

<div class="terminal-mode">
  <!-- DÜRÜST uyarı: bu mod ana panelden zayıf; garanti değil, gizleme. -->
  <div class="warn">
    <b>⚠️ Ham terminal — kendi güvenliğiyle.</b> Bu sekme gerçek <code>claude</code> arayüzünü
    çalıştırır. Ana paneldeki <b>klasör kilidi burada GEÇERLİ DEĞİL</b> — komutlar klasör dışına
    çıkabilir. Tek koruma <b>claude'un kendi onay promptlarıdır</b> (aşağıda görürsün). Güvenli,
    onay-kapılı deneyim için <b>Claude Code</b> sekmesini kullan.
  </div>

  <div class="term-bar">
    <button class="pick" onclick={chooseFolder} disabled={started}>📁 Klasör</button>
    {#if claude.root}
      <code class="root" title={claude.root}>{claude.root}</code>
    {/if}
    {#if !started}
      <button
        class="start"
        onclick={startTerminal}
        disabled={!claude.root || !engineReady}
        title={!engineReady ? 'Önce Claude Code sekmesinden motoru kur' : ''}
      >
        ▶ Terminali başlat
      </button>
    {:else}
      <button class="stop" onclick={stopTerminal}>■ Kapat</button>
    {/if}
  </div>

  {#if !engineReady}
    <p class="hint">Motor hazır değil — önce <b>Claude Code</b> sekmesinden kur.</p>
  {/if}
  {#if hata}
    <p class="err">{hata}</p>
  {/if}
  {#if bitti}
    <p class="hint">Terminal oturumu bitti. Yeniden başlatmak için “Kapat” → “Terminali başlat”.</p>
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
  }
  .term-bar {
    display: flex;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
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
  }
  .err {
    font-size: 12px;
    color: #b3261e;
    background: #fdecea;
    border-radius: 5px;
    padding: 6px 9px;
    margin: 0;
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
