<script lang="ts">
  /**
   * Çeviri düzenleyici — kullanıcı her arayüz metnini KENDİ cihazında
   * değiştirebilir, yeni dil ekleyebilir, dilini JSON olarak dışa aktarıp
   * GitHub'a katkı gönderebilir.
   *
   * Düzenlemeler yerleşik sözlüğün ÜZERİNE yazılır (localStorage) — uygulama
   * güncellense de kaybolmaz, "sıfırla" ile tek tek ya da toptan geri alınır.
   * Yer tutucu bozan bir metin kaydedilirse satırda uyarı gösterilir; sessizce
   * bozuk çeviri bırakmayız.
   */
  import { goto } from '$app/navigation';
  import {
    m,
    f,
    allLocales,
    getLocale,
    listEntries,
    setOverride,
    clearOverrides,
    hasOverrides,
    addCustomLocale,
    removeCustomLocale,
    exportLocale,
    importLocale,
    placeholders as phOf,
    type EditableEntry,
  } from '$lib/i18n.svelte';
  import { log, describeError } from '$lib/logger';

  // Düzenlenen dil (görüntüleme dilinden bağımsız). Türkçe dahil HER dil
  // düzenlenebilir — kullanıcı yerleşik Türkçe ifadeleri de kendine göre
  // değiştirebilmeli (ör. "Farklı Kaydet" yerine başka bir söz).
  let editing = $state(getLocale());

  let filter = $state('');
  let onlyChanged = $state(false);
  let onlyMissing = $state(false);
  let notice = $state('');

  // Kayıt her değişiklikte hemen yapılır; listeyi tazelemek için sayaç.
  let bump = $state(0);

  const entries = $derived.by(() => {
    void bump;
    return listEntries(editing);
  });

  const shown = $derived(
    entries.filter((e) => {
      if (onlyChanged && !e.override) return false;
      if (onlyMissing && (e.builtin || e.override)) return false;
      if (filter) {
        const q = filter.toLowerCase();
        if (
          !`${e.section}.${e.key}`.toLowerCase().includes(q) &&
          !e.reference.toLowerCase().includes(q) &&
          !e.builtin.toLowerCase().includes(q) &&
          !e.override.toLowerCase().includes(q)
        )
          return false;
      }
      return true;
    }),
  );

  const doneCount = $derived(entries.filter((e) => e.override || e.builtin).length);

  /** Kaydedilen metin yer tutucuları bozuyor mu? (satır bazında uyarı) */
  function phBroken(e: EditableEntry): boolean {
    const text = e.override;
    if (!text) return false;
    return phOf(e.reference).join(',') !== phOf(text).join(',');
  }

  function save(e: EditableEntry, text: string) {
    setOverride(editing, e.section, e.key, text);
    bump++;
  }

  function resetKey(e: EditableEntry) {
    setOverride(editing, e.section, e.key, '');
    bump++;
  }

  function resetAll() {
    if (!confirm(m.translations.resetAllConfirm)) return;
    clearOverrides(editing);
    bump++;
  }

  // ── dışa/içe aktarma ─────────────────────────────────────────────
  function doExport() {
    const json = exportLocale(editing);
    const blob = new Blob([json], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `efatura-edit-locale-${editing}.json`;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  let importInput = $state<HTMLInputElement>();
  async function doImport(ev: Event) {
    notice = '';
    const file = (ev.currentTarget as HTMLInputElement).files?.[0];
    if (!file) return;
    try {
      const code = importLocale(await file.text());
      editing = code;
      bump++;
      notice = f(m.translations.imported, { code });
    } catch (err) {
      notice = f(m.translations.importFailed, { msg: describeError(err) });
      log.error(`[çeviri] içe aktarma hatası: ${describeError(err)}`);
    }
    if (importInput) importInput.value = '';
  }

  // ── yeni dil ─────────────────────────────────────────────────────
  let newCode = $state('');
  let newLabel = $state('');
  let newFlag = $state('');
  function addLang() {
    if (!newCode.trim()) return;
    addCustomLocale(newCode, newLabel, newFlag);
    editing = newCode.trim().toLowerCase();
    newCode = newLabel = newFlag = '';
    bump++;
  }

  const editingOption = $derived(allLocales().find((o) => o.code === editing));

  function removeLang() {
    const opt = editingOption;
    if (!opt?.custom) return;
    if (!confirm(f(m.translations.removeConfirm, { label: opt.label }))) return;
    removeCustomLocale(opt.code);
    editing = 'en';
    bump++;
  }
</script>

<div class="tr-page">
  <header class="page-header">
    <button class="back" onclick={() => goto('/settings')}>← {m.common.back}</button>
    <h1>{m.translations.title}</h1>
    <span class="count">{f(m.translations.count, { done: doneCount, total: entries.length })}</span>
  </header>

  <div class="tr-toolbar">
    <label>
      {m.translations.editing}
      <select bind:value={editing} onchange={() => (bump++, (notice = ''))}>
        {#each allLocales() as opt}
          <option value={opt.code}>{opt.flag} {opt.label}{opt.custom ? ' *' : ''}</option>
        {/each}
      </select>
    </label>

    <input type="text" class="search" placeholder={m.common.search} bind:value={filter} />

    <label class="chk"><input type="checkbox" bind:checked={onlyChanged} />{m.translations.onlyChanged}</label>
    <label class="chk"><input type="checkbox" bind:checked={onlyMissing} />{m.translations.onlyMissing}</label>

    <span class="spacer"></span>

    <button onclick={doExport} title={m.translations.exportHint}>⬇ {m.translations.export}</button>
    <button onclick={() => importInput?.click()}>⬆ {m.translations.import}</button>
    <input bind:this={importInput} type="file" accept=".json" style="display:none" onchange={doImport} />
    {#if hasOverrides(editing)}
      <button class="danger" onclick={resetAll}>{m.translations.resetAllOverrides}</button>
    {/if}
    {#if editingOption?.custom}
      <button class="danger" onclick={removeLang}>🗑 {m.translations.removeLanguage}</button>
    {/if}
  </div>

  {#if notice}
    <div class="notice">{notice}</div>
  {/if}

  <div class="tr-list">
    {#each shown as e (e.section + '.' + e.key)}
      <div class="tr-row" class:changed={!!e.override} class:broken={phBroken(e)}>
        <div class="tr-meta">
          <code class="tr-key">{e.section}.{e.key}</code>
          {#if e.placeholders.length}
            <span class="tr-ph" title={m.translations.placeholderWarn}>
              {#each e.placeholders as ph}<code>&#123;{ph}&#125;</code>{/each}
            </span>
          {/if}
        </div>
        <div class="tr-texts">
          {#if editing !== 'tr'}
            <div class="tr-ref" title={m.translations.referenceLabel}>🇹🇷 {e.reference}</div>
          {/if}
          {#if e.builtin}
            <div class="tr-builtin" title={m.translations.builtinLabel}>
              {editingOption?.flag} {e.builtin}
            </div>
          {/if}
          <div class="tr-edit">
            <textarea
              rows={e.reference.length > 80 ? 3 : 1}
              placeholder={e.builtin || m.translations.yourText}
              value={e.override}
              onchange={(ev) => save(e, (ev.currentTarget as HTMLTextAreaElement).value)}
            ></textarea>
            {#if e.override}
              <button class="tr-reset" onclick={() => resetKey(e)} title={m.translations.resetKey}>↺</button>
            {/if}
          </div>
          {#if phBroken(e)}
            <div class="tr-warn">⚠️ {m.translations.placeholderWarn}</div>
          {/if}
        </div>
      </div>
    {/each}
  </div>

  <div class="tr-addlang">
    <h3>{m.translations.addLanguage}</h3>
    <div class="addlang-row">
      <input type="text" maxlength="5" placeholder={m.translations.newCode} bind:value={newCode} />
      <input type="text" placeholder={m.translations.newLabel} bind:value={newLabel} />
      <input type="text" maxlength="4" class="flag" placeholder={m.translations.newFlag} bind:value={newFlag} />
      <button onclick={addLang} disabled={!newCode.trim()}>{m.translations.add}</button>
    </div>
    <p class="hint">{m.translations.subtitle}</p>
  </div>
</div>

<style>
  .tr-page {
    height: 100vh;
    display: flex;
    flex-direction: column;
    background: #f5f6f8;
    color: #1f2430;
  }
  .page-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.7rem 1.2rem;
    background: #fff;
    border-bottom: 1px solid #e2e5ea;
  }
  .page-header h1 { font-size: 17px; margin: 0; flex: 1; }
  .back {
    border: 1px solid #cbd0d6; background: #fff; border-radius: 6px;
    padding: 0.3rem 0.7rem; cursor: pointer; font-size: 13px;
  }
  .count { font-size: 12px; color: #6b7280; font-variant-numeric: tabular-nums; }

  .tr-toolbar {
    display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap;
    padding: 0.6rem 1.2rem; background: #fbfbfc; border-bottom: 1px solid #e9ebef;
    font-size: 12.5px;
  }
  .tr-toolbar label { display: inline-flex; align-items: center; gap: 0.4rem; }
  .tr-toolbar select, .tr-toolbar .search {
    padding: 0.3rem 0.5rem; border: 1px solid #cbd0d6; border-radius: 6px;
    font-size: 12.5px; background: #fff;
  }
  .search { min-width: 200px; }
  .chk { color: #4b5563; }
  .spacer { flex: 1; }
  .tr-toolbar button {
    border: 1px solid #cbd0d6; background: #fff; border-radius: 6px;
    padding: 0.3rem 0.7rem; cursor: pointer; font-size: 12px;
  }
  .tr-toolbar button:hover { background: #f1f3f5; }
  .tr-toolbar .danger { color: #b91c1c; border-color: #fecaca; }

  .notice {
    padding: 0.4rem 1.2rem; background: #eef4fc; border-bottom: 1px solid #c7d7ee;
    color: #1d4ed8; font-size: 12.5px;
  }

  .tr-list { flex: 1; overflow-y: auto; padding: 0.6rem 1.2rem 2rem; }
  .tr-row {
    display: grid; grid-template-columns: 230px 1fr; gap: 0.8rem;
    padding: 0.55rem 0.6rem; border-bottom: 1px solid #eceef2; border-radius: 6px;
  }
  .tr-row.changed { background: #f0fdf4; }
  .tr-row.broken { background: #fef2f2; }
  .tr-meta { min-width: 0; }
  .tr-key { font-size: 11px; color: #6b7280; word-break: break-all; }
  .tr-ph { display: block; margin-top: 0.25rem; }
  .tr-ph code {
    display: inline-block; margin: 0 0.2rem 0.2rem 0; padding: 0 0.3rem;
    background: #ede9fe; color: #6d28d9; border-radius: 4px; font-size: 10.5px;
  }
  .tr-texts { min-width: 0; }
  .tr-ref { font-size: 12.5px; color: #374151; margin-bottom: 0.25rem; white-space: pre-wrap; }
  .tr-builtin { font-size: 12.5px; color: #6b7280; margin-bottom: 0.25rem; white-space: pre-wrap; }
  .tr-edit { display: flex; gap: 0.4rem; align-items: flex-start; }
  .tr-edit textarea {
    flex: 1; padding: 0.35rem 0.5rem; border: 1px solid #cbd0d6; border-radius: 6px;
    font-size: 12.5px; font-family: inherit; resize: vertical; background: #fff;
  }
  .tr-reset {
    border: 1px solid #cbd0d6; background: #fff; border-radius: 6px;
    padding: 0.25rem 0.5rem; cursor: pointer;
  }
  .tr-warn { margin-top: 0.25rem; font-size: 11.5px; color: #b91c1c; }

  .tr-addlang {
    padding: 0.8rem 1.2rem 1.2rem; border-top: 1px solid #e2e5ea; background: #fbfbfc;
  }
  .tr-addlang h3 { margin: 0 0 0.5rem; font-size: 13.5px; }
  .addlang-row { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  .addlang-row input {
    padding: 0.35rem 0.5rem; border: 1px solid #cbd0d6; border-radius: 6px; font-size: 12.5px;
  }
  .addlang-row .flag { width: 4.5rem; }
  .addlang-row button {
    border: 1px solid #1d4ed8; background: #2563eb; color: #fff; border-radius: 6px;
    padding: 0.35rem 0.9rem; cursor: pointer; font-size: 12.5px;
  }
  .addlang-row button:disabled { opacity: 0.5; cursor: default; }
  .hint { margin: 0.5rem 0 0; font-size: 11.5px; color: #6b7280; }

  /* ── koyu tema ── */
  :global(html.dark) .tr-page { background: #1e1e1e; color: #e6e6e6; }
  :global(html.dark) .page-header,
  :global(html.dark) .tr-toolbar,
  :global(html.dark) .tr-addlang { background: #26272b; border-color: #3f3f46; }
  :global(html.dark) .back,
  :global(html.dark) .tr-toolbar button,
  :global(html.dark) .tr-reset {
    background: #2d2d30; border-color: #4b4b52; color: #e6e6e6;
  }
  :global(html.dark) .tr-toolbar select,
  :global(html.dark) .tr-toolbar .search,
  :global(html.dark) .tr-edit textarea,
  :global(html.dark) .addlang-row input {
    background: #1f1f23; border-color: #4b4b52; color: #e6e6e6;
  }
  :global(html.dark) .tr-row { border-bottom-color: #333338; }
  :global(html.dark) .tr-row.changed { background: #16281c; }
  :global(html.dark) .tr-row.broken { background: #2d1a1a; }
  :global(html.dark) .tr-ref { color: #d4d4d8; }
  :global(html.dark) .tr-builtin,
  :global(html.dark) .tr-key,
  :global(html.dark) .chk,
  :global(html.dark) .count,
  :global(html.dark) .hint { color: #9aa1ac; }
  :global(html.dark) .notice { background: #1e293b; border-color: #35507a; color: #93c5fd; }
</style>
