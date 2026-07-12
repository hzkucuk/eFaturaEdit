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
    match request.provider.as_str() {
        "anthropic" => call_anthropic(&request).await,
        "gemini" => call_gemini(&request).await,
        _ => call_openai_compatible(&request).await, // openai, ollama, nvidia, deepseek
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct AiListModelsRequest {
    pub provider: String,
    pub base_url: String,
    pub api_key: String,
}

#[tauri::command]
pub async fn ai_list_models(request: AiListModelsRequest) -> Result<Vec<String>, String> {
    match request.provider.as_str() {
        "anthropic" => list_anthropic_models(&request).await,
        "gemini" => list_gemini_models(&request).await,
        _ => list_openai_compatible_models(&request).await, // openai, ollama, nvidia, deepseek
    }
}

async fn list_anthropic_models(req: &AiListModelsRequest) -> Result<Vec<String>, String> {
    let client = reqwest::Client::new();
    let url = format!("{}/models", req.base_url.trim_end_matches('/'));
    let resp = client
        .get(&url)
        .header("x-api-key", &req.api_key)
        .header("anthropic-version", "2023-06-01")
        .send()
        .await
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "Model listesi alınamadı ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }

    Ok(json["data"]
        .as_array()
        .map(|arr| arr.iter().filter_map(|m| m["id"].as_str().map(str::to_string)).collect())
        .unwrap_or_default())
}

async fn list_gemini_models(req: &AiListModelsRequest) -> Result<Vec<String>, String> {
    let client = reqwest::Client::new();
    let url = format!("{}/models?key={}", req.base_url.trim_end_matches('/'), req.api_key);
    let resp = client
        .get(&url)
        .send()
        .await
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "Model listesi alınamadı ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }

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
    let client = reqwest::Client::new();
    let url = format!("{}/models", req.base_url.trim_end_matches('/'));
    let mut builder = client.get(&url);
    if !req.api_key.is_empty() {
        builder = builder.bearer_auth(&req.api_key);
    }

    let resp = builder
        .send()
        .await
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "Model listesi alınamadı ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }

    Ok(json["data"]
        .as_array()
        .map(|arr| arr.iter().filter_map(|m| m["id"].as_str().map(str::to_string)).collect())
        .unwrap_or_default())
}

async fn call_anthropic(req: &AiChatRequest) -> Result<String, String> {
    let client = reqwest::Client::new();
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
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "Claude API hatası ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }
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
    let client = reqwest::Client::new();
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
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "Gemini API hatası ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }

    json["candidates"][0]["content"]["parts"][0]["text"]
        .as_str()
        .map(str::to_string)
        .ok_or_else(|| "Yanıtta metin bulunamadı.".into())
}

/// OpenAI Chat Completions formatı — OpenAI, Ollama (yerel) ve NVIDIA NIM
/// hepsi bu formatı kullanıyor, yalnızca base_url/model/api_key değişiyor.
async fn call_openai_compatible(req: &AiChatRequest) -> Result<String, String> {
    let client = reqwest::Client::new();
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
        .map_err(|e| format!("İstek gönderilemedi: {e}"))?;

    let status = resp.status();
    let json: Value = resp
        .json()
        .await
        .map_err(|e| format!("Yanıt okunamadı: {e}"))?;

    if !status.is_success() {
        return Err(format!(
            "API hatası ({status}): {}",
            json["error"]["message"].as_str().unwrap_or("bilinmeyen hata")
        ));
    }

    json["choices"][0]["message"]["content"]
        .as_str()
        .map(str::to_string)
        .ok_or_else(|| "Yanıtta metin bulunamadı.".into())
}
