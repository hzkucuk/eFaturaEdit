<script lang="ts">
  /**
   * Güncelleme bildirimi. Yeni sürüm bulunduğunda sürüm notlarıyla birlikte
   * çıkar; indirme/kurma yalnızca kullanıcı onaylarsa başlar.
   */
  import { updater, downloadAndInstall, dismissUpdate } from '$lib/updater.svelte';
  import manifest from '$lib/data/manifest.json';

  const open = $derived(
    (updater.stage === 'available' && !updater.dismissed) ||
      updater.stage === 'downloading' ||
      updater.stage === 'installing'
  );

  const pct = $derived(
    updater.total > 0 ? Math.min(100, Math.round((updater.downloaded / updater.total) * 100)) : 0
  );

  const mb = (n: number) => (n / 1024 / 1024).toFixed(1);
  const busy = $derived(updater.stage === 'downloading' || updater.stage === 'installing');
</script>

{#if open}
  <div class="u-backdrop">
    <div class="u-modal" role="dialog" aria-modal="true" aria-labelledby="u-title">
      <h2 id="u-title">
        {#if busy}⬇️ Güncelleme kuruluyor{:else}🎉 Yeni sürüm hazır{/if}
      </h2>

      <p class="u-ver">
        <span class="u-old">v{manifest.version}</span>
        <span class="u-arrow">→</span>
        <strong class="u-new">v{updater.version}</strong>
      </p>

      {#if updater.notes && !busy}
        <div class="u-notes">{updater.notes}</div>
      {/if}

      {#if busy}
        <div class="u-progress">
          <div class="u-bar" style:width="{pct}%"></div>
        </div>
        <p class="u-status">
          {#if updater.stage === 'installing'}
            Kuruluyor — uygulama birazdan yeniden başlayacak…
          {:else if updater.total > 0}
            {mb(updater.downloaded)} / {mb(updater.total)} MB (%{pct})
          {:else}
            İndiriliyor…
          {/if}
        </p>
      {:else}
        <div class="u-actions">
          <button class="u-later" onclick={dismissUpdate}>Sonra</button>
          <button class="u-go" onclick={() => void downloadAndInstall()}>
            İndir ve Kur
          </button>
        </div>
        <p class="u-hint">Kurulum bitince uygulama yeniden başlatılır.</p>
      {/if}
    </div>
  </div>
{/if}

<style>
  .u-backdrop {
    position: fixed;
    inset: 0;
    z-index: 10000;
    display: grid;
    place-items: center;
    background: rgba(15, 23, 42, 0.5);
  }
  .u-modal {
    width: min(460px, 92vw);
    padding: 1.4rem 1.6rem;
    border-radius: 12px;
    background: #fff;
    color: #1e293b;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.32);
  }
  h2 {
    margin: 0 0 0.6rem;
    font-size: 17px;
  }
  .u-ver {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0 0 0.9rem;
    font-family: var(--mono, ui-monospace, monospace);
    font-size: 13px;
  }
  .u-old {
    color: #94a3b8;
    text-decoration: line-through;
  }
  .u-arrow {
    color: #94a3b8;
  }
  .u-new {
    color: #16a34a;
  }
  .u-notes {
    max-height: 220px;
    overflow-y: auto;
    padding: 0.7rem 0.85rem;
    margin-bottom: 1rem;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    background: #f8fafc;
    font-size: 12.5px;
    line-height: 1.5;
    white-space: pre-wrap;
  }
  .u-progress {
    height: 8px;
    margin-bottom: 0.5rem;
    border-radius: 999px;
    background: #e2e8f0;
    overflow: hidden;
  }
  .u-bar {
    height: 100%;
    background: #2563eb;
    transition: width 0.15s linear;
  }
  .u-status {
    margin: 0;
    font-size: 12px;
    color: #64748b;
  }
  .u-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
  }
  .u-later,
  .u-go {
    padding: 0.45rem 0.95rem;
    border-radius: 6px;
    font-size: 13px;
    cursor: pointer;
  }
  .u-later {
    border: 1px solid #cbd5e1;
    background: #fff;
    color: #475569;
  }
  .u-later:hover {
    background: #f1f5f9;
  }
  .u-go {
    border: 1px solid #1d4ed8;
    background: #2563eb;
    color: #fff;
  }
  .u-go:hover {
    background: #1d4ed8;
  }
  .u-hint {
    margin: 0.6rem 0 0;
    font-size: 11px;
    color: #94a3b8;
    text-align: right;
  }

  :global(html.dark) .u-modal {
    background: #1e293b;
    color: #e5e7eb;
  }
  :global(html.dark) .u-notes {
    background: #0f172a;
    border-color: #334155;
  }
  :global(html.dark) .u-progress {
    background: #334155;
  }
  :global(html.dark) .u-later {
    background: #1e293b;
    border-color: #475569;
    color: #cbd5e1;
  }
  :global(html.dark) .u-later:hover {
    background: #26344b;
  }
</style>
