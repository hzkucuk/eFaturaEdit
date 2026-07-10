<!--
  Splitter — iki panel arasına konulan sürüklenebilir tutamaç.

  Kullanım (yatay bölme = 2 sütun):
    <div class="parent" style="grid-template-columns: {leftPx}px 4px 1fr">
      <div>...sol...</div>
      <Splitter direction="vertical" bind:position={leftPx} min={150} max={600} />
      <div>...sağ...</div>
    </div>

  Kullanım (dikey bölme = 2 satır):
    <div class="parent" style="grid-template-rows: {topPx}px 4px 1fr">
      <div>...üst...</div>
      <Splitter direction="horizontal" bind:position={topPx} min={100} max={800} />
      <div>...alt...</div>
    </div>

  <b>direction</b> = tutamacın kendisinin yönü:
    - "vertical"   → dikey tutamaç (sol/sağ paneli ayırır) — imleç ↔
    - "horizontal" → yatay tutamaç (üst/alt paneli ayırır) — imleç ↕
-->
<script lang="ts">
  interface Props {
    direction: 'vertical' | 'horizontal';
    position: number;
    min?: number;
    max?: number;
  }

  let { direction, position = $bindable(), min = 100, max = 2000 }: Props = $props();

  let dragging = $state(false);
  let startCoord = 0;
  let startPos = 0;

  function onPointerDown(e: PointerEvent) {
    dragging = true;
    startCoord = direction === 'vertical' ? e.clientX : e.clientY;
    startPos = position;
    (e.target as HTMLElement).setPointerCapture(e.pointerId);
  }

  function onPointerMove(e: PointerEvent) {
    if (!dragging) return;
    const coord = direction === 'vertical' ? e.clientX : e.clientY;
    const delta = coord - startCoord;
    position = Math.max(min, Math.min(max, startPos + delta));
  }

  function onPointerUp(e: PointerEvent) {
    dragging = false;
    (e.target as HTMLElement).releasePointerCapture(e.pointerId);
  }
</script>

<div
  class="splitter"
  class:vertical={direction === 'vertical'}
  class:horizontal={direction === 'horizontal'}
  class:dragging
  role="separator"
  aria-orientation={direction === 'vertical' ? 'vertical' : 'horizontal'}
  onpointerdown={onPointerDown}
  onpointermove={onPointerMove}
  onpointerup={onPointerUp}
></div>

<style>
  .splitter {
    background: #d5d8dc;
    transition: background 0.15s;
    user-select: none;
    touch-action: none;
    z-index: 1;
  }
  .splitter.vertical {
    cursor: col-resize;
    width: 100%;
    height: 100%;
  }
  .splitter.horizontal {
    cursor: row-resize;
    width: 100%;
    height: 100%;
  }
  .splitter:hover,
  .splitter.dragging {
    background: #0a5cff;
  }
</style>
