<script lang="ts">
  import {
    settings,
    updateSetting,
    updateAiProviderConfig,
    resetSettings,
    loadApiKeys,
    THEME_OPTIONS,
    AI_PROVIDER_OPTIONS,
    type AiProvider,
  } from '$lib/settings.svelte';
  import { goto } from '$app/navigation';
  import { invoke } from '@tauri-apps/api/core';
  import { onMount } from 'svelte';
  import { manifest } from '$lib/data';
  import { openUrl } from '@tauri-apps/plugin-opener';
  import { updater, checkForUpdate } from '$lib/updater.svelte';
  import { log, describeError } from '$lib/logger';
  import { m, f, allLocales, detectSystemLocale } from '$lib/i18n.svelte';
  import UpdateModal from '$lib/UpdateModal.svelte';

  const REPO_URL = 'https://github.com/hzkucuk/eFaturaEdit';
  const CONTACT_EMAIL = 'hzkucuk@gmail.com';

  // Ayarlara doğrudan gelinirse anahtarların zincirden yüklendiğinden emin ol
  // (once-guard: ana sayfada zaten yüklendiyse tekrar çalışmaz).
  onMount(() => void loadApiKeys());

  const currentAiConfig = $derived(settings.aiProviders[settings.aiProvider]);
  const currentAiOption = $derived(
    AI_PROVIDER_OPTIONS.find((o) => o.value === settings.aiProvider),
  );

  let modelOptions = $state<string[]>([]);
  let modelsLoading = $state(false);
  let modelsError = $state('');

  // Google'ın ListModels uç noktası "-latest" takma adlarını (ör.
  // gemini-flash-latest) hiç listelemiyor, ama bu takma adlar sağlayıcı
  // tarafından her zaman güncel/geçerli modele yönlendiriliyor — bu yüzden
  // dropdown'a elle ekliyoruz ki kullanıcı ListModels'in gösterdiği (ve bazen
  // yeni hesaplarda 404 veren) tarihli sürümlere mahkum kalmasın.
  const KNOWN_ALIASES: Partial<Record<AiProvider, string[]>> = {
    gemini: ['gemini-flash-latest', 'gemini-pro-latest'],
  };

  function withKnownAliases(provider: AiProvider, models: string[]): string[] {
    const aliases = KNOWN_ALIASES[provider] ?? [];
    return [...aliases, ...models.filter((model) => !aliases.includes(model))];
  }

  // Sağlayıcı değişince o sağlayıcının önbelleklenmiş model listesini göster;
  // hiç önbellek yoksa ve anahtar gerektirmiyorsa (ör. Ollama) otomatik getir.
  $effect(() => {
    const provider = settings.aiProvider;
    const cached = settings.aiProviders[provider].cachedModels;
    modelOptions = withKnownAliases(provider, cached);
    modelsError = '';
    if (cached.length === 0 && !currentAiOption?.needsKey) {
      void fetchModels();
    }
  });

  // Sağlayıcı listeleri metin-sohbeti dışı uzman modelleri de döndürüyor
  // (görsel/ses/embedding önizlemeleri gibi) — bunlar genelde ücretsiz planda
  // kotasız (limit: 0) olduğundan otomatik seçimde atlanmalı.
  const NON_CHAT_HINTS = ['embedding', 'aqa', 'tts', 'image', 'imagen', 'vision', 'audio', 'live'];

  function pickAutoModel(models: string[]): string {
    const chatCandidates = models.filter(
      (model) => !NON_CHAT_HINTS.some((hint) => model.toLowerCase().includes(hint)),
    );
    const pool = chatCandidates.length > 0 ? chatCandidates : models;
    // Kararlı (preview/exp içermeyen) bir sürüm varsa onu tercih et.
    const stable = pool.find((model) => !/preview|exp/i.test(model));
    return stable ?? pool[0];
  }

  async function fetchModels() {
    modelsError = '';
    modelsLoading = true;
    try {
      const models = await invoke<string[]>('ai_list_models', {
        request: {
          provider: settings.aiProvider,
          base_url: currentAiConfig.baseUrl,
          api_key: currentAiConfig.apiKey,
        },
      });
      modelOptions = withKnownAliases(settings.aiProvider, models);
      updateAiProviderConfig(settings.aiProvider, 'cachedModels', models);
      // "-latest" takma adları (ör. gemini-flash-latest) sağlayıcı tarafından
      // her zaman güncel modele yönlendirilir ama ListModels çıktısında hiç
      // görünmeyebilir — listede yok diye elden alınmamalı.
      const isAlias = /-latest$/i.test(currentAiConfig.model);
      const currentIsNonChat = NON_CHAT_HINTS.some((hint) =>
        currentAiConfig.model.toLowerCase().includes(hint),
      );
      if (models.length === 0) {
        modelsError = 'Sağlayıcı hiç model döndürmedi.';
      } else if (!isAlias && (!models.includes(currentAiConfig.model) || currentIsNonChat)) {
        // Kayıtlı model artık listede yok ya da sohbet için uygun değil (ör. görsel/ses önizlemesi) — uygun bir modele düş.
        updateAiProviderConfig(settings.aiProvider, 'model', pickAutoModel(models));
      }
    } catch (err) {
      modelsError = (err as Error).message ?? String(err);
    } finally {
      modelsLoading = false;
    }
  }

  function onApiKeyBlur() {
    if (currentAiConfig.apiKey.trim().length > 0) {
      void fetchModels();
    }
  }

  let logPath = $state('');
  let logError = $state('');

  // Yolu baştan göster: klasör açılamasa bile kullanıcı elle bulabilsin.
  onMount(async () => {
    try {
      logPath = await invoke<string>('log_dir');
    } catch (err) {
      log.error(`[ayarlar] günlük yolu alınamadı: ${describeError(err)}`);
    }
  });

  /**
   * Günlük klasörünü aç. Rust tarafından açılır — arayüzün `opener` izni yalnızca
   * $APPDATA altını kapsıyor, günlük klasörü ise başka yerde (istek sessizce
   * reddediliyordu).
   *
   * Hata olursa SESSİZ KALMA: kullanıcı "tıklıyorum, hiçbir şey olmuyor" diyordu.
   */
  async function openLogDir() {
    logError = '';
    try {
      logPath = await invoke<string>('open_log_dir');
    } catch (err) {
      logError = describeError(err);
      log.error(`[ayarlar] günlük klasörü açılamadı: ${logError}`);
    }
  }
</script>

<div class="settings-page">
  <header class="page-header">
    <button class="back" onclick={() => goto('/')}>← {m.common.back}</button>
    <h1>{m.settings.title}</h1>
    <button class="reset" onclick={resetSettings}>{m.settings.resetAll}</button>
  </header>

  <div class="content">
    <section class="group">
      <h2>{m.settings.editor}</h2>

      <div class="row">
        <label for="font-size">{m.settings.fontSize}</label>
        <input
          id="font-size"
          type="range"
          min="10"
          max="24"
          step="1"
          value={settings.fontSize}
          oninput={(e) => updateSetting('fontSize', +(e.currentTarget as HTMLInputElement).value)}
        />
        <span class="val">{settings.fontSize}px</span>
      </div>

      <div class="row">
        <label for="tab-width">{m.settings.tabWidth}</label>
        <select
          id="tab-width"
          value={settings.tabWidth}
          onchange={(e) => updateSetting('tabWidth', +(e.currentTarget as HTMLSelectElement).value)}
        >
          <option value={2}>{f(m.settings.spaces, { n: 2 })}</option>
          <option value={4}>{f(m.settings.spaces, { n: 4 })}</option>
          <option value={8}>{f(m.settings.spaces, { n: 8 })}</option>
        </select>
      </div>

      <div class="row">
        <label for="wrap">{m.settings.wordWrap}</label>
        <input
          id="wrap"
          type="checkbox"
          checked={settings.wordWrap}
          onchange={(e) =>
            updateSetting('wordWrap', (e.currentTarget as HTMLInputElement).checked)}
        />
      </div>

      <div class="row">
        <label for="linenum">{m.settings.lineNumbers}</label>
        <input
          id="linenum"
          type="checkbox"
          checked={settings.showLineNumbers}
          onchange={(e) =>
            updateSetting('showLineNumbers', (e.currentTarget as HTMLInputElement).checked)}
        />
      </div>
    </section>

    <section class="group">
      <h2>{m.settings.appearance}</h2>

      <!-- Dil: seçilmemişse sistem dilinden algılanır (bkz. +layout.svelte). -->
      <div class="row">
        <label for="language">{m.settings.language}</label>
        <select
          id="language"
          value={settings.language ?? detectSystemLocale()}
          onchange={(e) =>
            updateSetting('language', (e.currentTarget as HTMLSelectElement).value)}
        >
          {#each allLocales() as opt}
            <option value={opt.code}>{opt.flag} {opt.label}</option>
          {/each}
        </select>
        <span class="hint lang-hint">{m.settings.languageHint}</span>
        <button class="link-btn" onclick={() => goto('/translations')}>
          ✏️ {m.settings.editTranslations}
        </button>
      </div>

      <div class="row">
        <label for="theme">{m.settings.theme}</label>
        <select
          id="theme"
          value={settings.theme}
          onchange={(e) =>
            updateSetting(
              'theme',
              (e.currentTarget as HTMLSelectElement).value as typeof settings.theme,
            )}
        >
          {#each THEME_OPTIONS as opt}
            <option value={opt.value}>
              {opt.label} ({opt.kind === 'dark' ? m.settings.themeDark : m.settings.themeLight})
            </option>
          {/each}
        </select>
      </div>
    </section>

    <section class="group">
      <h2>Davranış</h2>

      <div class="row">
        <label for="autoxform">Örnek/dosya yüklendiğinde otomatik dönüştür</label>
        <input
          id="autoxform"
          type="checkbox"
          checked={settings.autoTransformOnLoad}
          onchange={(e) =>
            updateSetting(
              'autoTransformOnLoad',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>

      <div class="row">
        <label for="autoxformsave">Kaydettikten sonra otomatik dönüştür</label>
        <input
          id="autoxformsave"
          type="checkbox"
          checked={settings.autoTransformOnSave}
          onchange={(e) =>
            updateSetting(
              'autoTransformOnSave',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>

      <div class="row">
        <label for="autocomplete">Autocomplete (Ctrl+Space)</label>
        <input
          id="autocomplete"
          type="checkbox"
          checked={settings.autocomplete}
          onchange={(e) =>
            updateSetting(
              'autocomplete',
              (e.currentTarget as HTMLInputElement).checked,
            )}
        />
      </div>

      <div class="row">
        <label for="autosave">Dosyayı otomatik kaydet (yalnızca disk yolu olan dosyalar)</label>
        <input
          id="autosave"
          type="checkbox"
          checked={settings.autoSave}
          onchange={(e) =>
            updateSetting('autoSave', (e.currentTarget as HTMLInputElement).checked)}
        />
      </div>

      {#if settings.autoSave}
        <div class="row">
          <label for="autosave-delay">Otomatik kaydetme gecikmesi</label>
          <select
            id="autosave-delay"
            value={settings.autoSaveDelayMs}
            onchange={(e) =>
              updateSetting('autoSaveDelayMs', +(e.currentTarget as HTMLSelectElement).value)}
          >
            <option value={1000}>1 saniye</option>
            <option value={3000}>3 saniye</option>
            <option value={5000}>5 saniye</option>
            <option value={10000}>10 saniye</option>
          </select>
        </div>
      {/if}
    </section>

    <section class="group">
      <h2>AI Asistan</h2>
      <p class="hint">
        Sohbet panelinden XSLT/XML önerileri almak için bir sağlayıcı seçip kendi
        API anahtarınızı girin. Anahtar yalnızca bu cihazda saklanır — hiçbir
        sunucuya gönderilmez, uygulamaya gömülü bir anahtar yoktur.
      </p>

      <div class="row">
        <label for="ai-provider">Sağlayıcı</label>
        <select
          id="ai-provider"
          value={settings.aiProvider}
          onchange={(e) =>
            updateSetting(
              'aiProvider',
              (e.currentTarget as HTMLSelectElement).value as typeof settings.aiProvider,
            )}
        >
          {#each AI_PROVIDER_OPTIONS as opt}
            <option value={opt.value}>{opt.label}</option>
          {/each}
        </select>
      </div>

      {#if currentAiOption?.needsKey}
        <div class="row">
          <label for="ai-key">API Anahtarı</label>
          <input
            id="ai-key"
            type="password"
            placeholder="API anahtarınızı yapıştırın"
            value={currentAiConfig.apiKey}
            oninput={(e) =>
              updateAiProviderConfig(
                settings.aiProvider,
                'apiKey',
                (e.currentTarget as HTMLInputElement).value,
              )}
            onblur={onApiKeyBlur}
          />
        </div>
      {/if}

      <div class="row">
        <label for="ai-model">Model</label>
        <input
          id="ai-model"
          type="text"
          list="ai-model-list"
          placeholder="ör. claude-sonnet-5"
          value={currentAiConfig.model}
          oninput={(e) =>
            updateAiProviderConfig(
              settings.aiProvider,
              'model',
              (e.currentTarget as HTMLInputElement).value,
            )}
        />
        <button class="fetch-models" onclick={fetchModels} disabled={modelsLoading} title="Sağlayıcıdan kullanılabilir modelleri getir">
          {modelsLoading ? '…' : '🔄 Getir'}
        </button>
      </div>
      <datalist id="ai-model-list">
        {#each modelOptions as m}
          <option value={m}></option>
        {/each}
      </datalist>
      {#if modelsError}
        <p class="model-error">{modelsError}</p>
      {/if}

      <div class="row">
        <label for="ai-baseurl">Base URL</label>
        <input
          id="ai-baseurl"
          type="text"
          value={currentAiConfig.baseUrl}
          oninput={(e) =>
            updateAiProviderConfig(
              settings.aiProvider,
              'baseUrl',
              (e.currentTarget as HTMLInputElement).value,
            )}
        />
      </div>
    </section>

    <section class="group panel-sizes">
      <h2>Panel Boyutları</h2>
      <p class="hint">
        Mouse ile bölmeleri sürükleyerek de değiştirebilirsin. Bu değerler
        otomatik kaydedilir.
      </p>
      <div class="row">
        <span class="static-label">Snippet Panel Genişliği</span>
        <span class="val">{settings.panelSizes.snippetsWidth}px</span>
      </div>
      <div class="row">
        <span class="static-label">Editör Kolonu Genişliği</span>
        <span class="val">{settings.panelSizes.editorsWidth}px</span>
      </div>
      <div class="row">
        <span class="static-label">XSLT Editör Yüksekliği</span>
        <span class="val">{settings.panelSizes.xsltHeight}px</span>
      </div>
    </section>

    <section class="group about">
      <h2>{m.settings.about}</h2>
      <div class="about-head">
        <div>
          <div class="about-name">{m.settings.appName}</div>
          <div class="about-ver">{f(m.settings.version, { v: manifest.version })}</div>
        </div>
      </div>

      <div class="row">
        <span class="static-label">{m.settings.logs}</span>
        <span class="upd-cell">
          <button class="link-btn" onclick={openLogDir}>{m.settings.openLogDir}</button>
          {#if logError}
            <span class="upd-err">{f(m.settings.logOpenFailed, { msg: logError })}</span>
          {:else}
            <span class="log-hint">{m.settings.logHint}</span>
          {/if}
        </span>
      </div>
      {#if logPath}
        <!-- Yol her zaman görünür: klasör açılamasa bile elle bulunabilsin. -->
        <div class="row">
          <span class="static-label"></span>
          <code class="log-path" title={m.settings.logDirTitle}>{logPath}</code>
        </div>
      {/if}

      <div class="row">
        <span class="static-label">{m.settings.update}</span>
        <span class="upd-cell">
          <button
            class="link-btn"
            onclick={() => void checkForUpdate(true)}
            disabled={updater.stage === 'checking'}
          >
            {updater.stage === 'checking' ? m.update.checking : m.update.check}
          </button>
          {#if updater.stage === 'none'}
            <span class="upd-ok">{m.update.upToDate}</span>
          {:else if updater.stage === 'available'}
            <span class="upd-new">{f(m.update.found, { version: updater.version })}</span>
          {:else if updater.stage === 'error'}
            <span class="upd-err">{f(m.update.checkFailed, { msg: updater.error })}</span>
          {/if}
        </span>
      </div>
      <p class="hint">
        Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) belgeleri için XSLT
        tasarım düzenleyicisi. Tam XSLT 1.0/2.0/3.0 (Saxon-HE), canlı önizleme,
        BYOK AI asistan ve hazır snippet kütüphanesi.
      </p>

      <div class="row">
        <span class="static-label">{m.settings.license}</span>
        <span class="upd-cell">
          <span class="about-val">MIT</span>
          <span class="fb-dot">·</span>
          <button
            class="link-btn"
            onclick={() => openUrl(`${REPO_URL}/blob/master/LICENSE.tr.md`)}
            title={m.settings.licenseTrTitle}
          >{m.settings.licenseTr}</button>
        </span>
      </div>
      <div class="row">
        <span class="static-label">{m.settings.copyright}</span>
        <span class="about-val">© 2018–2026 Zafer Bilgisayar</span>
      </div>
      <div class="row">
        <span class="static-label">{m.settings.sourceCode}</span>
        <button class="link-btn" onclick={() => openUrl(REPO_URL)}>github.com/hzkucuk/eFaturaEdit</button>
      </div>
      <div class="row">
        <span class="static-label">{m.settings.contact}</span>
        <span class="upd-cell">
          <button class="link-btn" onclick={() => openUrl(`mailto:${CONTACT_EMAIL}`)}>{CONTACT_EMAIL}</button>
          <span class="fb-dot">·</span>
          <button class="link-btn" onclick={() => openUrl(`${REPO_URL}/issues`)}>{m.settings.reportIssue}</button>
        </span>
      </div>

      <h3 class="about-sub">{m.settings.openSourceLibs}</h3>
      <ul class="about-libs">
        <li><b>Saxon-HE</b> — XSLT 2.0/3.0 motoru · Mozilla Public License 2.0 · © Saxonica</li>
        <li><b>Tauri</b> · <b>SvelteKit</b> / <b>Svelte</b> · <b>CodeMirror 6</b> · <b>Vite</b> — MIT/Apache-2.0</li>
        <li><b>reqwest</b>, <b>keyring</b> (Rust) — API çağrıları ve OS anahtar zinciri</li>
      </ul>
      <p class="hint">
        API anahtarları yalnızca bu cihazda, OS anahtar zincirinde şifreli
        saklanır. AI özellikleri BYOK'tur (kendi anahtarınız); uygulamaya gömülü
        hiçbir anahtar yoktur.
      </p>
    </section>
  </div>
</div>

<!-- Elle denetlemede güncelleme bulunursa kurulum penceresi burada da açılsın. -->
<UpdateModal />

<style>
  .fb-dot {
    color: #9ca3af;
  }

  .log-path {
    font-family: var(--mono, ui-monospace, monospace);
    font-size: 11px;
    color: #6b7280;
    word-break: break-all;
  }
  :global(html.dark) .log-path { color: #9aa1ac; }

  .lang-hint {
    flex: 1 1 auto;
  }

  .log-hint {
    font-size: 11px;
    color: #6b7280;
  }
  :global(html.dark) .log-hint { color: #9aa1ac; }

  .upd-cell {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    flex-wrap: wrap;
  }
  .upd-ok {
    font-size: 12px;
    color: #16a34a;
  }
  .upd-new {
    font-size: 12px;
    color: #2563eb;
    font-weight: 600;
  }
  .upd-err {
    font-size: 12px;
    color: #dc2626;
  }

  /* Sabit açık renk yerine temaya duyarlı: koyu tema kök <html.dark>'tan gelir. */
  :global(body) {
    background: #f5f6f8;
  }
  :global(html.dark body) {
    background: #1a1a1a;
  }
  .settings-page {
    max-width: 800px;
    margin: 0 auto;
    padding: 1rem;
    height: 100vh;
    overflow-y: auto;
  }
  .page-header {
    display: grid;
    grid-template-columns: auto 1fr auto;
    align-items: center;
    gap: 1rem;
    padding: 0.75rem 1rem;
    background: #fff;
    border: 1px solid #d5d8dc;
    border-radius: 8px;
    margin-bottom: 1rem;
  }
  .page-header h1 {
    margin: 0;
    font-size: 18px;
    color: #1a1a1a;
  }
  .back,
  .reset {
    padding: 0.4rem 0.8rem;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 4px;
    cursor: pointer;
    font-size: 13px;
  }
  .back:hover {
    background: #f0f2f5;
  }
  .reset {
    color: #b91c1c;
    border-color: #fca5a5;
  }
  .reset:hover {
    background: #fee2e2;
  }

  .content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }
  .group {
    background: #fff;
    border: 1px solid #d5d8dc;
    border-radius: 8px;
    padding: 1rem 1.25rem;
  }
  .group h2 {
    margin: 0 0 0.75rem 0;
    font-size: 14px;
    color: #0a5cff;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  .row {
    display: grid;
    grid-template-columns: 1fr auto auto;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid #f0f2f5;
  }
  .row:last-child {
    border-bottom: none;
  }
  .row label,
  .row .static-label {
    font-size: 13px;
    color: #1a1a1a;
  }
  .row input[type='range'] {
    width: 200px;
  }
  .row input[type='checkbox'] {
    width: 18px;
    height: 18px;
    cursor: pointer;
  }
  .row select {
    padding: 0.3rem 0.5rem;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 13px;
    background: #fff;
    cursor: pointer;
  }
  .row input[type='text'],
  .row input[type='password'] {
    width: 260px;
    padding: 0.3rem 0.5rem;
    border: 1px solid #cbd0d6;
    border-radius: 4px;
    font-size: 13px;
    font-family: ui-monospace, Menlo, monospace;
  }
  .val {
    font-family: ui-monospace, Menlo, monospace;
    font-size: 12px;
    color: #6b7280;
    min-width: 60px;
    text-align: right;
  }
  .hint {
    margin: 0 0 0.75rem 0;
    font-size: 12px;
    color: #6b7280;
    line-height: 1.4;
  }
  .about-head {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.5rem;
  }
  .about-name {
    font-size: 15px;
    font-weight: 700;
    color: #0a5cff;
  }
  .about-ver {
    font-size: 12px;
    color: #6b7280;
    font-family: ui-monospace, Menlo, monospace;
  }
  .about-val {
    font-size: 13px;
    color: #1a1a1a;
  }
  .about-sub {
    margin: 1rem 0 0.4rem;
    font-size: 12px;
    color: #6b7280;
    text-transform: uppercase;
    letter-spacing: 0.4px;
  }
  .about-libs {
    margin: 0 0 0.75rem;
    padding-left: 1.1rem;
    font-size: 12px;
    color: #4b5563;
    line-height: 1.6;
  }
  .link-btn {
    background: none;
    border: none;
    color: #0a5cff;
    cursor: pointer;
    font-size: 13px;
    padding: 0;
    text-decoration: underline;
  }
  .fetch-models {
    padding: 0.3rem 0.6rem;
    border: 1px solid #cbd0d6;
    background: #fff;
    border-radius: 4px;
    font-size: 12px;
    cursor: pointer;
    white-space: nowrap;
  }
  .fetch-models:hover:not(:disabled) {
    background: #eef4ff;
  }
  .fetch-models:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
  .model-error {
    margin: -0.25rem 0 0.5rem 0;
    font-size: 11.5px;
    color: #b91c1c;
  }

  /* ── Koyu tema ──────────────────────────────────────────────────────
     Ayarlar ayrı bir route olduğundan ana sayfadaki `.app.dark` sınıfına
     erişemez; tema kök <html class="dark"> üzerinden uygulanır (bkz.
     +layout.svelte). */
  :global(html.dark) .page-header,
  :global(html.dark) .group {
    background: #252526;
    border-color: #3f3f46;
  }
  :global(html.dark) .page-header h1,
  :global(html.dark) .row label,
  :global(html.dark) .row .static-label,
  :global(html.dark) .about-val {
    color: #e6e6e6;
  }
  :global(html.dark) .row {
    border-bottom-color: #3f3f46;
  }
  :global(html.dark) .back,
  :global(html.dark) .row select,
  :global(html.dark) .row input[type='text'],
  :global(html.dark) .row input[type='password'],
  :global(html.dark) .fetch-models {
    background: #2d2d30;
    border-color: #4b5563;
    color: #e6e6e6;
  }
  :global(html.dark) .back:hover,
  :global(html.dark) .fetch-models:hover:not(:disabled) {
    background: #3a3a3d;
  }
  :global(html.dark) .reset {
    background: #2d2d30;
    border-color: #7f1d1d;
    color: #fca5a5;
  }
  :global(html.dark) .reset:hover {
    background: #3b1111;
  }
  :global(html.dark) .hint,
  :global(html.dark) .val,
  :global(html.dark) .about-ver,
  :global(html.dark) .about-sub,
  :global(html.dark) .about-libs {
    color: #9ca3af;
  }
</style>
