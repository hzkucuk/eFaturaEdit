//! AI sağlayıcı köprüsü — Claude, ChatGPT, Gemini, DeepSeek, Ollama, NVIDIA NIM.
//!
//! Kapsam kilidi burada uygulanır: bu modül yalnızca bir metin yanıtı
//! döndürür, hiçbir dosya sistemi/ayar erişimi yoktur. Çağıran taraf
//! (Svelte), dönen metni yalnızca "önerilen değişiklik" olarak gösterip
//! kullanıcı onayı olmadan uygulamaz.
//!
//! API anahtarları yalnızca kullanıcının kendi ayarlarından (BYOK) gelir,
//! hiçbir anahtar bu ikilikte gömülü değildir.

use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::time::{Duration, Instant};

/// Bağlantı kurma üst sınırı. Ulaşılamayan uç noktada hızlıca hata ver.
const CONNECT_TIMEOUT: Duration = Duration::from_secs(15);
/// Yanıt üst sınırı. Sağlayıcı isteği kuyruğa alıp yanıt vermezse (NVIDIA NIM
/// büyük modellerde kuyruklar), timeout olmadan istek SONSUZA KADAR bekler ve
/// kullanıcı ekranda sonsuz "Düşünüyor…" görür — hata bile almaz. Uzun
/// reasoning yanıtlarına yetecek kadar geniş, asılı kalmaya yetmeyecek kadar dar.
const REQUEST_TIMEOUT: Duration = Duration::from_secs(180);

/// Tüm AI çağrıları bu istemciden geçer — timeout'suz `Client::new()` kullanma.
fn http_client() -> Result<reqwest::Client, String> {
    reqwest::Client::builder()
        .connect_timeout(CONNECT_TIMEOUT)
        .timeout(REQUEST_TIMEOUT)
        .build()
        .map_err(|e| format!("HTTP istemcisi kurulamadı: {e}"))
}

/// Günlüğe yazılabilir uç nokta: yalnızca şema+host. Gemini anahtarı sorgu
/// dizesinde taşır — URL'i olduğu gibi loglamak anahtarı sızdırır.
fn safe_endpoint(base_url: &str) -> String {
    match reqwest::Url::parse(base_url) {
        Ok(u) => format!("{}://{}", u.scheme(), u.host_str().unwrap_or("?")),
        Err(_) => "<geçersiz URL>".into(),
    }
}

/// `reqwest::Error`'un üst mesajı sebebi göstermez ("error sending request for
/// url ...") — gerçek sebep kaynak zincirindedir (DNS, TLS, kapanan bağlantı).
/// Zinciri düzleştir; aksi halde teşhis yine körlemesine olur.
fn error_chain(e: &dyn std::error::Error) -> String {
    let mut parts = vec![e.to_string()];
    let mut src = e.source();
    while let Some(s) = src {
        parts.push(s.to_string());
        src = s.source();
    }
    parts.join(" ← ")
}

/// Ağ hatasını kullanıcının anlayacağı Türkçeye çevirir. Ham `reqwest` metni
/// ("operation timed out") sebebi göstermez; timeout'u bağlantı hatasından ayır.
fn send_error(e: reqwest::Error) -> String {
    let detail = error_chain(&e);
    if e.is_timeout() {
        format!(
            "Sağlayıcı {} saniyede yanıt vermedi — model muhtemelen yanıt üretmiyor \
             (kuyrukta veya bu hesaba servis edilmiyor). Ayarlar → AI'dan başka bir \
             model seçip yeniden deneyin. ({detail})",
            REQUEST_TIMEOUT.as_secs()
        )
    } else if e.is_connect() {
        format!("Sağlayıcıya bağlanılamadı: {detail}")
    } else {
        format!("İstek gönderilemedi: {detail}")
    }
}

/// Başarısız HTTP yanıtını okunur hataya çevirir. Sağlayıcılar hata gövdesini
/// farklı şekillerde sarar (`error.message`, `message`, `detail`, düz metin);
/// hiçbiri tutmazsa gövdeyi ham haliyle göster — "bilinmeyen hata" deme.
fn api_error(what: &str, status: reqwest::StatusCode, body: &str) -> String {
    let parsed: Option<Value> = serde_json::from_str(body).ok();
    let msg = parsed
        .as_ref()
        .and_then(|j| {
            j["error"]["message"]
                .as_str()
                .or_else(|| j["message"].as_str())
                .or_else(|| j["detail"].as_str())
                .or_else(|| j["title"].as_str())
        })
        .map(str::to_string)
        .unwrap_or_else(|| {
            let t = body.trim();
            if t.is_empty() {
                "(sağlayıcı boş gövde döndürdü)".into()
            } else {
                t.chars().take(300).collect()
            }
        });
    if status == reqwest::StatusCode::NOT_FOUND {
        return format!(
            "{what} ({status}): {msg} — Bu model bu uç noktada servis edilmiyor. \
             Ayarlar → AI'dan başka bir model seçin."
        );
    }
    format!("{what} ({status}): {msg}")
}

/// Bir mesaja iliştirilen görsel/PDF eki. `data` = base64 (prefix'siz).
#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct AiAttachment {
    /// "image" veya "document" (PDF).
    pub kind: String,
    /// ör. "image/png", "image/jpeg", "application/pdf".
    pub media_type: String,
    /// Base64 kodlu içerik (data URI prefix'i olmadan).
    pub data: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct AiMessage {
    pub role: String,
    pub content: String,
    /// Yalnızca kullanıcı mesajlarında; görsel/PDF ekleri (vision/doküman).
    #[serde(default)]
    pub attachments: Vec<AiAttachment>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct AiChatRequest {
    pub provider: String,
    pub base_url: String,
    pub api_key: String,
    pub model: String,
    pub system_prompt: String,
    /// Dosya bağlamı (güncel XSLT/XML). Sohbet mesajlarına DEĞİL, promptun
    /// kararlı önekine (system bölümü) konur — böylece prompt caching devreye
    /// girer: Anthropic'te açıkça `cache_control` ile işaretlenir, OpenAI ve
    /// Gemini'de önek değişmediği sürece otomatik/örtük cache tutar.
    /// Dosya düzenlenmediği sürece tekrar tekrar ücretlendirilmez.
    #[serde(default)]
    pub cached_context: String,
    /// Derin düşünme (extended thinking / reasoning). Frontend yalnızca
    /// destekleyen sağlayıcı+model için true gönderir (AI_PARAM_DESCRIPTORS).
    #[serde(default)]
    pub thinking: bool,
    /// Yaratıcılık; `None` = sağlayıcı varsayılanı (istekte hiç gönderilmez).
    #[serde(default)]
    pub temperature: Option<f64>,
    pub messages: Vec<AiMessage>,
}

#[tauri::command]
pub async fn ai_chat(request: AiChatRequest) -> Result<String, String> {
    // Dış ağ çağrısı: girdi boyutları, süre ve sonuç MUTLAKA loglanır. Anahtar
    // asla loglanmaz. Bu izler olmadan "yanıt gelmiyor" şikâyeti kör teşhistir.
    let attachments: usize = request.messages.iter().map(|m| m.attachments.len()).sum();
    log::info!(
        "[ai] istek — {} · {} · {} · bağlam {} bayt · {} mesaj · {} ek · thinking={} · temp={:?}",
        request.provider,
        request.model,
        safe_endpoint(&request.base_url),
        request.cached_context.len(),
        request.messages.len(),
        attachments,
        request.thinking,
        request.temperature,
    );

    let started = Instant::now();
    let result = match request.provider.as_str() {
        "anthropic" => call_anthropic(&request).await,
        "gemini" => call_gemini(&request).await,
        _ => call_openai_compatible(&request).await, // openai, ollama, nvidia, deepseek
    };
    let ms = started.elapsed().as_millis();

    match &result {
        Ok(text) => log::info!("[ai] yanıt tamam — {} bayt, {ms} ms", text.len()),
        Err(e) => log::error!("[ai] çağrı başarısız ({ms} ms): {e}"),
    }
    result
}

#[derive(Debug, Serialize, Deserialize)]
pub struct AiListModelsRequest {
    pub provider: String,
    pub base_url: String,
    pub api_key: String,
}

#[tauri::command]
pub async fn ai_list_models(request: AiListModelsRequest) -> Result<Vec<String>, String> {
    let result = match request.provider.as_str() {
        "anthropic" => list_anthropic_models(&request).await,
        "gemini" => list_gemini_models(&request).await,
        _ => list_openai_compatible_models(&request).await, // openai, ollama, nvidia, deepseek
    };
    // "Listede model eksik" şikâyeti ancak sağlayıcının NE döndürdüğü bilinirse
    // teşhis edilebilir — listeyi olduğu gibi logla (anahtar loglanmaz).
    match &result {
        Ok(models) => log::info!(
            "[ai] model listesi — {} · {} · {} model: {}",
            request.provider,
            safe_endpoint(&request.base_url),
            models.len(),
            models.join(", ")
        ),
        Err(e) => log::error!("[ai] model listesi alınamadı ({}): {e}", request.provider),
    }
    result
}

async fn list_anthropic_models(req: &AiListModelsRequest) -> Result<Vec<String>, String> {
    let client = http_client()?;
    let url = format!("{}/models", req.base_url.trim_end_matches('/'));
    let resp = client
        .get(&url)
        .header("x-api-key", &req.api_key)
        .header("anthropic-version", "2023-06-01")
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("Model listesi alınamadı", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;

    Ok(json["data"]
        .as_array()
        .map(|arr| arr.iter().filter_map(|m| m["id"].as_str().map(str::to_string)).collect())
        .unwrap_or_default())
}

async fn list_gemini_models(req: &AiListModelsRequest) -> Result<Vec<String>, String> {
    let client = http_client()?;
    let url = format!("{}/models?key={}", req.base_url.trim_end_matches('/'), req.api_key);
    let resp = client
        .get(&url)
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("Model listesi alınamadı", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;

    Ok(json["models"]
        .as_array()
        .map(|arr| {
            arr.iter()
                .filter(|m| {
                    m["supportedGenerationMethods"]
                        .as_array()
                        .map(|methods| methods.iter().any(|x| x.as_str() == Some("generateContent")))
                        .unwrap_or(true)
                })
                .filter_map(|m| m["name"].as_str().map(|s| s.trim_start_matches("models/").to_string()))
                .collect()
        })
        .unwrap_or_default())
}

async fn list_openai_compatible_models(req: &AiListModelsRequest) -> Result<Vec<String>, String> {
    let client = http_client()?;
    let url = format!("{}/models", req.base_url.trim_end_matches('/'));
    let mut builder = client.get(&url);
    if !req.api_key.is_empty() {
        builder = builder.bearer_auth(&req.api_key);
    }

    let resp = builder
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("Model listesi alınamadı", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;

    Ok(json["data"]
        .as_array()
        .map(|arr| arr.iter().filter_map(|m| m["id"].as_str().map(str::to_string)).collect())
        .unwrap_or_default())
}

async fn call_anthropic(req: &AiChatRequest) -> Result<String, String> {
    let client = http_client()?;
    let url = format!("{}/messages", req.base_url.trim_end_matches('/'));
    let messages: Vec<Value> = req
        .messages
        .iter()
        .map(|m| {
            if m.attachments.is_empty() {
                json!({ "role": m.role, "content": m.content })
            } else {
                let mut parts: Vec<Value> = Vec::new();
                if !m.content.is_empty() {
                    parts.push(json!({ "type": "text", "text": m.content }));
                }
                for a in &m.attachments {
                    let block_type = if a.kind == "document" { "document" } else { "image" };
                    parts.push(json!({
                        "type": block_type,
                        "source": { "type": "base64", "media_type": a.media_type, "data": a.data },
                    }));
                }
                json!({ "role": m.role, "content": parts })
            }
        })
        .collect();
    // System bölümü blok dizisi olarak kurulur: [statik prompt] + [dosya bağlamı].
    // Dosya bloğu `cache_control: ephemeral` ile işaretlenir → aynı dosyayla
    // yapılan sonraki isteklerde bu blok cache'ten okunur (girdi maliyetinin
    // ~%10'u). Cache, blok içeriği değişince (dosya düzenlenince) kendiliğinden
    // geçersizleşir; ayrıca cache_control yalnızca son statik blokta olmalıdır.
    let mut system_blocks: Vec<Value> = vec![json!({
        "type": "text",
        "text": req.system_prompt,
    })];
    if !req.cached_context.is_empty() {
        system_blocks.push(json!({
            "type": "text",
            "text": req.cached_context,
            "cache_control": { "type": "ephemeral" },
        }));
    } else {
        // Bağlam yoksa statik promptu cache'le (yine de tekrar tekrar ödenmesin).
        system_blocks[0]["cache_control"] = json!({ "type": "ephemeral" });
    }

    let mut body = json!({
        // Büyük XSLT şablonları tek yanıtta dönebildiğinden yüksek tutuluyor;
        // aksi halde yanıt yarıda kesilip kod bloğu kapanmıyor (Editöre Uygula
        // butonu kaybolur, sohbete kapanmamış dev kod dökülür).
        "model": req.model,
        "max_tokens": 16384,
        "system": system_blocks,
        "messages": messages,
    });
    if req.thinking {
        // Bütçe max_tokens'tan küçük olmalı; thinking açıkken Anthropic
        // temperature kabul etmez (1 olmak zorunda) — bu yüzden gönderilmez.
        body["thinking"] = json!({ "type": "enabled", "budget_tokens": 8192 });
    } else if let Some(t) = req.temperature {
        body["temperature"] = json!(t);
    }

    let resp = client
        .post(&url)
        .header("x-api-key", &req.api_key)
        .header("anthropic-version", "2023-06-01")
        .json(&body)
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("Claude API hatası", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;
    if json["stop_reason"].as_str() == Some("refusal") {
        return Err("İstek Claude tarafından güvenlik nedeniyle reddedildi.".into());
    }

    json["content"]
        .as_array()
        .and_then(|blocks| blocks.iter().find(|b| b["type"].as_str() == Some("text")))
        .and_then(|b| b["text"].as_str())
        .map(str::to_string)
        .ok_or_else(|| "Yanıtta metin bulunamadı.".into())
}

async fn call_gemini(req: &AiChatRequest) -> Result<String, String> {
    let client = http_client()?;
    let url = format!(
        "{}/models/{}:generateContent?key={}",
        req.base_url.trim_end_matches('/'),
        req.model,
        req.api_key
    );
    let contents: Vec<Value> = req
        .messages
        .iter()
        .map(|m| {
            let role = if m.role == "assistant" { "model" } else { "user" };
            let mut parts: Vec<Value> = Vec::new();
            if !m.content.is_empty() {
                parts.push(json!({ "text": m.content }));
            }
            for a in &m.attachments {
                parts.push(json!({ "inline_data": { "mime_type": a.media_type, "data": a.data } }));
            }
            if parts.is_empty() {
                parts.push(json!({ "text": "" }));
            }
            json!({ "role": role, "parts": parts })
        })
        .collect();
    let mut body = json!({
        "contents": contents,
        "generationConfig": { "maxOutputTokens": 16384 },
    });
    if let Some(t) = req.temperature {
        body["generationConfig"]["temperature"] = json!(t);
    }
    if req.thinking {
        // -1 = dinamik bütçe (model kendisi belirler). Kapalıyken hiç
        // gönderilmez → modelin varsayılan davranışı korunur (2.5 Pro'da
        // düşünme kapatılamadığından açıkça 0 göndermek hata üretirdi).
        body["generationConfig"]["thinkingConfig"] = json!({ "thinkingBudget": -1 });
    }
    // Dosya bağlamı system_instruction'a konur (kararlı önek) → Gemini'nin örtük
    // cache'i devreye girer; sohbet mesajları değişse de önek aynı kaldığı sürece
    // bu bölüm yeniden ücretlendirilmez.
    if !req.system_prompt.is_empty() || !req.cached_context.is_empty() {
        let mut parts: Vec<Value> = Vec::new();
        if !req.system_prompt.is_empty() {
            parts.push(json!({ "text": req.system_prompt }));
        }
        if !req.cached_context.is_empty() {
            parts.push(json!({ "text": req.cached_context }));
        }
        body["system_instruction"] = json!({ "parts": parts });
    }

    let resp = client
        .post(&url)
        .json(&body)
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("Gemini API hatası", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;

    json["candidates"][0]["content"]["parts"][0]["text"]
        .as_str()
        .map(str::to_string)
        .ok_or_else(|| "Yanıtta metin bulunamadı.".into())
}

/// OpenAI Chat Completions formatı — OpenAI, Ollama (yerel) ve NVIDIA NIM
/// hepsi bu formatı kullanıyor, yalnızca base_url/model/api_key değişiyor.
async fn call_openai_compatible(req: &AiChatRequest) -> Result<String, String> {
    let client = http_client()?;
    let url = format!("{}/chat/completions", req.base_url.trim_end_matches('/'));

    // Dosya bağlamı ilk (system) mesaja eklenir → promptun öneki sabit kalır ve
    // OpenAI'ın otomatik prompt cache'i (≥1024 token'lık kararlı önek) devreye
    // girer. Bağlam sohbet mesajlarına eklenirse önek her turda değişir ve cache
    // hiç tutmaz — bu yüzden burada birleştiriliyor.
    let system_content = if req.cached_context.is_empty() {
        req.system_prompt.clone()
    } else {
        format!("{}\n{}", req.system_prompt, req.cached_context)
    };
    let mut messages = vec![json!({ "role": "system", "content": system_content })];
    messages.extend(req.messages.iter().map(|m| {
        // OpenAI Chat Completions yalnızca görseli (image_url) destekler; PDF
        // bu formatta gönderilemez, sessizce atlanır (kullanıcı UI'da uyarılır).
        let images: Vec<&AiAttachment> = m.attachments.iter().filter(|a| a.kind == "image").collect();
        if images.is_empty() {
            json!({ "role": m.role, "content": m.content })
        } else {
            let mut parts: Vec<Value> = Vec::new();
            if !m.content.is_empty() {
                parts.push(json!({ "type": "text", "text": m.content }));
            }
            for a in images {
                let url = format!("data:{};base64,{}", a.media_type, a.data);
                parts.push(json!({ "type": "image_url", "image_url": { "url": url } }));
            }
            json!({ "role": m.role, "content": parts })
        }
    }));
    // DeepSeek chat completions max_tokens için 8192 üst sınırı koyar;
    // 16384 göndermek 400 invalid_request_error döndürür.
    let max_tokens = if req.provider == "deepseek" { 8192 } else { 16384 };
    let mut body = json!({ "model": req.model, "messages": messages });
    // OpenAI reasoning modelleri (o-serisi, gpt-5) `max_tokens`'ı reddeder;
    // halefi `max_completion_tokens` tüm güncel OpenAI modellerinde geçerli.
    // Ollama/NVIDIA/DeepSeek ise yalnızca `max_tokens` tanır.
    if req.provider == "openai" {
        body["max_completion_tokens"] = json!(max_tokens);
    } else {
        body["max_tokens"] = json!(max_tokens);
    }
    if let Some(t) = req.temperature {
        body["temperature"] = json!(t);
    }
    if req.thinking && req.provider == "openai" {
        // Yalnızca reasoning modelleri (o-serisi, gpt-5) kabul eder; frontend
        // bu kapıyı zaten uygular (AI_PARAM_DESCRIPTORS).
        body["reasoning_effort"] = json!("high");
    }

    let mut builder = client.post(&url).json(&body);
    if !req.api_key.is_empty() {
        builder = builder.bearer_auth(&req.api_key);
    }

    let resp = builder
        .send()
        .await
        .map_err(send_error)?;

    let status = resp.status();
    // Gövdeyi ÖNCE ham metin olarak al: hata gövdesi her zaman JSON değil
    // (HTML hata sayfası, düz metin). Doğrudan json() edersek gerçek sebep
    // "Yanıt okunamadı" arkasında kaybolur.
    let body = resp
        .text()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {}", error_chain(&e)))?;

    if !status.is_success() {
        return Err(api_error("API hatası", status, &body));
    }

    let json: Value = serde_json::from_str(&body)
        .map_err(|e| format!("Yanıt çözümlenemedi ({status}): {e}"))?;

    json["choices"][0]["message"]["content"]
        .as_str()
        .map(str::to_string)
        .ok_or_else(|| "Yanıtta metin bulunamadı.".into())
}
