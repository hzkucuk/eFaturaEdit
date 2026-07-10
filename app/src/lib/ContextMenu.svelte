<!--
  ContextMenu — pozisyon-tabanlı özel sağ tık menüsü.
  Kullanım:
    let menu = $state<{x:number,y:number} | null>(null);
    <div oncontextmenu={(e) => { e.preventDefault(); menu = {x:e.clientX, y:e.clientY}; }}>...</div>
    {#if menu}
      <ContextMenu x={menu.x} y={menu.y} onclose={() => menu = null}>
        <button onclick={print}>🖨 Yazdır</button>
        <button onclick={savePdf}>📄 PDF Kaydet</button>
      </ContextMenu>
    {/if}
-->
<script lang="ts">
  import { onMount, onDestroy, type Snippet } from 'svelte';

  interface Props {
    x: number;
    y: number;
    onclose: () => void;
    children: Snippet;
  }

  let { x, y, onclose, children }: Props = $props();

  let menuEl: HTMLDivElement;
  // svelte-ignore state_referenced_locally
  let adjusted = $state({ x, y });

  function onGlobalClick(e: MouseEvent) {
    if (!menuEl?.contains(e.target as Node)) onclose();
  }
  function onEscape(e: KeyboardEvent) {
    if (e.key === 'Escape') onclose();
  }

  onMount(() => {
    // Ekran sınırlarını aş
    const rect = menuEl.getBoundingClientRect();
    let nx = x, ny = y;
    if (nx + rect.width > window.innerWidth) nx = window.innerWidth - rect.width - 4;
    if (ny + rect.height > window.innerHeight) ny = window.innerHeight - rect.height - 4;
    adjusted = { x: Math.max(4, nx), y: Math.max(4, ny) };

    // Kısa gecikme sonrası global click dinle (aynı sağ tık'ı yakalamamak için)
    setTimeout(() => {
      window.addEventListener('click', onGlobalClick);
      window.addEventListener('contextmenu', onGlobalClick);
    }, 0);
    window.addEventListener('keydown', onEscape);
  });

  onDestroy(() => {
    if (typeof window !== 'undefined') {
      window.removeEventListener('click', onGlobalClick);
      window.removeEventListener('contextmenu', onGlobalClick);
      window.removeEventListener('keydown', onEscape);
    }
  });
</script>

<div
  bind:this={menuEl}
  class="context-menu"
  style="left: {adjusted.x}px; top: {adjusted.y}px;"
  role="menu"
>
  {@render children()}
</div>

<style>
  .context-menu {
    position: fixed;
    background: #fff;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.16);
    min-width: 200px;
    padding: 4px 0;
    z-index: 10000;
    font-size: 13px;
  }
  :global(.context-menu button) {
    display: block;
    width: 100%;
    text-align: left;
    padding: 6px 14px;
    background: none;
    border: none;
    cursor: pointer;
    color: #1a1a1a;
    font-size: 13px;
  }
  :global(.context-menu button:hover) {
    background: #eef4ff;
  }
  :global(.context-menu button:disabled) {
    color: #9ca3af;
    cursor: not-allowed;
  }
  :global(.context-menu button:disabled:hover) {
    background: none;
  }
  :global(.context-menu .divider) {
    height: 1px;
    background: #e5e7eb;
    margin: 4px 0;
  }
  :global(.app.dark .context-menu) {
    background: #2d2d30;
    border-color: #555;
  }
  :global(.app.dark .context-menu button) {
    color: #e6e6e6;
  }
  :global(.app.dark .context-menu button:hover) {
    background: #094771;
  }
  :global(.app.dark .context-menu .divider) {
    background: #3f3f46;
  }
</style>
