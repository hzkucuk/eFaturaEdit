<!--
  FileExplorer — ajanın çalışma klasörünün dosya gezgini (VSCode benzeri).

  <b>Neden burada:</b> ajan dosyaları oluşturup değiştirirken kullanıcı ne olduğunu
  CANLI görmeli. Rozetler (Y/M/S) "ajan bu oturumda neye dokundu"yu gösterir —
  git durumu DEĞİL (fatura klasörleri genelde git deposu değildir). Bkz. `agent-activity`.

  <b>Kök kilidi:</b> listeleme ve TÜM dosya işlemleri (oluştur/sil/ad değiştir/kopyala)
  Rust'taki `guard()`'dan geçer — kaynak ve hedef ayrı ayrı denetlenir, `..`/symlink
  kaçışı reddedilir. Gezgin çalışma klasörünün dışına çıkamaz (5 birim testiyle kanıtlı).
-->
<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { ask } from '@tauri-apps/plugin-dialog';
  import { writeText } from '@tauri-apps/plugin-clipboard-manager';
  import { claude, setClaudeRoot, noteUserActivity } from '$lib/claude-session.svelte';
  import { agentActivity, activityBadge, reconcileDeleted, clearAgentActivity } from '$lib/agent-activity.svelte';
  import { pickFolder } from '$lib/batch';

  let { onOpen }: { onOpen?: (paths: string[]) => void } = $props();

  interface DirEntry {
    name: string;
    /** Köke göreli yol. */
    rel: string;
    is_dir: boolean;
    bytes: number;
  }

  let acik = $state(new Set<string>(['']));
  let icerik = $state<Record<string, DirEntry[]>>({});
  let hata = $state('');
  let yukleniyor = $state(false);
  let yuklenenKok = $state('');

  // Arama
  let arama = $state('');
  let aramaSonuc = $state<DirEntry[] | null>(null);
  let aramaCalisiyor = $state(false);

  // Sağ tık menüsü
  let menu = $state<{ x: number; y: number; hedef: DirEntry | null } | null>(null);
  // Pano (kopyala/kes → yapıştır)
  let pano = $state<{ rel: string; kes: boolean } | null>(null);
  // Satır içi giriş (yeni dosya/klasör/ad değiştir)
  let giris = $state<{ mod: 'dosya' | 'klasor' | 'ad'; hedefRel: string; deger: string } | null>(null);

  const AC_UZANTI = ['xslt', 'xsl', 'xml'];
  const ARAMA_SINIRI = 3000;

  const uzanti = (ad: string) => ad.slice(ad.lastIndexOf('.') + 1).toLowerCase();
  const acilabilir = (e: DirEntry) => !e.is_dir && AC_UZANTI.includes(uzanti(e.name));
  const ayrac = $derived(claude.root.includes('\\') ? '\\' : '/');

  function mutlak(rel: string): string {
    return rel ? `${claude.root}${ayrac}${rel}` : claude.root;
  }
  /** Bir göreli yolun bulunduğu klasör ('' = kök). */
  function ustKlasor(rel: string): string {
    const i = rel.lastIndexOf('/');
    return i === -1 ? '' : rel.slice(0, i);
  }
  function birlestir(klasor: string, ad: string): string {
    return klasor ? `${klasor}/${ad}` : ad;
  }

  async function klasoruYukle(rel: string): Promise<void> {
    if (!claude.root) return;
    yukleniyor = true;
    hata = '';
    try {
      const liste = await invoke<DirEntry[]>('agent_list', { root: claude.root, sub: rel });
      liste.sort((a, b) =>
        a.is_dir !== b.is_dir ? (a.is_dir ? -1 : 1) : a.name.localeCompare(b.name, 'tr'),
      );
      icerik[rel] = liste;
      reconcileDeleted(mutlak(rel), liste.filter((e) => !e.is_dir).map((e) => mutlak(e.rel)));
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
    } finally {
      yukleniyor = false;
    }
  }

  async function klasorTikla(rel: string): Promise<void> {
    const s = new Set(acik);
    if (s.has(rel)) s.delete(rel);
    else {
      s.add(rel);
      if (!icerik[rel]) await klasoruYukle(rel);
    }
    acik = s;
  }

  /** Açık klasörleri diskten tazele. */
  async function tazele(): Promise<void> {
    for (const rel of [...acik]) await klasoruYukle(rel);
    if (arama.trim()) await ara();
  }

  function tumunuKapat(): void {
    acik = new Set(['']);
  }

  async function klasorSec(): Promise<void> {
    const f = await pickFolder();
    if (f) setClaudeRoot(f);
  }

  // Kök DEĞİŞİNCE ağacı sıfırla ve o klasöre konumlan.
  $effect(() => {
    const kok = claude.root;
    if (!kok || kok === yuklenenKok) return;
    yuklenenKok = kok;
    icerik = {};
    acik = new Set(['']);
    arama = '';
    aramaSonuc = null;
    void klasoruYukle('');
  });

  // Ajan koşusu bitince tazele.
  $effect(() => {
    const kosuyor = claude.running;
    if (!kosuyor && claude.root && icerik['']) void tazele();
  });

  // ── Arama: kökten özyinelemeli tara (sınırlı) ────────────────────────────
  async function ara(): Promise<void> {
    const q = arama.trim().toLowerCase();
    if (!q) {
      aramaSonuc = null;
      return;
    }
    aramaCalisiyor = true;
    const bulunan: DirEntry[] = [];
    const kuyruk = [''];
    let sayac = 0;
    try {
      while (kuyruk.length > 0 && sayac < ARAMA_SINIRI) {
        const rel = kuyruk.shift()!;
        let liste = icerik[rel];
        if (!liste) {
          liste = await invoke<DirEntry[]>('agent_list', { root: claude.root, sub: rel });
          icerik[rel] = liste;
        }
        for (const e of liste) {
          sayac++;
          if (e.name.toLowerCase().includes(q)) bulunan.push(e);
          if (e.is_dir) kuyruk.push(e.rel);
        }
      }
      aramaSonuc = bulunan;
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
    } finally {
      aramaCalisiyor = false;
    }
  }

  let aramaTimer: ReturnType<typeof setTimeout> | null = null;
  function aramaDegisti(): void {
    if (aramaTimer) clearTimeout(aramaTimer);
    aramaTimer = setTimeout(() => void ara(), 250);
  }

  // ── Dosya işlemleri ───────────────────────────────────────────────────────
  function dosyaAc(e: DirEntry): void {
    if (!acilabilir(e)) return;
    const yol = mutlak(e.rel);
    onOpen?.([yol]);
    noteUserActivity(`Editörde şu dosyayı açtım: ${yol}`);
  }

  /** Yeni dosya/klasör girişini aç (hedef: seçili klasör ya da seçilinin üstü). */
  function yeniBaslat(mod: 'dosya' | 'klasor', hedef: DirEntry | null): void {
    const klasor = hedef ? (hedef.is_dir ? hedef.rel : ustKlasor(hedef.rel)) : '';
    if (klasor && !acik.has(klasor)) void klasorTikla(klasor);
    giris = { mod, hedefRel: klasor, deger: '' };
    menu = null;
  }

  function adDegistirBaslat(e: DirEntry): void {
    giris = { mod: 'ad', hedefRel: e.rel, deger: e.name };
    menu = null;
  }

  async function girisOnayla(): Promise<void> {
    if (!giris) return;
    const ad = giris.deger.trim();
    if (!ad) {
      giris = null;
      return;
    }
    hata = '';
    try {
      if (giris.mod === 'klasor') {
        await invoke('agent_mkdir', { root: claude.root, path: birlestir(giris.hedefRel, ad) });
      } else if (giris.mod === 'dosya') {
        await invoke('agent_write', {
          root: claude.root,
          path: birlestir(giris.hedefRel, ad),
          content: '',
        });
      } else {
        const yeni = birlestir(ustKlasor(giris.hedefRel), ad);
        await invoke('agent_rename', { root: claude.root, from: giris.hedefRel, to: yeni });
      }
      const yenilenecek = giris.mod === 'ad' ? ustKlasor(giris.hedefRel) : giris.hedefRel;
      giris = null;
      await klasoruYukle(yenilenecek);
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
      giris = null;
    }
  }

  async function sil(e: DirEntry): Promise<void> {
    menu = null;
    const tur = e.is_dir ? 'klasörü (içindeki her şeyle birlikte)' : 'dosyayı';
    // ⚠️ Onay diyaloğu başarısız olursa SİLME — ve sebebi görünür kıl. Sessizce
    // "hiçbir şey olmadı" bırakmak, kullanıcının silme sandığı şeyin durmasından beter.
    let onay = false;
    try {
      onay = await ask(
        `Bu ${tur} kalıcı olarak silmek istiyor musun?\n\n${e.rel}\n\nBu işlem geri alınamaz.`,
        { title: 'Sil', kind: 'warning', okLabel: 'Sil', cancelLabel: 'Vazgeç' },
      );
    } catch (err) {
      hata = `Onay penceresi açılamadı, silme yapılmadı: ${String((err as Error)?.message ?? err)}`;
      return;
    }
    if (!onay) return;
    try {
      await invoke('agent_delete', { root: claude.root, path: e.rel });
      await klasoruYukle(ustKlasor(e.rel));
    } catch (err) {
      hata = String((err as Error)?.message ?? err);
    }
  }

  function kopyala(e: DirEntry, kes: boolean): void {
    pano = { rel: e.rel, kes };
    menu = null;
  }

  async function yapistir(hedef: DirEntry | null): Promise<void> {
    menu = null;
    if (!pano) return;
    const klasor = hedef ? (hedef.is_dir ? hedef.rel : ustKlasor(hedef.rel)) : '';
    const ad = pano.rel.slice(pano.rel.lastIndexOf('/') + 1);
    let to = birlestir(klasor, ad);
    // Aynı klasöre kopyalanıyorsa ad çakışmasın.
    if (!pano.kes && to === pano.rel) {
      const nokta = ad.lastIndexOf('.');
      const govde = nokta > 0 ? ad.slice(0, nokta) : ad;
      const uz = nokta > 0 ? ad.slice(nokta) : '';
      to = birlestir(klasor, `${govde} kopya${uz}`);
    }
    try {
      await invoke(pano.kes ? 'agent_rename' : 'agent_copy', {
        root: claude.root,
        from: pano.rel,
        to,
      });
      if (pano.kes) {
        await klasoruYukle(ustKlasor(pano.rel));
        pano = null;
      }
      await klasoruYukle(klasor);
    } catch (e) {
      hata = String((e as Error)?.message ?? e);
    }
  }

  async function yoluKopyala(e: DirEntry): Promise<void> {
    menu = null;
    try {
      await writeText(mutlak(e.rel));
    } catch (err) {
      hata = String((err as Error)?.message ?? err);
    }
  }

  function menuAc(ev: MouseEvent, hedef: DirEntry | null): void {
    ev.preventDefault();
    menu = { x: ev.clientX, y: ev.clientY, hedef };
  }

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

<svelte:window
  onclick={() => (menu = null)}
  onkeydown={(e) => {
    if (e.key === 'Escape') {
      menu = null;
      giris = null;
    }
  }}
/>

<div class="explorer">
  <div class="exp-bar">
    <button class="exp-btn" onclick={klasorSec} title="Çalışma klasörü seç">📁</button>
    <button class="exp-btn" onclick={tazele} disabled={!claude.root || yukleniyor} title="Yenile">⟳</button>
    <button class="exp-btn" onclick={tumunuKapat} disabled={!claude.root} title="Tümünü kapat">⇱</button>
    <button class="exp-btn" onclick={() => yeniBaslat('dosya', null)} disabled={!claude.root} title="Yeni dosya">🗎﹢</button>
    <button class="exp-btn" onclick={() => yeniBaslat('klasor', null)} disabled={!claude.root} title="Yeni klasör">📁﹢</button>
    <span class="exp-spacer"></span>
    {#if dokunulanSayisi > 0}
      <span class="exp-count" title="Ajanın bu oturumda dokunduğu dosya sayısı">{dokunulanSayisi}</span>
      <button class="exp-btn ghost" onclick={clearAgentActivity} title="Rozetleri temizle">✕</button>
    {/if}
  </div>

  {#if claude.root}
    <div class="exp-search">
      <input
        type="text"
        placeholder="Dosya ara…"
        bind:value={arama}
        oninput={aramaDegisti}
      />
      {#if arama}
        <button class="exp-btn ghost" onclick={() => { arama = ''; aramaSonuc = null; }} title="Aramayı temizle">✕</button>
      {/if}
    </div>
  {/if}

  {#if !claude.root}
    <p class="exp-empty">
      Ajanla aynı çalışma klasörünü gösterir. Başlamak için bir klasör seç — Claude Code
      panelinde seçtiğin klasör burada da görünür.
    </p>
  {:else}
    <code class="exp-root" title={claude.root}>{claude.root}</code>
    {#if hata}<p class="exp-err">{hata}</p>{/if}

    <!-- Boş alana sağ tık = köke yapıştır / yeni oluştur -->
    <div class="exp-tree" oncontextmenu={(e) => menuAc(e, null)} role="tree" tabindex="-1">
      {#if aramaSonuc}
        <div class="exp-sonuc-basligi">
          {aramaCalisiyor ? 'Aranıyor…' : `${aramaSonuc.length} sonuç`}
        </div>
        {#each aramaSonuc as e (e.rel)}
          {@const r = rozet(e)}
          <div class="exp-row">
            <button
              class="exp-item"
              class:openable={acilabilir(e)}
              onclick={() => (e.is_dir ? klasorTikla(e.rel) : dosyaAc(e))}
              oncontextmenu={(ev) => menuAc(ev, e)}
              title={e.rel}
            >
              <span class="exp-icon">{e.is_dir ? '📁' : acilabilir(e) ? '📄' : '·'}</span>
              <span class="exp-name">{e.name}</span>
              {#if r}<span class="exp-badge" style="color:{r.renk}" title={r.baslik}>{r.harf}</span>{/if}
              <span class="exp-rel">{ustKlasor(e.rel) || '.'}</span>
            </button>
          </div>
        {/each}
      {:else}
        {#snippet dal(rel: string, derinlik: number)}
          {#if giris && giris.hedefRel === rel && giris.mod !== 'ad'}
            <div class="exp-row" style="padding-left:{(derinlik + 1) * 12 + 4}px">
              <span class="exp-icon">{giris.mod === 'klasor' ? '📁' : '🗎'}</span>
              <!-- svelte-ignore a11y_autofocus -->
              <input
                class="exp-input"
                autofocus
                bind:value={giris.deger}
                onkeydown={(e) => {
                  if (e.key === 'Enter') void girisOnayla();
                  if (e.key === 'Escape') giris = null;
                }}
                onblur={girisOnayla}
              />
            </div>
          {/if}
          {#each icerik[rel] ?? [] as e (e.rel)}
            {@const r = rozet(e)}
            <div class="exp-row" style="padding-left:{derinlik * 12 + 4}px">
              {#if giris && giris.mod === 'ad' && giris.hedefRel === e.rel}
                <span class="exp-caret"></span>
                <span class="exp-icon">{e.is_dir ? '📁' : '🗎'}</span>
                <!-- svelte-ignore a11y_autofocus -->
                <input
                  class="exp-input"
                  autofocus
                  bind:value={giris.deger}
                  onkeydown={(ev) => {
                    if (ev.key === 'Enter') void girisOnayla();
                    if (ev.key === 'Escape') giris = null;
                  }}
                  onblur={girisOnayla}
                />
              {:else if e.is_dir}
                <button class="exp-item" onclick={() => klasorTikla(e.rel)} oncontextmenu={(ev) => menuAc(ev, e)}>
                  <span class="exp-caret">{acik.has(e.rel) ? '▾' : '▸'}</span>
                  <span class="exp-icon">📁</span>
                  <span class="exp-name">{e.name}</span>
                </button>
              {:else}
                <button
                  class="exp-item"
                  class:openable={acilabilir(e)}
                  onclick={() => dosyaAc(e)}
                  oncontextmenu={(ev) => menuAc(ev, e)}
                  title={acilabilir(e) ? 'Editörde aç' : 'Bu tür editörde açılamaz'}
                >
                  <span class="exp-caret"></span>
                  <span class="exp-icon">{acilabilir(e) ? '📄' : '·'}</span>
                  <span class="exp-name">{e.name}</span>
                  {#if r}<span class="exp-badge" style="color:{r.renk}" title={r.baslik}>{r.harf}</span>{/if}
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
      {/if}
    </div>
  {/if}
</div>

<!-- Sağ tık menüsü -->
{#if menu}
  <div class="ctx" style="left:{menu.x}px; top:{menu.y}px" role="menu" tabindex="-1">
    {#if menu.hedef && !menu.hedef.is_dir}
      <button onclick={() => menu?.hedef && dosyaAc(menu.hedef)} disabled={!acilabilir(menu.hedef)}>Editörde aç</button>
      <div class="ctx-sep"></div>
    {/if}
    <button onclick={() => yeniBaslat('dosya', menu?.hedef ?? null)}>Yeni dosya</button>
    <button onclick={() => yeniBaslat('klasor', menu?.hedef ?? null)}>Yeni klasör</button>
    {#if menu.hedef}
      <div class="ctx-sep"></div>
      <button onclick={() => menu?.hedef && adDegistirBaslat(menu.hedef)}>Ad değiştir</button>
      <button onclick={() => menu?.hedef && kopyala(menu.hedef, false)}>Kopyala</button>
      <button onclick={() => menu?.hedef && kopyala(menu.hedef, true)}>Kes</button>
    {/if}
    <button onclick={() => yapistir(menu?.hedef ?? null)} disabled={!pano}>
      Yapıştır{pano ? ` (${pano.rel.slice(pano.rel.lastIndexOf('/') + 1)})` : ''}
    </button>
    {#if menu.hedef}
      <div class="ctx-sep"></div>
      <button onclick={() => menu?.hedef && yoluKopyala(menu.hedef)}>Yolu kopyala</button>
      <button class="tehlike" onclick={() => menu?.hedef && sil(menu.hedef)}>Sil…</button>
    {/if}
  </div>
{/if}

<style>
  .explorer { display: flex; flex-direction: column; height: 100%; min-height: 0; font-size: 12px; }
  .exp-bar { display: flex; align-items: center; gap: 3px; padding: 4px 6px; flex-shrink: 0; }
  .exp-spacer { flex: 1; }
  .exp-btn {
    padding: 2px 7px; border: 1px solid #ccc; background: #f7f7f7; border-radius: 4px;
    cursor: pointer; font-size: 11px; line-height: 1.4;
  }
  .exp-btn:disabled { opacity: 0.45; cursor: default; }
  .exp-btn.ghost { border: none; background: none; color: #999; }
  .exp-count {
    font-size: 10px; background: #fff8e1; border: 1px solid #ffe082; color: #8a6d3b;
    border-radius: 8px; padding: 0 6px; white-space: nowrap;
  }
  .exp-search { display: flex; align-items: center; gap: 2px; padding: 0 6px 4px; flex-shrink: 0; }
  .exp-search input {
    flex: 1; min-width: 0; padding: 3px 7px; border: 1px solid #d5d5d5; border-radius: 4px;
    font: inherit; font-size: 11px;
  }
  .exp-root {
    font-size: 10px; color: #777; padding: 0 8px 4px; overflow: hidden;
    text-overflow: ellipsis; white-space: nowrap; flex-shrink: 0;
  }
  .exp-empty, .exp-err { font-size: 11px; color: #777; padding: 8px; margin: 0; line-height: 1.5; }
  .exp-err { color: #b3261e; }
  .exp-tree { flex: 1; min-height: 0; overflow: auto; padding-bottom: 6px; }
  .exp-sonuc-basligi { font-size: 10px; color: #888; padding: 2px 8px; }
  .exp-row { display: flex; align-items: center; }
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
  .exp-size, .exp-rel { font-size: 10px; color: #aaa; flex-shrink: 0; }
  .exp-input {
    flex: 1; min-width: 0; padding: 1px 5px; border: 1px solid #1a73e8; border-radius: 3px;
    font: inherit; font-size: 12px;
  }

  .ctx {
    position: fixed; z-index: 80; min-width: 170px; padding: 4px;
    background: #fff; border: 1px solid #d5d5d5; border-radius: 6px;
    box-shadow: 0 6px 24px rgba(0, 0, 0, 0.18); display: flex; flex-direction: column;
  }
  .ctx button {
    text-align: left; padding: 5px 10px; border: none; background: none; cursor: pointer;
    font: inherit; font-size: 12px; border-radius: 4px; color: #222;
  }
  .ctx button:hover:not(:disabled) { background: #eef2ff; }
  .ctx button:disabled { color: #bbb; cursor: default; }
  .ctx button.tehlike { color: #b3261e; }
  .ctx button.tehlike:hover { background: #fdecea; }
  .ctx-sep { height: 1px; background: #eee; margin: 3px 0; }

  :global(html.dark) .exp-item { color: #ddd; }
  :global(html.dark) .exp-item:hover { background: #2b2b40; }
  :global(html.dark) .exp-btn { background: #333; color: #ddd; border-color: #555; }
  :global(html.dark) .exp-root, :global(html.dark) .exp-empty { color: #999; }
  :global(html.dark) .exp-search input { background: #2a2a2a; color: #ddd; border-color: #555; }
  :global(html.dark) .ctx { background: #2a2a2a; border-color: #555; }
  :global(html.dark) .ctx button { color: #ddd; }
  :global(html.dark) .ctx button:hover:not(:disabled) { background: #3a3a55; }
</style>
