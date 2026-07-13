<!--
  XPathConsole — XML veri editörünün altında açılan XPath test çekmecesi.

  Amaç: "bu alan neden boş geliyor?" sorusunu şablonu kurcalamadan yanıtlamak.
  İfade doğrudan yüklü veriye karşı çalışır; kaç düğüm eşleşti ve değerleri ne,
  anında görünür.

  Motor rozeti KASITLI: Saxon yoksa tarayıcının XPath 1.0'ına düşülür ve bu
  görünür şekilde söylenir — sessiz geri düşüş bu projede yasak (2.0 fonksiyonları
  1.0'da hata vermeden farklı davranabilir).
-->
<script lang="ts">
  import { evaluateXPath, XPathError, MAX_ITEMS, type XPathOutcome } from '$lib/xpath';
  import { XsltError } from '$lib/xslt';
  import { m, f } from '$lib/i18n.svelte';

  interface Props {
    xmlText: string;
    onclose: () => void;
  }

  let { xmlText, onclose }: Props = $props();

  let expr = $state('');
  let outcome = $state<XPathOutcome | null>(null);
  let error = $state('');
  let running = $state(false);
  let inputEl = $state<HTMLInputElement>();

  /** Oturum içi geçmiş — ↑/↓ ile gezilir. */
  let history = $state<string[]>([]);
  let historyIndex = $state(-1);

  async function run() {
    const trimmed = expr.trim();
    if (!trimmed || running) return;

    running = true;
    error = '';
    try {
      outcome = await evaluateXPath(xmlText, trimmed);
      if (history[0] !== trimmed) history = [trimmed, ...history].slice(0, 50);
      historyIndex = -1;
    } catch (err) {
      outcome = null;
      // Gerçek sebebi göster — "bilinmeyen hata" demek teşhisi kör eder.
      if (err instanceof XsltError) error = f(m.xpath.badXml, { msg: err.message });
      else if (err instanceof XPathError) error = err.message;
      else error = (err as Error).message ?? String(err);
    } finally {
      running = false;
    }
  }

  function onKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault();
      run();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      onclose();
    } else if (e.key === 'ArrowUp' && history.length) {
      e.preventDefault();
      historyIndex = Math.min(historyIndex + 1, history.length - 1);
      expr = history[historyIndex];
    } else if (e.key === 'ArrowDown' && historyIndex >= 0) {
      e.preventDefault();
      historyIndex -= 1;
      expr = historyIndex < 0 ? '' : history[historyIndex];
    }
  }

  $effect(() => {
    inputEl?.focus();
  });
</script>

<div class="xp">
  <div class="xp-bar">
    <span class="xp-label">XPath</span>
    <!-- svelte-ignore a11y_autofocus -->
    <input
      bind:this={inputEl}
      bind:value={expr}
      onkeydown={onKeydown}
      placeholder={m.xpath.placeholder}
      spellcheck="false"
      autocomplete="off"
    />
    <button class="xp-run" onclick={run} disabled={running || !expr.trim()}>
      {running ? m.xpath.running : m.xpath.run}
    </button>
    <button class="xp-close" onclick={onclose} title={m.common.close} aria-label={m.common.close}>×</button>
  </div>

  <div class="xp-out">
    {#if error}
      <div class="xp-err">{error}</div>
    {:else if outcome}
      <div class="xp-meta">
        <span class="xp-count" class:zero={outcome.count === 0}>
          {f(m.xpath.matches, { n: outcome.count })}
        </span>
        <span class="xp-dim">{outcome.ms} ms</span>
        {#if outcome.engine === 'browser'}
          <span class="xp-warn" title={m.xpath.browserEngineTitle}>{m.xpath.browserEngine}</span>
        {/if}
        {#if outcome.truncated}
          <span class="xp-dim">{f(m.xpath.truncated, { n: MAX_ITEMS })}</span>
        {/if}
      </div>

      {#if outcome.count === 0}
        <p class="xp-hint">{m.xpath.noMatch}</p>
      {:else}
        <table class="xp-table">
          <tbody>
            {#each outcome.items as item, i (i)}
              <tr>
                <td class="xp-i">{i + 1}</td>
                <td class="xp-name">{item.name || item.kind}</td>
                <td class="xp-val">{item.value}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      {/if}
    {:else}
      <p class="xp-hint">{m.xpath.hint}</p>
    {/if}
  </div>
</div>

<style>
  .xp {
    display: flex;
    flex-direction: column;
    border-top: 2px solid #0a5cff;
    background: #f5f6f8;
    max-height: 260px;
    min-height: 120px;
  }
  .xp-bar {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 6px 8px;
    border-bottom: 1px solid #cbd0d6;
  }
  .xp-label {
    font-size: 11px;
    font-weight: 700;
    color: #0a5cff;
    letter-spacing: 0.04em;
  }
  .xp-bar input {
    flex: 1;
    min-width: 0;
    padding: 5px 10px;
    font-size: 13px;
    font-family: ui-monospace, Menlo, monospace;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    background: #fff;
    color: #1a1a1a;
  }
  .xp-bar input:focus {
    outline: none;
    border-color: #0a5cff;
    box-shadow: 0 0 0 2px rgba(10, 92, 255, 0.15);
  }
  .xp-run {
    padding: 5px 12px;
    font-size: 12px;
    font-weight: 600;
    background: #0a5cff;
    color: #fff;
    border: 1px solid #0a5cff;
    border-radius: 4px;
    cursor: pointer;
  }
  .xp-run:disabled {
    background: #c3c6cb;
    border-color: #c3c6cb;
    cursor: not-allowed;
  }
  .xp-close {
    padding: 2px 8px;
    font-size: 16px;
    background: transparent;
    border: none;
    color: #6b7280;
    cursor: pointer;
  }
  .xp-close:hover {
    background: #fee2e2;
    color: #b91c1c;
    border-radius: 4px;
  }

  .xp-out {
    flex: 1;
    overflow: auto;
    padding: 6px 8px;
  }
  .xp-meta {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 4px;
    font-size: 12px;
  }
  .xp-count {
    font-weight: 700;
    color: #0f7b3f;
  }
  .xp-count.zero {
    color: #b91c1c;
  }
  .xp-dim {
    color: #8b8f96;
  }
  .xp-warn {
    color: #92400e;
    background: #fef3c7;
    border-radius: 3px;
    padding: 1px 6px;
    font-weight: 600;
    cursor: help;
  }
  .xp-err {
    color: #b91c1c;
    background: #fee2e2;
    border-radius: 4px;
    padding: 6px 8px;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    white-space: pre-wrap;
  }
  .xp-hint {
    color: #8b8f96;
    font-size: 12px;
    margin: 4px 0;
  }

  .xp-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;
    font-family: ui-monospace, Menlo, monospace;
  }
  .xp-table td {
    padding: 3px 6px;
    border-bottom: 1px solid #e5e7eb;
    vertical-align: top;
  }
  .xp-i {
    color: #b6bac0;
    width: 2.5em;
    text-align: right;
  }
  .xp-name {
    color: #0a5cff;
    white-space: nowrap;
  }
  .xp-val {
    color: #1a1a1a;
    word-break: break-word;
  }

  /* Koyu tema */
  :global(.app.dark) .xp {
    background: #252526;
    border-top-color: #0a5cff;
  }
  :global(.app.dark) .xp-bar {
    border-bottom-color: #3f3f46;
  }
  :global(.app.dark) .xp-bar input {
    background: #1e1e1e;
    border-color: #3f3f46;
    color: #e6e6e6;
  }
  :global(.app.dark) .xp-table td {
    border-bottom-color: #3f3f46;
  }
  :global(.app.dark) .xp-val {
    color: #e6e6e6;
  }
  :global(.app.dark) .xp-err {
    background: #4c1d1d;
    color: #fca5a5;
  }
</style>
