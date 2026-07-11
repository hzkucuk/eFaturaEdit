//! AI sağlayıcı köprüsü — Claude, ChatGPT, Gemini, Ollama, NVIDIA NIM.
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
    pub messages: Vec<AiMessage>,
}

#[tauri::command]
pub async fn ai_chat(request: AiChatRequest) -> Result<String, String> {
    match request.provider.as_str() {
        "anthropic" => call_anthropic(&request).await,
        "gemini" => call_gemini(&request).await,
        _ => call_openai_compatible(&request).await, // openai, ollama, nvidia
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
        _ => list_openai_compatible_models(&request).await, // openai, ollama, nvidia
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
    let body = json!({
        // Büyük XSLT şablonları tek yanıtta dönebildiğinden yüksek tutuluyor;
        // aksi halde yanıt yarıda kesilip kod bloğu kapanmıyor (Editöre Uygula
        // butonu kaybolur, sohbete kapanmamış dev kod dökülür).
        "model": req.model,
        "max_tokens": 16384,
        "system": req.system_prompt,
        "messages": messages,
    });

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
    if !req.system_prompt.is_empty() {
        body["system_instruction"] = json!({ "parts": [{ "text": req.system_prompt }] });
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

    let mut messages = vec![json!({ "role": "system", "content": req.system_prompt })];
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
    let body = json!({ "model": req.model, "messages": messages, "max_tokens": 16384 });

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
