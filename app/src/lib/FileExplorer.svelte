<!--
  FileExplorer — ajanın çalışma klasörünün dosya gezgini (VSCode benzeri).

  <b>Neden burada:</b> ajan dosyaları oluşturup değiştirirken kullanıcı ne olduğunu
  CANLI görmeli. Rozetler (Y/M/S) "ajan bu oturumda neye dokundu"yu gösterir —
  git durumu DEĞİL (fatura klasörleri genelde git deposu değildir; git'e dayansaydık
  rozetler çoğu klasörde hiç görünmezdi). Bkz. `agent-activity.svelte.ts`.

  <b>Kök kilidi:</b> listeleme Rust'taki `agent_list` ile yapılır — o da `guard()` ile
  köke kilitlidir (`..`/symlink kaçışı reddedilir). Gezgin klasör dışını gösteremez.
-->
<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { claude, setClaudeRoot } from '$lib/claude-session.svelte';
  import { agentActivity, activityBadge, reconcileDeleted, clearAgentActivity } from '$lib/agent-activity.svelte';
  import { pickFolder } from '$lib/batch';

  /** Dosyaya çift tıklanınca editörde aç (XSLT+XML çifti için liste alır). */
  let { onOpen }: { onOpen?: (paths: string[]) => void } = $props();

  /** Rust `DirEntry`. */
  interface DirEntry {
    name: string;
    /** Köke göreli yol. */
    rel: string;
    is_dir: boolean;
    bytes: number;
  }

  /** Açık klasörlerin göreli yolları ('' = kök). */
  let acik = $state(new Set<string>(['']));
  /** Göreli klasör yolu → içeriği (yüklendiyse). */
  let icerik = $state<Record<string, DirEntry[]>>({});
  let hata = $state('');
  let yukleniyor = $state(false);

  const AC_UZANTI = ['xslt', 'xsl', 'xml'];

  function uzanti(ad: string): string {
    return ad.slice(ad.lastIndexOf('.') + 1).toLowerCase();
  }
  function acilabilir(e: DirEntry): boolean {
    return !e.is_dir && AC_UZANTI.includes(uzanti(e.name));
  }
  /** Göreli yolu mutlak yola çevir (rozet aramak ve editörde açmak için). */
  function mutlak(rel: string): string {
    const ayrac = claude.root.includes('\\') ? '\\' : '/';
    return rel ? `${claude.root}${ayrac}${rel}` : claude.root;
  }

  async function klasoruYukle(rel: string): Promise<void> {
    if (!claude.root) return;
    yukleniyor = true;
    hata = '';
    try {
      const liste = await invoke<DirEntry[]>('agent_list', { root: claude.root, sub: rel });
      // Klasörler önce, sonra alfabetik — VSCode düzeni.
      liste.sort((a, b) =>
        a.is_dir !== b.is_dir ? (a.is_dir ? -1 : 1) : a.name.localeCompare(b.name, 'tr'),
      );
      icerik[rel] = liste;
      // Bildiğimiz ama artık listede olmayan dosyalar → "silindi" rozeti.
      reconcileDeleted(
        mutlak(rel),
        liste.filter((e) => !e.is_dir).map((e) => mutlak(e.rel)),
      );
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
    } finally {
      yukleniyor = false;
    }
  }

  async function klasorTikla(rel: string): Promise<void> {
    const s = new Set(acik);
    if (s.has(rel)) {
      s.delete(rel);
    } else {
      s.add(rel);
      if (!icerik[rel]) await klasoruYukle(rel);
    }
    acik = s;
  }

  /** Açık tüm klasörleri diskten tazele (ajan iş yaptıktan sonra). */
  async function tazele(): Promise<void> {
    for (const rel of [...acik]) await klasoruYukle(rel);
  }

  async function klasorSec(): Promise<void> {
    const f = await pickFolder();
    if (!f) return;
    setClaudeRoot(f);
    icerik = {};
    acik = new Set(['']);
    await klasoruYukle('');
  }

  // Kök seçilince/değişince kökü yükle.
  $effect(() => {
    const kok = claude.root;
    if (kok && !icerik['']) void klasoruYukle('');
  });

  // Ajan bir koşuyu bitirdiğinde gezgini tazele — kullanıcı elle yenilemek zorunda kalmasın.
  $effect(() => {
    const kosuyor = claude.running;
    if (!kosuyor && claude.root && icerik['']) void tazele();
  });

  /** Bir satırın rozeti (yoksa null). */
  function rozet(e: DirEntry) {
    if (e.is_dir) return null;
    const durum = agentActivity.dosyalar[mutlak(e.rel)];
    return durum ? activityBadge(durum) : null;
  }

  function boyut(n: number): string {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(0)} KB`;
    return `${(n / 1024 / 1024).toFixed(1)} MB`;
  }

  const dokunulanSayisi = $derived(Object.keys(agentActivity.dosyalar).length);
</script>

<div class="explorer">
  <div class="exp-bar">
    <button class="exp-btn" onclick={klasorSec} title="Çalışma klasörü seç">📁 Klasör</button>
    <button class="exp-btn" onclick={tazele} disabled={!claude.root || yukleniyor} title="Yenile">
      ⟳
    </button>
    {#if dokunulanSayisi > 0}
      <span class="exp-count" title="Ajanın bu oturumda dokunduğu dosya sayısı">
        {dokunulanSayisi} değişiklik
      </span>
      <button class="exp-btn ghost" onclick={clearAgentActivity} title="Rozetleri temizle">✕</button>
    {/if}
  </div>

  {#if !claude.root}
    <p class="exp-empty">
      Ajanla aynı çalışma klasörünü gösterir. Başlamak için bir klasör seç — Claude Code
      panelinde seçtiğin klasör burada da görünür.
    </p>
  {:else}
    <code class="exp-root" title={claude.root}>{claude.root}</code>
    {#if hata}<p class="exp-err">{hata}</p>{/if}

    <div class="exp-tree">
      {#snippet dal(rel: string, derinlik: number)}
        {#each icerik[rel] ?? [] as e (e.rel)}
          {@const r = rozet(e)}
          <div class="exp-row" style="padding-left:{derinlik * 12 + 4}px">
            {#if e.is_dir}
              <button class="exp-item" onclick={() => klasorTikla(e.rel)}>
                <span class="exp-caret">{acik.has(e.rel) ? '▾' : '▸'}</span>
                <span class="exp-icon">📁</span>
                <span class="exp-name">{e.name}</span>
              </button>
            {:else}
              <button
                class="exp-item"
                class:openable={acilabilir(e)}
                ondblclick={() => acilabilir(e) && onOpen?.([mutlak(e.rel)])}
                onclick={() => acilabilir(e) && onOpen?.([mutlak(e.rel)])}
                title={acilabilir(e) ? 'Editörde aç' : 'Bu tür editörde açılamaz'}
              >
                <span class="exp-caret"></span>
                <span class="exp-icon">{acilabilir(e) ? '📄' : '·'}</span>
                <span class="exp-name">{e.name}</span>
                {#if r}
                  <span class="exp-badge" style="color:{r.renk}" title={r.baslik}>{r.harf}</span>
                {/if}
                <span class="exp-size">{boyut(e.bytes)}</span>
              </button>
            {/if}
          </div>
          {#if e.is_dir && acik.has(e.rel)}
            {@render dal(e.rel, derinlik + 1)}
          {/if}
        {/each}
      {/snippet}
      {@render dal('', 0)}
    </div>
  {/if}
</div>

<style>
  .explorer { display: flex; flex-direction: column; height: 100%; min-height: 0; font-size: 12px; }
  .exp-bar { display: flex; align-items: center; gap: 4px; padding: 4px 6px; flex-shrink: 0; }
  .exp-btn {
    padding: 2px 8px; border: 1px solid #ccc; background: #f7f7f7; border-radius: 4px;
    cursor: pointer; font-size: 11px;
  }
  .exp-btn:disabled { opacity: 0.5; cursor: default; }
  .exp-btn.ghost { border: none; background: none; color: #999; }
  .exp-count {
    font-size: 10px; background: #fff8e1; border: 1px solid #ffe082; color: #8a6d3b;
    border-radius: 8px; padding: 0 6px; white-space: nowrap;
  }
  .exp-root {
    font-size: 10px; color: #777; padding: 0 8px 4px; overflow: hidden;
    text-overflow: ellipsis; white-space: nowrap; flex-shrink: 0;
  }
  .exp-empty, .exp-err { font-size: 11px; color: #777; padding: 8px; margin: 0; line-height: 1.5; }
  .exp-err { color: #b3261e; }
  .exp-tree { flex: 1; min-height: 0; overflow: auto; padding-bottom: 6px; }
  .exp-row { display: flex; }
  .exp-item {
    display: flex; align-items: center; gap: 4px; width: 100%; padding: 2px 6px 2px 0;
    background: none; border: none; cursor: pointer; font: inherit; text-align: left; color: #333;
    border-radius: 3px;
  }
  .exp-item:hover { background: #eef2ff; }
  .exp-caret { width: 10px; color: #888; flex-shrink: 0; }
  .exp-icon { flex-shrink: 0; }
  .exp-name { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .exp-item:not(.openable) .exp-name { color: #999; }
  .exp-badge { font-weight: 700; font-size: 11px; flex-shrink: 0; }
  .exp-size { font-size: 10px; color: #aaa; flex-shrink: 0; }

  :global(html.dark) .exp-item { color: #ddd; }
  :global(html.dark) .exp-item:hover { background: #2b2b40; }
  :global(html.dark) .exp-btn { background: #333; color: #ddd; border-color: #555; }
  :global(html.dark) .exp-root, :global(html.dark) .exp-empty { color: #999; }
</style>
