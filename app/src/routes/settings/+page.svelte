<script lang="ts">
  import { settings, updateSetting, resetSettings, THEME_OPTIONS } from '$lib/settings.svelte';
  import { goto } from '$app/navigation';
</script>

<div class="settings-page">
  <header class="page-header">
    <button class="back" onclick={() => goto('/')}>← Geri</button>
    <h1>Ayarlar</h1>
    <button class="reset" onclick={resetSettings}>Varsayılana Sıfırla</button>
  </header>

  <div class="content">
    <section class="group">
      <h2>Editör</h2>

      <div class="row">
        <label for="font-size">Yazı Tipi Boyutu</label>
        <input
          id="font-size"
          type="range"
          min="10"
          max="24"
          step="1"
          value={settings.fontSize}
          oninput={(e) => updateSetting('fontSize', +(e.currentTarget as HTMLInputElement).value)}
        />
        <span class="val">{settings.fontSize}px</span>
      </div>

      <div class="row">
        <label for="tab-width">Sekme Genişliği</label>
        <select
          id="tab-width"
          value={settings.tabWidth}
          onchange={(e) => updateSetting('tabWidth', +(e.currentTarget as HTMLSelectElement).value)}
        >
          <option value={2}>2 boşluk</option>
          <option value={4}>4 boşluk</option>
          <option value={8}>8 boşluk</option>
        </select>
      </div>

      <div class="row">
        <label for="wrap">Kelime Kaydırma</label>
        <input
          id="wrap"
          type="checkbox"
          checked={settings.wordWrap}
          onchange={(e) =>
            updateSetting('wordWrap', (e.currentTarget as HTMLInputElement).checked)}
        />
      </div>

      <div class="row">
        <label for="linenum">Satır Numarası</label>
        <input
          id="linenum"
          type="checkbox"
          checked={settings.showLineNumbers}
          onchange={(e) =>
            updateSetting('showLineNumbers', (e.currentTarget as HTMLInputElement).checked)}
        />
      </div>
    </section>

    <section class="group">
      <h2>Görünüm</h2>

      <div class="row">
        <label for="theme">Tema</label>
        <select
          id="theme"
          value={settings.theme}
          onchange={(e) =>
            updateSetting(
              'theme',
              (e.currentTarget as HTMLSelectElement).value as typeof settings.theme,
            )}
        >
          {#each THEME_OPTIONS as opt}
            <option value={opt.value}>{opt.label} ({opt.kind === 'dark' ? 'Koyu' : 'Açık'})</option>
          {/each}
        </select>
      </div>
    </section>

    <section class="group">
      <h2>Davranış</h2>

      <div class="row">
        <label for="autoxform">Örnek/dosya yüklendiğinde otomatik dönüştür</label>
        <input
          id="autoxform"
          type="checkbox"
          checked={settings.autoTransformOnLoad}
          onchange={(e) =>
            updateSetting(
              'autoTransformOnLoad',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>

      <div class="row">
        <label for="autoxformsave">Kaydettikten sonra otomatik dönüştür</label>
        <input
          id="autoxformsave"
          type="checkbox"
          checked={settings.autoTransformOnSave}
          onchange={(e) =>
            updateSetting(
              'autoTransformOnSave',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>

      <div class="row">
        <label for="autocomplete">Autocomplete (Ctrl+Space)</label>
        <input
          id="autocomplete"
          type="checkbox"
          checked={settings.autocomplete}
          onchange={(e) =>
            updateSetting(
              'autocomplete',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>
    </section>

    <section class="group panel-sizes">
      <h2>Panel Boyutları</h2>
      <p class="hint">
        Mouse ile bölmeleri sürükleyerek de değiştirebilirsin. Bu değerler
        otomatik kaydedilir.
      </p>
      <div class="row">
        <span class="static-label">Snippet Panel Genişliği</span>
        <span class="val">{settings.panelSizes.snippetsWidth}px</span>
      </div>
      <div class="row">
        <span class="static-label">Editör Kolonu Genişliği</span>
        <span class="val">{settings.panelSizes.editorsWidth}px</span>
      </div>
      <div class="row">
        <span class="static-label">XSLT Editör Yüksekliği</span>
        <span class="val">{settings.panelSizes.xsltHeight}px</span>
      </div>
    </section>
  </div>
</div>

<style>
  :global(body) {
    background: #f5f6f8;
  }
  .settings-page {
    max-width: 800px;
    margin: 0 auto;
    padding: 1rem;
    height: 100vh;
    overflow-y: auto;
  }
  .page-header {
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem 1rem;
    background: #fff;
    border: 1px solid #d5d8dc;
    border-radius: 8px;
    margin-bottom: 1rem;
  }
  .page-header h1 {
    margin: 0;
    font-size: 18px;
    color: #1a1a1a;
  }
  .back,
  .reset {
    padding: 0.4rem 0.8rem;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 4px;
    cursor: pointer;
    font-size: 13px;
  }
  .back:hover {
    background: #f0f2f5;
  }
  .reset {
    color: #b91c1c;
    border-color: #fca5a5;
  }
  .reset:hover {
    background: #fee2e2;
  }

  .content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }
  .group {
    background: #fff;
    border: 1px solid #d5d8dc;
    border-radius: 8px;
    padding: 1rem 1.25rem;
  }
  .group h2 {
    margin: 0 0 0.75rem 0;
    font-size: 14px;
    color: #0a5cff;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  .row {
    display: grid;
    grid-template-columns: 1fr auto auto;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid #f0f2f5;
  }
  .row:last-child {
    border-bottom: none;
  }
  .row label,
  .row .static-label {
    font-size: 13px;
    color: #1a1a1a;
  }
  .row input[type='range'] {
    width: 200px;
  }
  .row input[type='checkbox'] {
    width: 18px;
    height: 18px;
    cursor: pointer;
  }
  .row select {
    padding: 0.3rem 0.5rem;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 13px;
    background: #fff;
    cursor: pointer;
  }
  .val {
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    color: #6b7280;
    min-width: 60px;
    text-align: right;
  }
  .hint {
    margin: 0 0 0.75rem 0;
    font-size: 12px;
    color: #6b7280;
    line-height: 1.4;
  }
</style>
