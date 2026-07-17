<!--
  ClaudePanel — Klasör Ajanı'nın **Claude Code motoru** modu.

  "Klasör Ajanı" (BYOK) modundan farkı: araç döngüsü uygulamada dönmez; `claude`
  ikilisi kendi araçlarını çalıştırır, biz onay kapısıyız (PreToolUse hook).

  Onaylar KUYRUKTA gösterilir: aynı anda 3 onay uçuşta olabildiği ÖLÇÜLDÜ —
  tek modal varsayımı bir aracı sessizce cevapsız bırakırdı.
-->
<script lang="ts">
  import {
    claude,
    runClaude,
    resolveFirst,
    setClaudeRoot,
    resetClaudeChat,
    refreshEngine,
    installEngine,
  } from '$lib/claude-session.svelte';
  import { settings } from '$lib/settings.svelte';
  import { pickFolder } from '$lib/batch';

  let input = $state('');

  // Motor durumunu yokla — panel açılınca VE ayar değişince.
  //
  // `if (!claude.engine)` ile korumak cazipti ama YANLIŞ olurdu: kullanıcı
  // "sistemdeki sürümü kullan"ı açtığında durum tazelenmez, ekranda eski motor
  // yazmaya devam ederdi (hata vermeden, sadece yanlış). Etki `claude.engine`'i
  // OKUMADIĞI için yazması döngü kurmaz.
  $effect(() => {
    void refreshEngine(settings.claudeUseSystemBinary);
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
          <span class="engine-sub">
            {mb(claude.install.downloaded)} / {mb(claude.install.total)}
          </span>
        {/if}
      {:else}
        <div class="engine-title">Claude Code motoru hazır değil</div>
        <p class="engine-sub">
          {claude.engine?.reason ?? 'Motor durumu okunuyor…'}
        </p>
        {#if !settings.claudeUseSystemBinary}
          <button class="install" onclick={() => installEngine(settings.claudeUseSystemBinary)}>
            Motoru indir ({claude.engine?.pinned_version ?? '…'}) — ~66 MB
          </button>
        {/if}
      {/if}
    </div>
  {/if}

  <!-- Klasör -->
  <div class="claude-root">
    <button class="pick" onclick={chooseFolder}>📁 Çalışma klasörü seç…</button>
    {#if claude.root}
      <code class="root-path" title={claude.root}>{claude.root}</code>
      <button class="reset" onclick={resetClaudeChat} disabled={claude.running}>
        Sohbeti sıfırla
      </button>
    {/if}
    {#if hazir}
      <span class="engine-badge" title={claude.engine?.path ?? ''}>
        {claude.engine?.source === 'system' ? 'sistem' : 'motor'}
        {claude.engine?.version ?? ''}
      </span>
    {/if}
  </div>

  <!--
    BİLGİLENDİRME — dürüst olmak zorunda (CLAUDE.md ders 15/17).
    "Klasör dışına yazamaz" doğru (sandbox varsa). "Okuyamaz" YANLIŞ olurdu:
    sandbox okumayı engellemiyor, ölçüldü. Yazmadığımız cümle bilinçli.
  -->
  {#if !claude.root}
    <div class="claude-hint">
      <p>
        Bu modda gerçek <b>Claude Code</b> seçtiğin klasörde çalışır: dosyaları okur,
        düzenler, oluşturur ve komut çalıştırır. <b>Her işlem senin onayından geçer</b> —
        onaylamadığın hiçbir şey çalışmaz.
      </p>
      <ul>
        {#if claude.engine?.sandbox}
          <li>✅ Seçtiğin klasörün <b>dışına yazamaz</b> (işletim sistemi sandbox'ı).</li>
        {:else}
          <li>
            ⚠️ Bu platformda sandbox <b>yok</b> — komutlar klasör dışına çıkabilir.
            Tek koruma <b>onay kapısı</b>: komutu çalışmadan önce görürsün.
          </li>
        {/if}
        <li>
          ⚠️ Diskteki <b>diğer dosyaları okuyabilir</b> — sandbox okumayı engellemiyor.
        </li>
        <li>🔑 Kimlik: Claude.ai aboneliğin veya Anthropic API anahtarın.</li>
      </ul>
      <p class="hint-foot">Başlamak için bir klasör seç.</p>
    </div>
  {/if}

  <!-- Olay akışı -->
  <div class="feed">
    {#each claude.feed as item, i (i)}
      {#if item.kind === 'user'}
        <div class="msg user">{item.text}</div>
      {:else if item.kind === 'assistant'}
        <div class="msg assistant">{item.text}</div>
      {:else if item.kind === 'info'}
        <div class="msg info">{item.text}</div>
      {:else if item.kind === 'tool'}
        <div class="msg tool" class:err={item.tool && item.tool.ok === false}>
          <span class="tname">{item.tool?.name}</span>
          <span class="tsum">{item.tool?.summary}</span>
          <span class="tstat">{item.tool?.ok === false ? '✗' : item.tool?.ok ? '✓' : '…'}</span>
        </div>
      {:else if item.kind === 'error'}
        <div class="msg err-msg">{item.text}</div>
      {/if}
    {/each}
    {#if claude.running && claude.queue.length === 0}
      <div class="msg thinking">Çalışıyor…</div>
    {/if}
  </div>

  {#if claude.error}
    <div class="claude-err">{claude.error}</div>
  {/if}

  <!-- Giriş -->
  <div class="claude-input">
    <textarea
      bind:value={input}
      onkeydown={onKey}
      placeholder={!hazir
        ? 'Önce motoru kur'
        : claude.root
          ? 'Ne yapmamı istersin? (⌘/Ctrl+Enter ile gönder)'
          : 'Önce bir klasör seç'}
      disabled={!claude.root || claude.running || !hazir}
      rows="2"
    ></textarea>
    <button
      class="send"
      onclick={send}
      disabled={!claude.root || claude.running || !hazir || !input.trim()}
    >
      {claude.running ? '…' : 'Gönder'}
    </button>
  </div>
</div>

<!--
  Onay modalı — KUYRUĞUN İLKİ. Arkada bekleyen varsa sayısı gösterilir; yoksa
  kullanıcı kaç işlem beklediğini bilemez ve modal "takılmış" görünürdü.
-->
{#if claude.queue.length > 0}
  {@const p = claude.queue[0]}
  <div class="approve-overlay">
    <div class="approve">
      <div class="approve-title">
        İşlem onayı
        {#if claude.queue.length > 1}
          <span class="qcount">+{claude.queue.length - 1} bekliyor</span>
        {/if}
      </div>
      <div class="approve-tool"><span class="tname">{p.arac}</span> — {p.ozet}</div>
      {#if p.arac === 'Bash'}
        <pre class="approve-body cmd">{String(p.girdi.command ?? '')}</pre>
        {#if !claude.engine?.sandbox}
          <p class="bash-warn">
            ⚠️ Bu platformda sandbox yok: komut <b>klasör dışına çıkabilir</b>.
            Gerçek koruma bu onaydır — ne yaptığını anladığından emin ol.
          </p>
        {/if}
      {:else if p.arac === 'Write' || p.arac === 'Edit'}
        <pre class="approve-body">{JSON.stringify(p.girdi, null, 2).slice(0, 2000)}</pre>
      {/if}
      <div class="approve-actions">
        <button class="ok" onclick={() => resolveFirst('allow')}>Onayla</button>
        <button class="always" onclick={() => resolveFirst('always')}>
          Bu aracı hep izin ver
        </button>
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
  .engine-title {
    font-weight: 600;
    margin-bottom: 4px;
  }
  .engine-sub {
    color: #666;
    font-size: 12px;
    margin: 4px 0;
  }
  .engine-box progress {
    width: 100%;
    height: 6px;
  }
  .install {
    margin-top: 6px;
    padding: 5px 10px;
    border: 1px solid #c9a227;
    background: #fff;
    border-radius: 5px;
    cursor: pointer;
  }

  .claude-root {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 6px 0;
    flex-wrap: wrap;
  }
  .pick,
  .reset {
    padding: 4px 9px;
    border: 1px solid #ccc;
    background: #f7f7f7;
    border-radius: 5px;
    cursor: pointer;
    font-size: 12px;
  }
  .root-path {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 11px;
    color: #555;
  }
  .engine-badge {
    font-size: 11px;
    color: #2e7d32;
    border: 1px solid #a5d6a7;
    border-radius: 4px;
    padding: 1px 5px;
  }

  .claude-hint {
    padding: 10px 12px;
    background: #f4f8ff;
    border: 1px solid #cfe0ff;
    border-radius: 6px;
    color: #333;
    line-height: 1.5;
  }
  .claude-hint ul {
    margin: 8px 0;
    padding-left: 18px;
  }
  .claude-hint li {
    margin: 3px 0;
  }
  .hint-foot {
    margin: 6px 0 0;
    color: #666;
  }

  .feed {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 6px 0;
  }
  .msg {
    padding: 6px 9px;
    border-radius: 6px;
    white-space: pre-wrap;
    word-break: break-word;
  }
  .msg.user {
    background: #e8f0fe;
    align-self: flex-end;
    max-width: 85%;
  }
  .msg.assistant {
    background: #f5f5f5;
  }
  .msg.info {
    background: #fff8e1;
    font-size: 12px;
    color: #6d4c41;
  }
  .msg.thinking {
    color: #888;
    font-style: italic;
  }
  .msg.tool {
    display: flex;
    gap: 8px;
    align-items: center;
    background: #f0f7f0;
    font-size: 12px;
  }
  .msg.tool.err {
    background: #fdecea;
  }
  .tname {
    font-weight: 600;
  }
  .tsum {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #555;
  }
  .msg.err-msg,
  .claude-err {
    background: #fdecea;
    color: #b3261e;
    padding: 6px 9px;
    border-radius: 6px;
  }

  .claude-input {
    display: flex;
    gap: 6px;
    padding-top: 6px;
  }
  .claude-input textarea {
    flex: 1;
    resize: none;
    font: inherit;
    padding: 6px;
    border: 1px solid #ccc;
    border-radius: 5px;
  }
  .send {
    padding: 6px 14px;
    border: none;
    background: #1a73e8;
    color: #fff;
    border-radius: 5px;
    cursor: pointer;
  }
  .send:disabled {
    background: #b0c4de;
    cursor: default;
  }

  .approve-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.35);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 60;
  }
  .approve {
    background: #fff;
    border-radius: 8px;
    padding: 16px;
    width: min(620px, 92vw);
    max-height: 80vh;
    overflow-y: auto;
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.25);
  }
  .approve-title {
    font-weight: 700;
    margin-bottom: 8px;
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .qcount {
    font-weight: 400;
    font-size: 12px;
    color: #8a6d3b;
    background: #fff8e1;
    border: 1px solid #ffe082;
    border-radius: 10px;
    padding: 1px 7px;
  }
  .approve-tool {
    margin-bottom: 8px;
  }
  .approve-body {
    background: #f6f6f6;
    padding: 8px;
    border-radius: 5px;
    font-size: 12px;
    max-height: 240px;
    overflow: auto;
    white-space: pre-wrap;
    word-break: break-word;
  }
  .approve-body.cmd {
    background: #1e1e1e;
    color: #eee;
  }
  .bash-warn {
    background: #fff8e1;
    border: 1px solid #ffe082;
    border-radius: 5px;
    padding: 7px 9px;
    font-size: 12px;
    color: #6d4c41;
  }
  .approve-actions {
    display: flex;
    gap: 8px;
    margin-top: 12px;
    flex-wrap: wrap;
  }
  .approve-actions button {
    padding: 6px 12px;
    border-radius: 5px;
    border: 1px solid #ccc;
    background: #f7f7f7;
    cursor: pointer;
  }
  .approve-actions .ok {
    background: #1a73e8;
    color: #fff;
    border-color: #1a73e8;
  }
  .approve-actions .deny {
    background: #fdecea;
    color: #b3261e;
    border-color: #f5c6c2;
  }

  :global(html.dark) .claude-root,
  :global(html.dark) .msg.assistant {
    color: #ddd;
  }
  :global(html.dark) .msg.assistant {
    background: #2a2a2a;
  }
  :global(html.dark) .msg.user {
    background: #1e3a5f;
    color: #ddd;
  }
  :global(html.dark) .msg.tool {
    background: #23301f;
    color: #ddd;
  }
  :global(html.dark) .claude-hint {
    background: #1e2a3a;
    border-color: #2f4159;
    color: #ddd;
  }
  :global(html.dark) .engine-box {
    background: #33291a;
    border-color: #5a4a2a;
    color: #ddd;
  }
  :global(html.dark) .approve {
    background: #222;
    color: #ddd;
  }
  :global(html.dark) .approve-body {
    background: #2a2a2a;
  }
  :global(html.dark) .claude-input textarea {
    background: #2a2a2a;
    color: #ddd;
    border-color: #444;
  }
  :global(html.dark) .pick,
  :global(html.dark) .reset,
  :global(html.dark) .install,
  :global(html.dark) .approve-actions button {
    background: #333;
    color: #ddd;
    border-color: #555;
  }
</style>
