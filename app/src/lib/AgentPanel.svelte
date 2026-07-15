<!--
  AgentPanel — Klasör Ajanı (VSCode-benzeri) modu.

  "Öneri" modundan farkı: model dosyaları okur/yazar/düzenler; çalışma klasörü
  sandbox'tır ve her araç kullanıcı onayından geçer ("hep izin ver" seçeneğiyle).

  Güvenlik: dosya yolları Rust'ta guard() ile köke kilitlenir (agent_tools.rs);
  bu bileşen yalnızca gösterir/tetikler, korumayı atlayamaz.
-->
<script lang="ts">
  import { agent, runAgent, resolveApproval, setAgentRoot, resetAgentChat } from '$lib/agent-session.svelte';
  import { pickFolder } from '$lib/batch';

  let input = $state('');

  async function chooseFolder(): Promise<void> {
    const f = await pickFolder();
    if (f) setAgentRoot(f);
  }

  async function send(): Promise<void> {
    const text = input.trim();
    if (!text || agent.running || !agent.root) return;
    input = '';
    await runAgent(text);
  }

  function onKey(e: KeyboardEvent): void {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      void send();
    }
  }
</script>

<div class="agent">
  <!-- Klasör (sandbox kökü) -->
  <div class="agent-root">
    <button class="pick" onclick={chooseFolder}>📁 Çalışma klasörü seç…</button>
    {#if agent.root}
      <code class="root-path" title={agent.root}>{agent.root}</code>
      <button class="reset" onclick={resetAgentChat} disabled={agent.running}>Sohbeti sıfırla</button>
    {/if}
  </div>

  {#if !agent.root}
    <p class="agent-hint">
      Ajan seçtiğin klasörün içinde çalışır — dosyaları okur, düzenler, oluşturur.
      Her işlem senin onayından geçer. Başlamak için bir klasör seç.
    </p>
  {/if}

  <!-- Olay akışı -->
  <div class="feed">
    {#each agent.feed as item, i (i)}
      {#if item.kind === 'user'}
        <div class="msg user">{item.text}</div>
      {:else if item.kind === 'assistant'}
        <div class="msg assistant">{item.text}</div>
      {:else if item.kind === 'tool'}
        <div class="msg tool" class:err={item.tool && item.tool.ok === false}>
          <span class="tname">{item.tool?.name}</span>
          <span class="tsum">{item.tool?.summary}</span>
          <span class="tstat">{item.tool?.ok === false ? '✗' : '✓'}</span>
        </div>
      {:else if item.kind === 'error'}
        <div class="msg err-msg">{item.text}</div>
      {/if}
    {/each}
    {#if agent.running && !agent.pending}
      <div class="msg thinking">Çalışıyor…</div>
    {/if}
  </div>

  {#if agent.error}
    <div class="agent-err">{agent.error}</div>
  {/if}

  <!-- Giriş -->
  <div class="agent-input">
    <textarea
      bind:value={input}
      onkeydown={onKey}
      placeholder={agent.root ? 'Ne yapmamı istersin? (⌘/Ctrl+Enter ile gönder)' : 'Önce bir klasör seç'}
      disabled={!agent.root || agent.running}
      rows="2"
    ></textarea>
    <button class="send" onclick={send} disabled={!agent.root || agent.running || !input.trim()}>
      {agent.running ? '…' : 'Gönder'}
    </button>
  </div>
</div>

<!-- Onay modalı -->
{#if agent.pending}
  {@const p = agent.pending}
  <div class="approve-overlay">
    <div class="approve">
      <div class="approve-title">İşlem onayı</div>
      <div class="approve-tool"><span class="tname">{p.call.name}</span> — {p.summary}</div>
      {#if p.call.name === 'run_bash'}
        <pre class="approve-body cmd">{String((p.call.input as Record<string, string>).command ?? '')}</pre>
        <p class="bash-warn">
          ⚠️ Komut çalışma klasöründe (cwd) çalışır ama <b>klasör dışına çıkabilir</b> —
          gerçek koruma bu onaydır. Ne yaptığını anladığından emin ol.
        </p>
      {:else if p.call.name === 'write_file' || p.call.name === 'edit_file'}
        <pre class="approve-body">{JSON.stringify(p.call.input, null, 2).slice(0, 2000)}</pre>
      {/if}
      <div class="approve-actions">
        <button class="ok" onclick={() => resolveApproval('allow')}>Onayla</button>
        <button class="always" onclick={() => resolveApproval('always')}>Bu aracı hep izin ver</button>
        <button class="trust" onclick={() => resolveApproval('trust_folder')}>Bu klasöre güven</button>
        <button class="deny" onclick={() => resolveApproval('deny')}>Reddet</button>
      </div>
    </div>
  </div>
{/if}

<style>
  .agent {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    font-size: 13px;
  }
  .agent-root {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 6px 8px;
    border-bottom: 1px solid #e5e7eb;
    flex-wrap: wrap;
  }
  .pick,
  .reset,
  .send {
    padding: 4px 10px;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    background: #fff;
    cursor: pointer;
    font-size: 12px;
  }
  .pick:hover,
  .reset:hover:not(:disabled),
  .send:hover:not(:disabled) {
    background: #eef4ff;
  }
  .root-path {
    flex: 1;
    min-width: 0;
    font-family: ui-monospace, Menlo, monospace;
    font-size: 11px;
    color: #6b7280;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .agent-hint {
    margin: 0.75rem;
    font-size: 12px;
    color: #6b7280;
    line-height: 1.5;
  }
  .feed {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    padding: 8px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .msg {
    padding: 6px 10px;
    border-radius: 6px;
    line-height: 1.45;
    white-space: pre-wrap;
    word-break: break-word;
  }
  .msg.user {
    background: #eef4ff;
    align-self: flex-end;
    max-width: 85%;
  }
  .msg.assistant {
    background: #f6f8fa;
  }
  .msg.tool {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    background: #f0fdf4;
    font-size: 12px;
    font-family: ui-monospace, Menlo, monospace;
  }
  .msg.tool.err {
    background: #fef2f2;
  }
  .msg.tool .tname {
    font-weight: 600;
    color: #166534;
  }
  .msg.tool.err .tname {
    color: #b91c1c;
  }
  .msg.tool .tsum {
    flex: 1;
    color: #374151;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .msg.thinking {
    color: #6b7280;
    font-style: italic;
  }
  .msg.err-msg {
    background: #fef2f2;
    color: #b91c1c;
  }
  .agent-err {
    padding: 6px 10px;
    background: #fef2f2;
    color: #b91c1c;
    font-size: 12px;
  }
  .agent-input {
    display: flex;
    gap: 0.5rem;
    padding: 8px;
    border-top: 1px solid #e5e7eb;
    align-items: flex-end;
  }
  .agent-input textarea {
    flex: 1;
    resize: vertical;
    padding: 6px 8px;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 13px;
    font-family: inherit;
  }
  .approve-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.35);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 10001;
  }
  .approve {
    background: #fff;
    border-radius: 8px;
    padding: 16px;
    max-width: 520px;
    width: 90%;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.25);
  }
  .approve-title {
    font-weight: 600;
    margin-bottom: 8px;
  }
  .approve-tool {
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    margin-bottom: 8px;
  }
  .approve-tool .tname {
    font-weight: 600;
    color: #166534;
  }
  .approve-body {
    max-height: 280px;
    overflow: auto;
    background: #f6f8fa;
    border-radius: 4px;
    padding: 8px;
    font-size: 11px;
    margin: 0 0 12px;
  }
  .approve-actions {
    display: flex;
    gap: 0.5rem;
    justify-content: flex-end;
    flex-wrap: wrap;
  }
  .approve-actions button {
    padding: 6px 12px;
    border-radius: 4px;
    border: 1px solid #cbd0d6;
    cursor: pointer;
    font-size: 13px;
  }
  .approve-actions .ok {
    background: #2563eb;
    color: #fff;
    border-color: #2563eb;
  }
  .approve-actions .always,
  .approve-actions .trust {
    background: #fff;
  }
  .approve-body.cmd {
    font-family: ui-monospace, Menlo, monospace;
    white-space: pre-wrap;
    color: #111;
  }
  .bash-warn {
    margin: 0 0 12px;
    padding: 6px 8px;
    background: #fffbeb;
    border: 1px solid #fde68a;
    border-radius: 4px;
    font-size: 12px;
    color: #92400e;
    line-height: 1.4;
  }
  :global(html.dark) .bash-warn {
    background: #2a2408;
    border-color: #78350f;
    color: #fbbf24;
  }
  :global(html.dark) .approve-body.cmd {
    color: #e6e6e6;
  }
  .approve-actions .deny {
    background: #fff;
    color: #b91c1c;
    border-color: #fca5a5;
  }
  :global(html.dark) .agent-root,
  :global(html.dark) .agent-input {
    border-color: #3f3f46;
  }
  :global(html.dark) .pick,
  :global(html.dark) .reset,
  :global(html.dark) .send,
  :global(html.dark) .agent-input textarea {
    background: #2d2d30;
    border-color: #4b5563;
    color: #e6e6e6;
  }
  :global(html.dark) .msg.assistant {
    background: #252526;
  }
  :global(html.dark) .msg.user {
    background: #094771;
    color: #e6e6e6;
  }
  :global(html.dark) .msg.tool {
    background: #0f2417;
  }
  :global(html.dark) .approve {
    background: #2d2d30;
    color: #e6e6e6;
  }
  :global(html.dark) .approve-body {
    background: #1e1e1e;
  }
</style>
