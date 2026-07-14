<script lang="ts">
  /**
   * Sekme şeridi — her sekme bir XSLT+XML çifti (bkz. editor-state.svelte.ts).
   *
   * Şerit yalnızca gösterir ve olay yayar; sekme <b>değiştirme</b> mantığı
   * (CodeMirror'a içerik yazmak, dirty izleyiciyi susturmak) `+page.svelte`'de
   * durur — çünkü editör örnekleri orada yaşıyor.
   */
  import { tabsState, tabTitle, isTabDirty, moveTab, type EditorTab } from './editor-state.svelte';
  import { m } from './i18n.svelte';

  interface Props {
    onselect: (id: number) => void;
    onclose: (id: number) => void;
    onnew: () => void;
  }

  let { onselect, onclose, onnew }: Props = $props();

  /** Sürüklenen sekmenin indeksi (yeniden sıralama). */
  let dragIndex = $state<number | null>(null);
  let dropIndex = $state<number | null>(null);

  /** Tam yollar — üzerine gelince ne olduğu görünsün. */
  function tooltip(tab: EditorTab): string {
    const lines: string[] = [];
    if (tab.xsltPath) lines.push(`XSLT: ${tab.xsltPath}`);
    if (tab.xmlPath) lines.push(`XML: ${tab.xmlPath}`);
    if (lines.length === 0) lines.push(m.tabs.unsavedTab);
    if (isTabDirty(tab)) lines.push(m.common.unsavedChanges);
    return lines.join('\n');
  }

  /** Orta tık = kapat (editör geleneği). */
  function onPointerDown(e: MouseEvent, id: number) {
    if (e.button === 1) {
      e.preventDefault();
      onclose(id);
    }
  }

  function onDragStart(e: DragEvent, i: number) {
    dragIndex = i;
    e.dataTransfer?.setData('text/plain', String(i));
    if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
  }

  function onDragOver(e: DragEvent, i: number) {
    if (dragIndex === null) return;
    e.preventDefault(); // bırakmaya izin ver
    dropIndex = i;
  }

  function onDrop(e: DragEvent, i: number) {
    e.preventDefault();
    if (dragIndex !== null) moveTab(dragIndex, i);
    dragIndex = null;
    dropIndex = null;
  }

  function onDragEnd() {
    dragIndex = null;
    dropIndex = null;
  }
</script>

<div class="tabbar" role="tablist">
  {#each tabsState.list as tab, i (tab.id)}
    <div
      class="tab"
      class:active={tab.id === tabsState.activeId}
      class:dragging={dragIndex === i}
      class:dropzone={dropIndex === i && dragIndex !== i}
      role="tab"
      tabindex="0"
      aria-selected={tab.id === tabsState.activeId}
      title={tooltip(tab)}
      draggable="true"
      onclick={() => onselect(tab.id)}
      onkeydown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onselect(tab.id);
        }
      }}
      onmousedown={(e) => onPointerDown(e, tab.id)}
      ondragstart={(e) => onDragStart(e, i)}
      ondragover={(e) => onDragOver(e, i)}
      ondrop={(e) => onDrop(e, i)}
      ondragend={onDragEnd}
    >
      <span class="tab-title">{tabTitle(tab, m.tabs.newTab)}</span>
      {#if isTabDirty(tab)}
        <span class="tab-dirty" title={m.common.unsavedChanges}>●</span>
      {/if}
      <button
        class="tab-close"
        title={m.tabs.closeTitle}
        aria-label={m.tabs.close}
        onclick={(e) => {
          e.stopPropagation();
          onclose(tab.id);
        }}>×</button>
    </div>
  {/each}

  <button class="tab-new" onclick={onnew} title={m.tabs.newTitle} aria-label={m.tabs.newTab}>+</button>
</div>

<style>
  .tabbar {
    display: flex;
    align-items: stretch;
    gap: 2px;
    padding: 0 0.25rem;
    background: #e8eaee;
    border-bottom: 1px solid #e5e7eb;
    overflow-x: auto;
    overflow-y: hidden;
    scrollbar-width: thin;
  }
  .tabbar::-webkit-scrollbar {
    height: 4px;
  }

  .tab {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    flex: 0 1 auto;
    min-width: 0;
    max-width: 180px;
    padding: 0 0.2rem 0 0.55rem;
    font-size: 11px;
    color: #4b5563;
    background: #dcdfe4;
    border: 1px solid transparent;
    border-bottom: none;
    border-radius: 4px 4px 0 0;
    cursor: pointer;
    user-select: none;
    white-space: nowrap;
  }
  .tab:hover {
    background: #d2d6dc;
  }
  .tab.active {
    background: #f0f2f5;
    color: #111827;
    font-weight: 600;
    border-color: #e5e7eb;
  }
  .tab.dragging {
    opacity: 0.4;
  }
  .tab.dropzone {
    border-left: 2px solid #2563eb;
  }

  .tab-title {
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .tab-dirty {
    flex: none;
    color: #d97706;
    font-size: 9px;
  }

  .tab-close {
    flex: none;
    width: 15px;
    height: 15px;
    line-height: 1;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 13px;
    color: #6b7280;
    background: none;
    border: none;
    border-radius: 3px;
    cursor: pointer;
  }
  .tab-close:hover {
    background: #c3c7cd;
    color: #111827;
  }

  .tab-new {
    flex: none;
    align-self: center;
    width: 18px;
    height: 18px;
    margin-left: 2px;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    color: #6b7280;
    background: none;
    border: none;
    border-radius: 3px;
    cursor: pointer;
  }
  .tab-new:hover {
    background: #d2d6dc;
    color: #111827;
  }

  /* ── Koyu tema ── */
  :global(.app.dark) .tabbar {
    background: #252528;
    border-bottom-color: #3f3f46;
  }
  :global(.app.dark) .tab {
    background: #2d2d30;
    color: #a0a0a0;
  }
  :global(.app.dark) .tab:hover {
    background: #37373b;
  }
  :global(.app.dark) .tab.active {
    background: #1e1e1e;
    color: #e6e6e6;
    border-color: #3f3f46;
  }
  :global(.app.dark) .tab-close:hover,
  :global(.app.dark) .tab-new:hover {
    background: #4a4a50;
    color: #e6e6e6;
  }
  :global(.app.dark) .tab-dirty {
    color: #f59e0b;
  }
</style>
