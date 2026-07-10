<!--
  SnippetEditor — kullanıcı tanımlı snippet ekleme/düzenleme formu.

  Kullanım:
    let editingSnippet = $state<Snippet | undefined>(undefined);
    let snippetEditorOpen = $state(false);
    {#if snippetEditorOpen}
      <SnippetEditor
        snippet={editingSnippet}
        categories={categories}
        existingKeys={allOtherKeys}
        onsave={(s, originalKey) => { ... }}
        onclose={() => (snippetEditorOpen = false)}
      />
    {/if}
-->
<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import type { Snippet } from './data/types';

  interface Props {
    /** Düzenlenecek snippet — verilmezse yeni snippet formu açılır. */
    snippet?: Snippet;
    /** Kategori seçimi için mevcut kategori adları (öneri listesi). */
    categories: string[];
    /** Anahtar benzersizliği kontrolü için (düzenlenen snippet'in kendi anahtarı hariç) mevcut tüm anahtarlar. */
    existingKeys: string[];
    onsave: (snippet: Snippet, originalKey?: string) => void;
    onclose: () => void;
  }

  let { snippet, categories, existingKeys, onsave, onclose }: Props = $props();

  // svelte-ignore state_referenced_locally
  const isEdit = !!snippet;

  // svelte-ignore state_referenced_locally
  let key = $state(snippet?.key ?? '');
  // svelte-ignore state_referenced_locally
  let category = $state(snippet?.category ?? categories[0] ?? 'Kullanıcı Tanımlı');
  // svelte-ignore state_referenced_locally
  let subCategory = $state(snippet?.subCategory ?? '');
  // svelte-ignore state_referenced_locally
  let displayName = $state(snippet?.displayName ?? '');
  // svelte-ignore state_referenced_locally
  let description = $state(snippet?.description ?? '');
  // svelte-ignore state_referenced_locally
  let iconText = $state(snippet?.iconText ?? '📝');
  // svelte-ignore state_referenced_locally
  let xsltCode = $state(snippet?.xsltCode ?? '');
  let error = $state('');

  function submit() {
    const trimmedKey = key.trim();
    if (!trimmedKey) {
      error = 'Anahtar (key) zorunludur.';
      return;
    }
    if (existingKeys.includes(trimmedKey)) {
      error = `"${trimmedKey}" anahtarı zaten kullanılıyor. Başka bir anahtar seçin.`;
      return;
    }
    if (!displayName.trim()) {
      error = 'Görünen ad zorunludur.';
      return;
    }
    if (!category.trim()) {
      error = 'Kategori zorunludur.';
      return;
    }
    if (!xsltCode.trim()) {
      error = 'XSLT kodu boş olamaz.';
      return;
    }
    onsave(
      {
        key: trimmedKey,
        category: category.trim(),
        subCategory: subCategory.trim() || null,
        displayName: displayName.trim(),
        description: description.trim(),
        iconText: iconText.trim() || '📝',
        xsltCode,
        dragDataString: '',
      },
      snippet?.key,
    );
  }

  function onKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') onclose();
  }

  onMount(() => window.addEventListener('keydown', onKeydown));
  onDestroy(() => {
    if (typeof window !== 'undefined') window.removeEventListener('keydown', onKeydown);
  });
</script>

<div class="se-overlay" role="presentation" onclick={(e) => e.target === e.currentTarget && onclose()}>
  <div class="se-modal" role="dialog" aria-label={isEdit ? 'Snippet Düzenle' : 'Snippet Ekle'}>
    <header class="se-header">
      <h2>{isEdit ? '✏️ Snippet Düzenle' : '➕ Yeni Snippet'}</h2>
      <button class="se-close" onclick={onclose} title="Kapat (Esc)">✕</button>
    </header>

    <div class="se-body">
      {#if error}
        <div class="se-error">{error}</div>
      {/if}

      <div class="se-row">
        <label for="se-key">Anahtar (key)</label>
        <input id="se-key" type="text" bind:value={key} placeholder="ör. UBLTR-YENI-ALAN" />
      </div>

      <div class="se-row two">
        <div>
          <label for="se-category">Kategori</label>
          <input id="se-category" type="text" list="se-category-list" bind:value={category} />
          <datalist id="se-category-list">
            {#each categories as c}<option value={c}></option>{/each}
          </datalist>
        </div>
        <div>
          <label for="se-subcategory">Alt Kategori (opsiyonel)</label>
          <input id="se-subcategory" type="text" bind:value={subCategory} />
        </div>
      </div>

      <div class="se-row two">
        <div>
          <label for="se-displayname">Görünen Ad</label>
          <input id="se-displayname" type="text" bind:value={displayName} />
        </div>
        <div>
          <label for="se-icon">Simge (emoji)</label>
          <input id="se-icon" type="text" bind:value={iconText} maxlength="2" />
        </div>
      </div>

      <div class="se-row">
        <label for="se-description">Açıklama</label>
        <input id="se-description" type="text" bind:value={description} />
      </div>

      <div class="se-row">
        <label for="se-code">XSLT Kodu</label>
        <textarea id="se-code" bind:value={xsltCode} rows="10" spellcheck="false"></textarea>
      </div>
    </div>

    <footer class="se-footer">
      <button class="se-cancel" onclick={onclose}>İptal</button>
      <button class="se-save" onclick={submit}>{isEdit ? 'Güncelle' : 'Ekle'}</button>
    </footer>
  </div>
</div>

<style>
  .se-overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 23, 42, 0.5);
    z-index: 20000;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .se-modal {
    width: min(640px, 92vw);
    max-height: 88vh;
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    display: grid;
    grid-template-rows: auto 1fr auto;
    overflow: hidden;
    color: #1a1a1a;
  }
  .se-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 1.25rem;
    background: linear-gradient(180deg, #ffffff 0%, #eef0f3 100%);
    border-bottom: 1px solid #d5d8dc;
  }
  .se-header h2 {
    margin: 0;
    font-size: 16px;
    color: #0a5cff;
  }
  .se-close {
    width: 28px;
    height: 28px;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 6px;
    cursor: pointer;
    font-size: 14px;
    color: #4b5563;
  }
  .se-close:hover {
    background: #fee2e2;
    color: #b91c1c;
    border-color: #fca5a5;
  }
  .se-body {
    padding: 1rem 1.25rem;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }
  .se-error {
    background: #fee2e2;
    color: #b91c1c;
    border: 1px solid #fca5a5;
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    font-size: 12px;
  }
  .se-row label {
    display: block;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: #6b7280;
    margin-bottom: 0.25rem;
  }
  .se-row input,
  .se-row textarea {
    width: 100%;
    box-sizing: border-box;
    padding: 0.4rem 0.55rem;
    border: 1px solid #cbd0d6;
    border-radius: 5px;
    font-size: 13px;
  }
  .se-row textarea {
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    resize: vertical;
  }
  .se-row.two {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.75rem;
  }
  .se-footer {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    padding: 0.75rem 1.25rem;
    border-top: 1px solid #d5d8dc;
    background: #fafbfc;
  }
  .se-cancel,
  .se-save {
    padding: 0.45rem 1rem;
    border-radius: 6px;
    font-size: 13px;
    cursor: pointer;
    border: 1px solid #cbd0d6;
    background: #fff;
  }
  .se-save {
    background: #0a5cff;
    border-color: #0a5cff;
    color: #fff;
    font-weight: 600;
  }
  .se-save:hover {
    background: #0847c9;
  }
  .se-cancel:hover {
    background: #f0f2f5;
  }
</style>
