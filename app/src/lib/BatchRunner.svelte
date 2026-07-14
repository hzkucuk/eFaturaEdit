<!--
  BatchRunner — şablonu bir klasördeki TÜM faturalara karşı çalıştırır.

  "Bir şeyi düzelttim, başka bir şeyi bozdum mu?" sorusunu tahminle değil
  ÖLÇÜMLE yanıtlar: her çıktının sha256'sı anlık görüntüye alınır, şablon
  değişince hangi faturaların çıktısının DEĞİŞTİĞİ satır satır görünür.
-->
<script lang="ts">
  import {
    pickFolder,
    runBatch,
    saveBaseline,
    type BatchReport,
  } from '$lib/batch';
  import { m, f } from '$lib/i18n.svelte';

  interface Props {
    xsltText: string;
    onclose: () => void;
    /** Bir faturayı editöre yükle (satıra çift tıklayınca). */
    onopen: (xmlPath: string) => void;
  }

  let { xsltText, onclose, onopen }: Props = $props();

  let folder = $state('');
  let report = $state<BatchReport | null>(null);
  let running = $state(false);
  let progress = $state({ done: 0, total: 0, name: '' });
  let error = $state('');
  let saved = $state('');

  async function choose() {
    error = '';
    const picked = await pickFolder();
    if (!picked) return;
    folder = picked;
    report = null;
    saved = '';
    await run();
  }

  async function run() {
    if (!folder || running) return;
    running = true;
    error = '';
    saved = '';
    report = null;
    try {
      report = await runBatch(folder, xsltText, (done, total, name) => {
        progress = { done, total, name };
      });
      if (report.rows.length === 0) error = m.batch.noXml;
    } catch (err) {
      error = (err as Error).message ?? String(err);
    } finally {
      running = false;
    }
  }

  async function snapshot() {
    if (!report) return;
    const n = await saveBaseline(report);
    saved = f(m.batch.snapshotSaved, { n });
    // Kaydettikten sonra satırlar artık "aynı" sayılır.
    report = { ...report, rows: report.rows.map((r) => ({ ...r, change: r.ok ? 'same' : 'none' })), changedCount: 0, hasBaseline: true };
  }
</script>

<div
  class="modal-backdrop"
  role="button"
  tabindex="-1"
  onclick={onclose}
  onkeydown={(e) => e.key === 'Escape' && onclose()}
>
  <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
  <div class="modal" onclick={(e) => e.stopPropagation()}>
    <header>
      <h2>🧪 {m.batch.title}</h2>
      <button class="x" onclick={onclose} aria-label={m.common.close}>×</button>
    </header>

    <p class="why">{m.batch.why}</p>

    <div class="bar">
      <button class="primary" onclick={choose} disabled={running}>📁 {m.batch.pickFolder}</button>
      {#if folder}
        <span class="folder" title={folder}>{folder}</span>
        <button onclick={run} disabled={running}>▶ {m.batch.rerun}</button>
      {/if}
    </div>

    {#if running}
      <div class="progress">
        <div class="pbar"><div class="pfill" style="width:{progress.total ? (progress.done / progress.total) * 100 : 0}%"></div></div>
        <span>{f(m.batch.progress, { done: progress.done, total: progress.total })} {progress.name}</span>
      </div>
    {/if}

    {#if error}
      <div class="err">{error}</div>
    {/if}

    {#if report && report.rows.length > 0}
      <div class="summary">
        <span class="ok">✓ {f(m.batch.passed, { n: report.okCount })}</span>
        {#if report.failCount > 0}
          <span class="fail">✗ {f(m.batch.failed, { n: report.failCount })}</span>
        {/if}
        {#if report.hasBaseline}
          <span class="changed" class:zero={report.changedCount === 0}>
            {report.changedCount === 0 ? m.batch.allSame : f(m.batch.changed, { n: report.changedCount })}
          </span>
        {:else}
          <span class="dim">{m.batch.noBaseline}</span>
        {/if}
        <span class="dim">{report.totalMs} ms</span>
        <button class="snap" onclick={snapshot}>📸 {m.batch.snapshot}</button>
      </div>
      {#if saved}<p class="saved">{saved}</p>{/if}

      <div class="rows">
        <table>
          <thead>
            <tr>
              <th></th>
              <th>{m.batch.colFile}</th>
              <th>{m.batch.colResult}</th>
              <th class="num">ms</th>
              <th class="num">{m.batch.colSize}</th>
              <th>sha256</th>
            </tr>
          </thead>
          <tbody>
            {#each report.rows as row (row.path)}
              <tr class:bad={!row.ok} ondblclick={() => onopen(row.path)} title={m.batch.openHint}>
                <td class="mark">{row.ok ? '✓' : '✗'}</td>
                <td class="file">{row.name}</td>
                <td class="res">
                  {#if !row.ok}
                    <span class="msg">{row.error}</span>
                  {:else if row.change === 'changed'}
                    <span class="tag chg">{m.batch.tagChanged}</span>
                  {:else if row.change === 'new'}
                    <span class="tag new">{m.batch.tagNew}</span>
                  {:else if row.change === 'same'}
                    <span class="tag same">{m.batch.tagSame}</span>
                  {/if}
                </td>
                <td class="num dim">{row.ms}</td>
                <td class="num dim">{row.ok ? (row.bytes / 1024).toFixed(1) + ' KB' : '—'}</td>
                <td class="hash dim">{row.hash.slice(0, 12)}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>
</div>

<style>
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9000;
    border: none;
    padding: 0;
  }
  .modal {
    background: #fff;
    border-radius: 8px;
    width: min(920px, 92vw);
    max-height: 86vh;
    display: flex;
    flex-direction: column;
    padding: 16px 18px;
    box-shadow: 0 12px 48px rgba(0, 0, 0, 0.3);
  }
  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  h2 {
    margin: 0;
    font-size: 17px;
  }
  .x {
    background: none;
    border: none;
    font-size: 22px;
    color: #6b7280;
    cursor: pointer;
  }
  .why {
    margin: 6px 0 12px;
    font-size: 12px;
    color: #6b7280;
    line-height: 1.5;
  }
  .bar {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 10px;
  }
  .folder {
    flex: 1;
    min-width: 0;
    font-size: 12px;
    color: #6b7280;
    font-family: ui-monospace, Menlo, monospace;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    direction: rtl;
    text-align: left;
  }
  button {
    padding: 6px 12px;
    font-size: 13px;
    border: 1px solid #cbd0d6;
    border-radius: 5px;
    background: #fff;
    cursor: pointer;
  }
  button:hover:not(:disabled) {
    background: #eef4ff;
    border-color: #0a5cff;
  }
  button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  button.primary {
    background: #0a5cff;
    color: #fff;
    border-color: #0a5cff;
    font-weight: 600;
  }
  .snap {
    margin-left: auto;
  }

  .progress {
    margin-bottom: 8px;
    font-size: 12px;
    color: #6b7280;
  }
  .pbar {
    height: 4px;
    background: #e5e7eb;
    border-radius: 2px;
    overflow: hidden;
    margin-bottom: 4px;
  }
  .pfill {
    height: 100%;
    background: #0a5cff;
    transition: width 0.15s;
  }

  .err {
    background: #fee2e2;
    color: #b91c1c;
    padding: 8px 10px;
    border-radius: 5px;
    font-size: 12px;
    margin-bottom: 8px;
  }
  .summary {
    display: flex;
    align-items: center;
    gap: 12px;
    font-size: 13px;
    padding: 8px 0;
    border-top: 1px solid #e5e7eb;
  }
  .ok {
    color: #0f7b3f;
    font-weight: 600;
  }
  .fail {
    color: #b91c1c;
    font-weight: 700;
  }
  .changed {
    color: #92400e;
    background: #fef3c7;
    padding: 2px 8px;
    border-radius: 4px;
    font-weight: 600;
  }
  .changed.zero {
    color: #0f7b3f;
    background: #dcfce7;
  }
  .dim {
    color: #8b8f96;
  }
  .saved {
    margin: 4px 0;
    font-size: 12px;
    color: #0f7b3f;
  }

  .rows {
    flex: 1;
    overflow: auto;
    border: 1px solid #e5e7eb;
    border-radius: 5px;
  }
  table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;
  }
  th {
    position: sticky;
    top: 0;
    background: #f5f6f8;
    text-align: left;
    padding: 6px 8px;
    font-weight: 600;
    color: #4b5563;
    border-bottom: 1px solid #e5e7eb;
  }
  td {
    padding: 5px 8px;
    border-bottom: 1px solid #f0f1f3;
    vertical-align: top;
  }
  tr:hover td {
    background: #f8fafc;
  }
  tr.bad td {
    background: #fff5f5;
  }
  .mark {
    width: 1.6em;
  }
  tr.bad .mark {
    color: #b91c1c;
    font-weight: 700;
  }
  tr:not(.bad) .mark {
    color: #0f7b3f;
  }
  .file {
    font-family: ui-monospace, Menlo, monospace;
    white-space: nowrap;
  }
  .num {
    text-align: right;
    white-space: nowrap;
  }
  .hash {
    font-family: ui-monospace, Menlo, monospace;
  }
  .msg {
    color: #b91c1c;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    word-break: break-word;
  }
  .tag {
    padding: 1px 7px;
    border-radius: 3px;
    font-weight: 600;
    font-size: 11px;
  }
  .tag.chg {
    background: #fef3c7;
    color: #92400e;
  }
  .tag.new {
    background: #dbeafe;
    color: #1e40af;
  }
  .tag.same {
    background: #f1f5f9;
    color: #64748b;
  }

  /* Koyu tema */
  :global(.app.dark) .modal {
    background: #2d2d30;
    color: #e6e6e6;
  }
  :global(.app.dark) button {
    background: #3a3a3d;
    border-color: #555;
    color: #e6e6e6;
  }
  :global(.app.dark) button.primary {
    background: #0a5cff;
    border-color: #0a5cff;
  }
  :global(.app.dark) th {
    background: #252526;
    color: #b6bac0;
    border-bottom-color: #3f3f46;
  }
  :global(.app.dark) td {
    border-bottom-color: #3f3f46;
  }
  :global(.app.dark) tr:hover td {
    background: #333336;
  }
  :global(.app.dark) tr.bad td {
    background: #3d2626;
  }
  :global(.app.dark) .rows {
    border-color: #3f3f46;
  }
</style>
