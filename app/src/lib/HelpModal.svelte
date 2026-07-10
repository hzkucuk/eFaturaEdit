<!--
  HelpModal — tam ekran yardım dokümantasyonu penceresi.
  Sidebar navigasyon + arama + içerik alanı.

  Kullanım:
    let helpOpen = $state(false);
    <button onclick={() => (helpOpen = true)}>❓ Yardım</button>
    {#if helpOpen}
      <HelpModal onclose={() => (helpOpen = false)} />
    {/if}
-->
<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { HELP_SECTIONS, searchHelpSections, type HelpSection } from '$lib/help-content';

  interface Props {
    onclose: () => void;
    /** Belirli bir bölümle aç (opsiyonel — bağlamsal yardım için). */
    initialSectionId?: string;
  }

  let { onclose, initialSectionId }: Props = $props();

  let searchTerm = $state('');
  // svelte-ignore state_referenced_locally
  let activeSectionId = $state(initialSectionId ?? HELP_SECTIONS[0].id);

  const filteredSections = $derived(searchHelpSections(searchTerm));
  const activeSection = $derived<HelpSection | undefined>(
    filteredSections.find((s) => s.id === activeSectionId) ?? filteredSections[0],
  );

  function selectSection(id: string) {
    activeSectionId = id;
  }

  function onKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') onclose();
  }

  onMount(() => {
    window.addEventListener('keydown', onKeydown);
  });
  onDestroy(() => {
    if (typeof window !== 'undefined') window.removeEventListener('keydown', onKeydown);
  });
</script>

<div class="help-overlay" role="presentation" onclick={(e) => e.target === e.currentTarget && onclose()}>
  <div class="help-modal" role="dialog" aria-label="Yardım">
    <header class="help-header">
      <h2>📚 Yardım ve Dokümantasyon</h2>
      <button class="help-close" onclick={onclose} title="Kapat (Esc)">✕</button>
    </header>

    <div class="help-body">
      <aside class="help-sidebar">
        <input
          type="text"
          class="help-search"
          placeholder="🔎 Yardımda ara..."
          bind:value={searchTerm}
        />
        <nav class="help-nav">
          {#each filteredSections as section (section.id)}
            <button
              class="help-nav-item"
              class:active={section.id === activeSection?.id}
              onclick={() => selectSection(section.id)}
            >
              <span class="help-nav-icon">{section.icon}</span>
              <span>{section.title}</span>
            </button>
          {:else}
            <p class="help-no-result">"{searchTerm}" için sonuç bulunamadı.</p>
          {/each}
        </nav>
      </aside>

      <div class="help-content">
        {#if activeSection}
          <h3>{activeSection.icon} {activeSection.title}</h3>
          <!-- eslint-disable-next-line svelte/no-at-html-tags -->
          {@html activeSection.html}
        {:else}
          <p class="help-no-result">Bir bölüm seç veya arama yap.</p>
        {/if}
      </div>
    </div>

    <footer class="help-footer">
      <span>💡 İpucu: Herhangi bir buton/kontrol üzerinde bir süre bekleyerek tooltip görebilirsin.</span>
      <span>F1 ile bu pencereyi her zaman açabilirsin.</span>
    </footer>
  </div>
</div>

<style>
  .help-overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 23, 42, 0.5);
    z-index: 20000;
    display: flex;
    align-items: center;
    justify-content: center;
    animation: fadeIn 0.15s ease-out;
  }
  @keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  .help-modal {
    width: min(900px, 92vw);
    height: min(700px, 88vh);
    background: #fff;
    border-radius: 10px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    display: grid;
    grid-template-rows: auto 1fr auto;
    overflow: hidden;
    color: #1a1a1a;
  }

  .help-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem 1.25rem;
    background: linear-gradient(180deg, #ffffff 0%, #eef0f3 100%);
    border-bottom: 1px solid #d5d8dc;
  }
  .help-header h2 {
    margin: 0;
    font-size: 17px;
    color: #0a5cff;
  }
  .help-close {
    width: 28px;
    height: 28px;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 6px;
    cursor: pointer;
    font-size: 14px;
    color: #4b5563;
  }
  .help-close:hover {
    background: #fee2e2;
    color: #b91c1c;
    border-color: #fca5a5;
  }

  .help-body {
    display: grid;
    grid-template-columns: 220px 1fr;
    overflow: hidden;
  }

  .help-sidebar {
    display: flex;
    flex-direction: column;
    background: #fafbfc;
    border-right: 1px solid #e5e7eb;
    overflow: hidden;
  }
  .help-search {
    margin: 0.6rem;
    padding: 0.4rem 0.6rem;
    border: 1px solid #cbd0d6;
    border-radius: 5px;
    font-size: 12px;
  }
  .help-nav {
    flex: 1;
    overflow-y: auto;
    padding: 0 0.4rem 0.6rem;
    display: flex;
    flex-direction: column;
    gap: 2px;
  }
  .help-nav-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.6rem;
    border: none;
    background: transparent;
    border-radius: 5px;
    text-align: left;
    cursor: pointer;
    font-size: 13px;
    color: #1a1a1a;
  }
  .help-nav-item:hover {
    background: #eef4ff;
  }
  .help-nav-item.active {
    background: #0a5cff;
    color: white;
    font-weight: 600;
  }
  .help-nav-icon {
    font-size: 15px;
  }
  .help-no-result {
    padding: 1rem;
    color: #9ca3af;
    font-size: 12px;
    text-align: center;
  }

  .help-content {
    padding: 1.5rem 2rem;
    overflow-y: auto;
    line-height: 1.65;
    font-size: 13.5px;
  }
  .help-content h3 {
    margin: 0 0 1rem;
    color: #0a5cff;
    font-size: 19px;
  }
  .help-content :global(h4) {
    margin: 1.2rem 0 0.5rem;
    color: #1a1a1a;
    font-size: 14px;
  }
  .help-content :global(p) {
    margin: 0 0 0.75rem;
    color: #374151;
  }
  .help-content :global(ul) {
    margin: 0 0 0.75rem;
    padding-left: 1.3rem;
  }
  .help-content :global(li) {
    margin-bottom: 0.4rem;
    color: #374151;
  }
  .help-content :global(strong) {
    color: #1a1a1a;
  }
  .help-content :global(code) {
    background: #f0f2f5;
    padding: 1px 5px;
    border-radius: 3px;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    color: #0a5cff;
  }
  .help-content :global(kbd) {
    display: inline-block;
    padding: 2px 7px;
    background: #f9fafb;
    border: 1px solid #d1d5db;
    border-bottom-width: 2px;
    border-radius: 4px;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    color: #374151;
  }
  .help-content :global(a) {
    color: #0a5cff;
  }
  .help-content :global(.help-table) {
    width: 100%;
    border-collapse: collapse;
    margin: 0.5rem 0 1rem;
  }
  .help-content :global(.help-table th) {
    text-align: left;
    padding: 6px 10px;
    background: #f0f2f5;
    font-size: 11px;
    text-transform: uppercase;
    color: #6b7280;
    border-bottom: 2px solid #e5e7eb;
  }
  .help-content :global(.help-table td) {
    padding: 6px 10px;
    border-bottom: 1px solid #f0f2f5;
    font-size: 12.5px;
  }

  .help-footer {
    display: flex;
    justify-content: space-between;
    padding: 0.6rem 1.25rem;
    background: #f9fafb;
    border-top: 1px solid #e5e7eb;
    font-size: 11px;
    color: #6b7280;
  }
</style>
