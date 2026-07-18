<!--
  ClaudePanel — Klasör Ajanı'nın **Claude Code motoru** modu.

  Gerçek `claude` ikilisi seçtiğin klasörde sürülür; her araç çağrısı bir PreToolUse
  hook üzerinden onay kuyruğundan geçer. Tasarım Claude Code arayüzüne benzer:
  akan araç kartları (IN/OUT açılır, zaman+süre damgalı) + alt giriş barı
  (durdur · slash · ekle · otomatik düzenle).

  Konuşma SÜRER: `sessionId` sonraki mesajda --resume ile geçer, model önceki turu hatırlar.
-->
<script lang="ts">
  import {
    claude,
    runClaude,
    cancelClaude,
    resolveFirst,
    setClaudeRoot,
    resetClaudeChat,
    refreshEngine,
    installEngine,
    loadClaudeSession,
  } from '$lib/claude-session.svelte';
  import {
    listClaudeSessions,
    deleteClaudeSession,
    type ClaudeHistorySession,
  } from '$lib/claude-history.svelte';
  import { settings } from '$lib/settings.svelte';
  import { pickFolder } from '$lib/batch';

  /** Geçmiş paneli açık mı (daraltılabilir; varsayılan kapalı). */
  let historyOpen = $state(false);
  /** Bu kök için kayıtlı oturumlar (en yeni üstte). */
  const gecmis = $derived(listClaudeSessions(claude.root));

  function secOturum(s: ClaudeHistorySession): void {
    loadClaudeSession(s);
    historyOpen = false;
  }
  function histZaman(ts: number): string {
    return new Date(ts).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
  }

  // Bir dosya yolunu ana editör sekmesinde açar (+page.svelte'in openPaths'i).
  // Modelin `open_in_editor` MCP çağrısı zaten olayla otomatik açar; bu düğme,
  // model aracı çağırmadan yalnız Write/Edit yaptığında manuel köprüdür.
  let { onOpenInEditor }: { onOpenInEditor?: (path: string) => void } = $props();

  let input = $state('');
  let feedEl: HTMLDivElement | null = $state(null);
  /** Açık (IN/OUT görünür) araç kartlarının feed index'leri. */
  let acik = $state(new Set<number>());

  $effect(() => {
    if (!claude.engine) void refreshEngine(settings.claudeUseSystemBinary);
  });

  // Otomatik kaydırma — "dibe yapış" deseni: kullanıcı elle yukarı kaydırana kadar
  // en altta kal. Eski hâli "zaten dibe yakınsa kaydır" idi; ilk yükte scrollTop=0
  // olduğu için hiç dibe inmiyordu (içerik en üstte kalıp altı kesiliyordu).
  let dibeYapis = $state(true);
  function feedKaydir(): void {
    const el = feedEl;
    if (!el) return;
    dibeYapis = el.scrollHeight - el.scrollTop - el.clientHeight < 60;
  }
  $effect(() => {
    void claude.feed.length;
    void claude.feed.at(-1)?.tool?.out; // araç sonucu gelince de dibe in
    void claude.queue.length;
    const el = feedEl;
    if (!el || !dibeYapis) return;
    queueMicrotask(() => (el.scrollTop = el.scrollHeight));
  });

  async function chooseFolder(): Promise<void> {
    const f = await pickFolder();
    if (f) setClaudeRoot(f);
  }

  async function send(): Promise<void> {
    const text = input.trim();
    if (!text || claude.running || !claude.root) return;
    input = '';
    await runClaude(text, settings.claudeUseSystemBinary);
  }

  function onKey(e: KeyboardEvent): void {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      void send();
    }
  }

  function toggleCard(i: number): void {
    const s = new Set(acik);
    s.has(i) ? s.delete(i) : s.add(i);
    acik = s;
  }

  function saat(ts: number): string {
    return new Date(ts).toLocaleTimeString('tr-TR', { hour12: false });
  }
  function tamTarih(ts: number): string {
    return new Date(ts).toLocaleString('tr-TR');
  }
  function sure(ms?: number): string {
    if (ms == null) return '';
    return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} sn`;
  }
  function mb(n: number): string {
    return `${(n / 1024 / 1024).toFixed(1)} MB`;
  }

  const hazir = $derived(claude.engine?.ready === true);
</script>

<div class="claude">
  <!-- Motor durumu / kurulum -->
  {#if !hazir}
    <div class="engine-box">
      {#if claude.install}
        <div class="engine-title">Motor kuruluyor — {claude.install.phase}</div>
        {#if claude.install.total > 0}
          <progress value={claude.install.downloaded} max={claude.install.total}></progress>
          <span class="engine-sub">{mb(claude.install.downloaded)} / {mb(claude.install.total)}</span>
        {/if}
      {:else}
        <div class="engine-title">Claude Code motoru hazır değil</div>
        <p class="engine-sub">{claude.engine?.reason ?? 'Motor durumu okunuyor…'}</p>
        {#if !settings.claudeUseSystemBinary}
          <button class="install" onclick={() => installEngine(settings.claudeUseSystemBinary)}>
            Motoru indir ({claude.engine?.pinned_version ?? '…'}) — ~66 MB
          </button>
        {/if}
      {/if}
    </div>
  {/if}

  <!-- Klasör + oturum -->
  <div class="claude-root">
    <button class="pick" onclick={chooseFolder}>📁 Klasör</button>
    {#if claude.root}
      <code class="root-path" title={claude.root}>{claude.root}</code>
      <button class="reset" onclick={resetClaudeChat} disabled={claude.running} title="Yeni oturum">
        ＋ Yeni
      </button>
    {/if}
    {#if hazir}
      <span class="engine-badge" title={claude.engine?.path ?? ''}>
        {claude.engine?.source === 'system' ? 'sistem' : 'motor'} {claude.engine?.version ?? ''}
      </span>
    {/if}
  </div>

  <!-- Oturum geçmişi (kök başına, daraltılabilir; varsayılan kapalı) -->
  {#if claude.root}
    <div class="history">
      <button class="hist-toggle" onclick={() => (historyOpen = !historyOpen)}>
        <span class="caret">{historyOpen ? '▾' : '▸'}</span> 🕘 Geçmiş
        {#if gecmis.length > 0}<span class="hist-count">{gecmis.length}</span>{/if}
      </button>
      {#if historyOpen}
        <div class="hist-list">
          {#each gecmis as s (s.id)}
            <div class="hist-item" class:active={claude.sessionId === s.id}>
              <button class="hist-load" onclick={() => secOturum(s)} disabled={claude.running} title={s.baslik}>
                <span class="hist-title">{s.baslik}</span>
                <span class="hist-time">{histZaman(s.updatedAt)}</span>
              </button>
              <button class="hist-del" onclick={() => deleteClaudeSession(s.id)} title="Sil">🗑</button>
            </div>
          {:else}
            <p class="hist-empty">Bu klasörde kayıtlı oturum yok.</p>
          {/each}
        </div>
      {/if}
    </div>
  {/if}

  <!-- Bilgilendirme (dürüst — ders 15/17) -->
  {#if !claude.root}
    <div class="claude-hint">
      <p>
        Gerçek <b>Claude Code</b> seçtiğin klasörde çalışır: dosyaları okur, düzenler, oluşturur,
        komut çalıştırır. <b>Her işlem senin onayından geçer.</b>
      </p>
      <ul>
        <li>✅ Dosya yazma/düzenleme klasör <b>dışına çıkamaz</b> — onay kapısında zorlanır (her platformda).</li>
        {#if claude.engine?.sandbox}
          <li>✅ Komutlar (bash) da sandbox ile klasöre kilitli.</li>
        {:else}
          <li>⚠️ Bu platformda komut (bash) sandbox'ı <b>yok</b> — komutlar klasör dışına çıkabilir; tek koruma onay kapısı.</li>
        {/if}
        <li>⚠️ Diskteki <b>diğer dosyaları okuyabilir</b> — okuma engellenmiyor.</li>
        <li>📂 Ürettiği .xslt/.xml dosyalarını <b>editörde açabilir</b> (senin de "Editörde aç" düğmen var).</li>
        <li>🔑 Kimlik: Claude.ai aboneliğin veya Anthropic API anahtarın.</li>
      </ul>
      <p class="hint-foot">Başlamak için bir klasör seç.</p>
    </div>
  {/if}

  <!-- Akış -->
  <div class="feed" bind:this={feedEl} onscroll={feedKaydir}>
    {#each claude.feed as item, i (i)}
      {#if item.kind === 'user'}
        <div class="row user">
          <div class="msg user-msg">{item.text}</div>
          <span class="ts" title={tamTarih(item.ts)}>{saat(item.ts)}</span>
        </div>
      {:else if item.kind === 'assistant'}
        <div class="row">
          <div class="msg assistant">{item.text}</div>
          <span class="ts" title={tamTarih(item.ts)}>{saat(item.ts)}</span>
        </div>
      {:else if item.kind === 'info'}
        <div class="row">
          <div class="msg info" class:thinking={item.text === '__thinking__'}>
            {item.text === '__thinking__' ? 'Düşünüyor…' : item.text}
          </div>
          <span class="ts" title={tamTarih(item.ts)}>{saat(item.ts)}</span>
        </div>
      {:else if item.kind === 'tool' && item.tool}
        <div class="tool-card" class:err={item.tool.ok === false}>
          <button class="tool-head" onclick={() => toggleCard(i)}>
            <span class="caret">{acik.has(i) ? '▾' : '▸'}</span>
            <span class="tname">{item.tool.name}</span>
            <span class="tsum" title={item.tool.summary}>{item.tool.summary}</span>
            <span class="tstat">{item.tool.ok === false ? '✗' : item.tool.ok ? '✓' : '…'}</span>
            {#if item.durationMs != null}<span class="tdur">{sure(item.durationMs)}</span>{/if}
            <span class="ts" title={tamTarih(item.ts)}>{saat(item.ts)}</span>
          </button>
          {#if item.tool.path}
            {@const tpath = item.tool.path}
            <div class="tool-actions">
              <button class="open-editor" onclick={() => onOpenInEditor?.(tpath)} title={tpath}>
                📂 Editörde aç
              </button>
            </div>
          {/if}
          {#if acik.has(i)}
            {#if item.tool.in}
              <div class="io"><span class="io-lbl">IN</span><pre>{item.tool.in}</pre></div>
            {/if}
            {#if item.tool.out}
              <div class="io"><span class="io-lbl out">OUT</span><pre>{item.tool.out}</pre></div>
            {/if}
          {/if}
        </div>
      {:else if item.kind === 'error'}
        <div class="row">
          <div class="msg err-msg">{item.text}</div>
          <span class="ts" title={tamTarih(item.ts)}>{saat(item.ts)}</span>
        </div>
      {/if}
    {/each}
    {#if claude.running && claude.queue.length === 0 && claude.feed.at(-1)?.text !== '__thinking__'}
      <div class="msg thinking">Çalışıyor…</div>
    {/if}
  </div>

  {#if claude.error}
    <div class="claude-err">{claude.error}</div>
  {/if}

  <!-- Alt giriş barı (Claude Code tarzı) -->
  <div class="composer">
    <textarea
      bind:value={input}
      onkeydown={onKey}
      placeholder={!hazir
        ? 'Önce motoru kur'
        : claude.root
          ? claude.running
            ? 'Çalışıyor… (durdurabilirsin)'
            : 'Ne yapmamı istersin? (⌘/Ctrl+Enter)'
          : 'Önce bir klasör seç'}
      disabled={!claude.root || !hazir}
      rows="2"
    ></textarea>
    <div class="composer-bar">
      <button class="cbtn" title="Klasör seç / ekle" onclick={chooseFolder}>＋</button>
      <button class="cbtn" title="Slash komutu (/compact, /clear…)" onclick={() => (input = '/' + input)}>
        /
      </button>
      <label class="auto" title="Açıkken tüm araçlar otomatik onaylanır (mac/Linux'ta sandbox korur)">
        <input type="checkbox" bind:checked={claude.autoApprove} />
        <span>⟨⟩ Otomatik düzenle</span>
      </label>
      <span class="spacer"></span>
      {#if claude.running}
        <button class="stop" onclick={cancelClaude} title="Durdur">■ Durdur</button>
      {:else}
        <button class="send" onclick={send} disabled={!claude.root || !hazir || !input.trim()}>
          Gönder
        </button>
      {/if}
    </div>
  </div>
</div>

<!-- Onay modalı — KUYRUĞUN İLKİ -->
{#if claude.queue.length > 0}
  {@const p = claude.queue[0]}
  <div class="approve-overlay">
    <div class="approve">
      <div class="approve-title">
        İşlem onayı
        {#if claude.queue.length > 1}<span class="qcount">+{claude.queue.length - 1} bekliyor</span>{/if}
      </div>
      <div class="approve-tool"><span class="tname">{p.arac}</span> — {p.ozet}</div>
      {#if p.arac === 'Bash'}
        <pre class="approve-body cmd">{String(p.girdi.command ?? '')}</pre>
        {#if !claude.engine?.sandbox}
          <p class="bash-warn">
            ⚠️ Bu platformda sandbox yok: komut <b>klasör dışına çıkabilir</b>. Gerçek koruma bu onaydır.
          </p>
        {/if}
      {:else if p.arac === 'Write' || p.arac === 'Edit'}
        <pre class="approve-body">{JSON.stringify(p.girdi, null, 2).slice(0, 2000)}</pre>
      {/if}
      <div class="approve-actions">
        <button class="ok" onclick={() => resolveFirst('allow')}>Onayla</button>
        <button class="always" onclick={() => resolveFirst('always')}>Bu aracı hep izin ver</button>
        <button class="deny" onclick={() => resolveFirst('deny')}>Reddet</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .claude {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    font-size: 13px;
  }

  .engine-box {
    padding: 10px 12px;
    margin-bottom: 6px;
    background: #fff8e1;
    border: 1px solid #ffe082;
    border-radius: 6px;
  }
  .engine-title { font-weight: 600; margin-bottom: 4px; }
  .engine-sub { color: #666; font-size: 12px; margin: 4px 0; }
  .engine-box progress { width: 100%; height: 6px; }
  .install {
    margin-top: 6px; padding: 5px 10px; border: 1px solid #c9a227;
    background: #fff; border-radius: 5px; cursor: pointer;
  }

  .claude-root {
    display: flex; align-items: center; gap: 6px; padding: 6px 0; flex-wrap: wrap;
  }
  .pick, .reset {
    padding: 4px 9px; border: 1px solid #ccc; background: #f7f7f7;
    border-radius: 5px; cursor: pointer; font-size: 12px; white-space: nowrap;
  }
  .root-path {
    flex: 1; min-width: 60px; overflow: hidden; text-overflow: ellipsis;
    white-space: nowrap; font-size: 11px; color: #555;
  }
  .engine-badge {
    font-size: 11px; color: #2e7d32; border: 1px solid #a5d6a7;
    border-radius: 4px; padding: 1px 5px; white-space: nowrap;
  }

  .history { margin: 2px 0 4px; }
  .hist-toggle {
    display: flex; align-items: center; gap: 5px; width: 100%; padding: 3px 4px;
    background: none; border: none; cursor: pointer; font: inherit; font-size: 12px;
    color: #555; text-align: left;
  }
  .hist-count {
    font-size: 10px; background: #e0e6f0; color: #445; border-radius: 8px; padding: 0 6px;
  }
  .hist-list {
    display: flex; flex-direction: column; gap: 2px; max-height: 160px; overflow-y: auto;
    padding: 2px 0 2px 14px;
  }
  .hist-item { display: flex; align-items: center; gap: 4px; }
  .hist-item.active .hist-load { background: #e8f0fe; border-color: #a5c8ff; }
  .hist-load {
    flex: 1; min-width: 0; display: flex; flex-direction: column; align-items: flex-start;
    gap: 1px; padding: 4px 7px; border: 1px solid #e2e2e2; background: #fafafa;
    border-radius: 5px; cursor: pointer; font: inherit; text-align: left;
  }
  .hist-load:disabled { cursor: default; opacity: 0.6; }
  .hist-title {
    font-size: 12px; color: #333; max-width: 100%; overflow: hidden;
    text-overflow: ellipsis; white-space: nowrap;
  }
  .hist-time { font-size: 10px; color: #999; }
  .hist-del {
    flex-shrink: 0; border: none; background: none; cursor: pointer; font-size: 12px;
    opacity: 0.5; padding: 2px 4px;
  }
  .hist-del:hover { opacity: 1; }
  .hist-empty { font-size: 11px; color: #999; padding: 4px 7px; margin: 0; }

  .claude-hint {
    padding: 10px 12px; background: #f4f8ff; border: 1px solid #cfe0ff;
    border-radius: 6px; color: #333; line-height: 1.5;
  }
  .claude-hint ul { margin: 8px 0; padding-left: 18px; }
  .claude-hint li { margin: 3px 0; }
  .hint-foot { margin: 6px 0 0; color: #666; }

  .feed {
    flex: 1; min-height: 0; overflow-y: auto; overflow-x: hidden; display: flex;
    flex-direction: column; gap: 5px; padding: 6px 2px; scroll-behavior: smooth;
  }
  .feed > * { max-width: 100%; }
  .row { display: flex; align-items: flex-start; gap: 6px; }
  .row.user { flex-direction: row-reverse; }
  .msg { padding: 6px 9px; border-radius: 6px; white-space: pre-wrap; word-break: break-word; flex: 1; }
  .user-msg { background: #e8f0fe; max-width: 85%; flex: 0 1 auto; }
  .assistant { background: #f5f5f5; }
  .info { background: #fff8e1; font-size: 12px; color: #6d4c41; }
  .info.thinking, .msg.thinking { color: #888; font-style: italic; background: transparent; }
  .ts { font-size: 10px; color: #aaa; white-space: nowrap; padding-top: 6px; }

  .tool-card { border: 1px solid #e0e6e0; border-radius: 6px; overflow: hidden; background: #f7faf7; }
  .tool-card.err { background: #fdecea; border-color: #f5c6c2; }
  .tool-head {
    display: flex; align-items: center; gap: 6px; width: 100%; padding: 5px 8px;
    background: none; border: none; cursor: pointer; font: inherit; text-align: left;
    min-width: 0;
  }
  .caret { color: #888; width: 10px; flex-shrink: 0; }
  .tname { font-weight: 600; white-space: nowrap; flex-shrink: 0; max-width: 45%; overflow: hidden; text-overflow: ellipsis; }
  .tsum { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #555; }
  .tstat { white-space: nowrap; }
  .tdur { font-size: 10px; color: #888; white-space: nowrap; }
  .tool-actions { padding: 2px 8px 6px; }
  .open-editor {
    font-size: 11px; padding: 3px 8px; border: 1px solid #a5c8ff; background: #eef4ff;
    color: #1a56c4; border-radius: 5px; cursor: pointer; max-width: 100%;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
  }
  .open-editor:hover { background: #e0ecff; }
  .io { border-top: 1px solid #e0e6e0; padding: 4px 8px; }
  .io-lbl {
    display: inline-block; font-size: 10px; font-weight: 700; color: #888;
    background: #eee; border-radius: 3px; padding: 0 4px; margin-bottom: 2px;
  }
  .io-lbl.out { color: #1a5; background: #e3f5ea; }
  .io pre {
    margin: 2px 0 0; font-size: 11px; white-space: pre-wrap; word-break: break-word;
    max-height: 260px; overflow: auto; background: #fff; padding: 5px; border-radius: 4px;
  }
  .err-msg, .claude-err {
    background: #fdecea; color: #b3261e; padding: 6px 9px; border-radius: 6px;
  }

  .composer {
    border: 1px solid #ccc; border-radius: 8px; padding: 6px; background: #fff; margin-top: 4px;
  }
  .composer textarea {
    width: 100%; resize: none; font: inherit; border: none; outline: none; padding: 2px 4px;
    background: transparent;
  }
  .composer-bar { display: flex; align-items: center; gap: 6px; margin-top: 4px; }
  .cbtn {
    width: 26px; height: 26px; border: 1px solid #ddd; background: #f7f7f7; border-radius: 6px;
    cursor: pointer; font-size: 14px; line-height: 1; color: #555;
  }
  .auto {
    display: flex; align-items: center; gap: 4px; font-size: 11px; color: #666; cursor: pointer;
    user-select: none;
  }
  .spacer { flex: 1; }
  .send {
    padding: 5px 14px; border: none; background: #1a73e8; color: #fff; border-radius: 6px; cursor: pointer;
  }
  .send:disabled { background: #b0c4de; cursor: default; }
  .stop {
    padding: 5px 14px; border: 1px solid #e0a; background: #fdecf4; color: #b3268a;
    border-radius: 6px; cursor: pointer; font-weight: 600;
  }

  .approve-overlay {
    position: fixed; inset: 0; background: rgba(0, 0, 0, 0.35);
    display: flex; align-items: center; justify-content: center; z-index: 60;
  }
  .approve {
    background: #fff; border-radius: 8px; padding: 16px; width: min(620px, 92vw);
    max-height: 80vh; overflow-y: auto; box-shadow: 0 8px 30px rgba(0, 0, 0, 0.25);
  }
  .approve-title { font-weight: 700; margin-bottom: 8px; display: flex; align-items: center; gap: 8px; }
  .qcount {
    font-weight: 400; font-size: 12px; color: #8a6d3b; background: #fff8e1;
    border: 1px solid #ffe082; border-radius: 10px; padding: 1px 7px;
  }
  .approve-tool { margin-bottom: 8px; }
  .approve-body {
    background: #f6f6f6; padding: 8px; border-radius: 5px; font-size: 12px;
    max-height: 240px; overflow: auto; white-space: pre-wrap; word-break: break-word;
  }
  .approve-body.cmd { background: #1e1e1e; color: #eee; }
  .bash-warn {
    background: #fff8e1; border: 1px solid #ffe082; border-radius: 5px;
    padding: 7px 9px; font-size: 12px; color: #6d4c41;
  }
  .approve-actions { display: flex; gap: 8px; margin-top: 12px; flex-wrap: wrap; }
  .approve-actions button {
    padding: 6px 12px; border-radius: 5px; border: 1px solid #ccc; background: #f7f7f7; cursor: pointer;
  }
  .approve-actions .ok { background: #1a73e8; color: #fff; border-color: #1a73e8; }
  .approve-actions .deny { background: #fdecea; color: #b3261e; border-color: #f5c6c2; }

  :global(html.dark) .claude-root,
  :global(html.dark) .assistant { color: #ddd; }
  :global(html.dark) .assistant { background: #2a2a2a; }
  :global(html.dark) .user-msg { background: #1e3a5f; color: #ddd; }
  :global(html.dark) .tool-card { background: #23301f; border-color: #35452f; }
  :global(html.dark) .tool-head, :global(html.dark) .tname { color: #ddd; }
  :global(html.dark) .io pre { background: #1a1a1a; color: #ddd; }
  :global(html.dark) .open-editor { background: #1e2f4a; border-color: #2f4159; color: #9dc0ff; }
  :global(html.dark) .hist-toggle { color: #bbb; }
  :global(html.dark) .hist-count { background: #33415a; color: #cdd; }
  :global(html.dark) .hist-load { background: #2a2a2a; border-color: #444; }
  :global(html.dark) .hist-title { color: #ddd; }
  :global(html.dark) .hist-item.active .hist-load { background: #1e3a5f; border-color: #2f4159; }
  :global(html.dark) .claude-hint { background: #1e2a3a; border-color: #2f4159; color: #ddd; }
  :global(html.dark) .engine-box { background: #33291a; border-color: #5a4a2a; color: #ddd; }
  :global(html.dark) .composer { background: #222; border-color: #444; }
  :global(html.dark) .composer textarea { color: #ddd; }
  :global(html.dark) .approve { background: #222; color: #ddd; }
  :global(html.dark) .approve-body { background: #2a2a2a; }
  :global(html.dark) .pick,
  :global(html.dark) .reset,
  :global(html.dark) .cbtn,
  :global(html.dark) .install,
  :global(html.dark) .approve-actions button { background: #333; color: #ddd; border-color: #555; }
</style>
