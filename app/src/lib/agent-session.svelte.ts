/**
 * Klasör Ajanı — modül-düzeyi durum + onay-kapılı araç döngüsü.
 *
 * "Öneri" modundan farkı: model ARAÇ çağırır (dosya oku/yaz/düzenle/listele) ve
 * döngü frontend'te dönerken her araç, çalıştırılmadan önce onaydan geçer.
 *
 * <b>Neden MODÜLDE (bileşende değil):</b> uçuştaki tur, bekleyen onay ve güven
 * kümesi pano/sayfa geçişinde ölmemeli (proje dersi: unmount'ta buharlaşan AI
 * yanıtı). Durum burada `$state` olarak yaşar; UI yalnızca gösterir/tetikler.
 *
 * <b>Güvenlik:</b> Her dosya yolu Rust'ta `guard()` ile çalışma köküne kilitlenir
 * (agent_tools.rs). Bu döngü kökü her `invoke`'a geçirir ama asıl koruma Rust
 * sınırındadır — buradaki kod onu atlayamaz.
 */
import { invoke } from '@tauri-apps/api/core';
import { settings, updateSetting } from '$lib/settings.svelte';

/** Ajan döngüsünün üst sınırı (sonsuz araç döngüsünü kes). */
const MAX_ITERS = 25;

// ─── Nötr blok tipleri (Rust AgentBlock ile birebir) ──────────────────────
type Block =
  | { type: 'text'; text: string }
  | { type: 'tool_use'; id: string; name: string; input: Record<string, unknown> }
  | { type: 'tool_result'; tool_use_id: string; content: string; is_error: boolean; name: string };

type Decision = 'allow' | 'deny' | 'always' | 'trust_folder';

interface AgentMsg {
  role: 'user' | 'assistant';
  blocks: Block[];
}

interface ToolCall {
  id: string;
  name: string;
  input: Record<string, unknown>;
}

/** UI'da gösterilecek olay akışı (kullanıcı+ajan+araç kartları). */
export interface FeedItem {
  kind: 'user' | 'assistant' | 'tool' | 'error';
  text: string;
  /** Araç kartları için: araç adı + hedef/özet. */
  tool?: { name: string; summary: string; ok?: boolean };
}

/** Bekleyen onay — UI modalı bunu gösterir, karar verince `resolve` çağrılır. */
export interface PendingApproval {
  call: ToolCall;
  summary: string;
  resolve: (decision: Decision) => void;
}

export const agent = $state({
  /** Çalışma klasörü (sandbox kökü). Boşsa mod kullanılamaz. */
  root: '' as string,
  running: false,
  messages: [] as AgentMsg[],
  feed: [] as FeedItem[],
  pending: null as PendingApproval | null,
  /** Bu oturumda "hep izin ver" denen araç adları. */
  trusted: new Set<string>(),
  error: '',
});

// ─── Araç tanımları (sağlayıcıdan bağımsız; Rust'a olduğu gibi geçer) ──────
const TOOLS = [
  {
    name: 'read_file',
    description:
      'Çalışma klasöründeki bir dosyayı oku. Düzenlemeden önce dosyanın güncel içeriğini gör.',
    input_schema: {
      type: 'object',
      properties: { path: { type: 'string', description: 'Köke göreli dosya yolu' } },
      required: ['path'],
    },
  },
  {
    name: 'list_dir',
    description: 'Çalışma klasöründeki (veya bir alt dizindeki) dosya ve klasörleri listele.',
    input_schema: {
      type: 'object',
      properties: {
        sub: { type: 'string', description: 'Köke göreli alt dizin; kök için boş bırak' },
      },
    },
  },
  {
    name: 'edit_file',
    description:
      'Bir dosyada hedefli metin değişimi. `old` dosyada TAM OLARAK BİR KEZ geçmeli (benzersiz olacak kadar bağlam ver). Küçük, hedefli değişiklikler için tercih et.',
    input_schema: {
      type: 'object',
      properties: {
        path: { type: 'string', description: 'Köke göreli dosya yolu' },
        old: { type: 'string', description: 'Değiştirilecek mevcut metin (birebir)' },
        new: { type: 'string', description: 'Yeni metin' },
      },
      required: ['path', 'old', 'new'],
    },
  },
  {
    name: 'write_file',
    description:
      'Bir dosyanın TAMAMINI yaz (yoksa oluşturur). Yeni dosya için veya küçük dosyaları baştan yazmak için kullan; büyük dosyalarda edit_file tercih et.',
    input_schema: {
      type: 'object',
      properties: {
        path: { type: 'string', description: 'Köke göreli dosya yolu' },
        content: { type: 'string', description: 'Dosyanın tam yeni içeriği' },
      },
      required: ['path', 'content'],
    },
  },
  {
    name: 'run_bash',
    description:
      'Çalışma klasöründe bir kabuk komutu çalıştır (cwd = kök). Derleme, test, git, dizin arama gibi işler için. Komut kullanıcı onayından geçer; yıkıcı komutlardan kaçın.',
    input_schema: {
      type: 'object',
      properties: { command: { type: 'string', description: 'Çalıştırılacak kabuk komutu' } },
      required: ['command'],
    },
  },
  {
    name: 'run_xslt',
    description:
      'Bir XSLT şablonunu bir XML verisine uygula ve render edilmiş HTML çıktısını gör (Saxon-HE, XSLT 1.0/2.0/3.0). Tasarımın GERÇEKTE ne bastığını doğrulamak için: örn. bir öğenin kaç kez basıldığını saymak, bir değişikliğin çıktıyı bozup bozmadığını görmek.',
    input_schema: {
      type: 'object',
      properties: {
        xslt_path: { type: 'string', description: 'Köke göreli XSLT dosya yolu' },
        xml_path: { type: 'string', description: 'Köke göreli XML dosya yolu' },
      },
      required: ['xslt_path', 'xml_path'],
    },
  },
];

const SYSTEM_PROMPT = `Sen bir kodlama ajanısın: bir çalışma klasörü içinde dosya okur, düzenler ve oluşturursun. Bu proje ağırlıklı olarak Türkiye e-Fatura / e-Arşiv / e-İrsaliye (UBL-TR) XSLT tasarımı ve XML verisi içerir, ama klasördeki her tür dosyayla çalışabilirsin.

KURALLAR:
- Yalnızca çalışma klasörünün İÇİNDE çalış. Kök dışına çıkmaya çalışma; sistem bunu zaten reddeder.
- Bir dosyayı değiştirmeden ÖNCE read_file ile güncel içeriğini oku. edit_file'da 'old' metni dosyadan BİREBİR kopyala; benzersiz olacak kadar bağlam ekle (aksi halde "birden çok eşleşme" hatası alırsın).
- Küçük, hedefli değişiklikler için edit_file; yeni/küçük dosya için write_file kullan. Büyük dosyaları write_file ile baştan yazma.
- Kullanıcının isteğini yerine getirmek için gereken araçları çağır; işi tahminle bitmiş sayma, dosyayı okuyup doğrula.
- İş bittiğinde ne yaptığını KISACA özetle (hangi dosyalar değişti). Her araç kullanıcının onayından geçer.
- UBL-TR/XSLT bağlamı: dönüşüm motoru Saxon-HE (XSLT 1.0/2.0/3.0). İmza (ds:Signature / XAdES) ASLA değiştirilmez.`;

/** UI/pano geçişinde çağrılır: kökü ayarla (sandbox). */
export function setAgentRoot(root: string): void {
  agent.root = root;
}

/** Onay iste — UI modalı `agent.pending` üzerinden gösterir. */
function requestApproval(call: ToolCall, summary: string): Promise<Decision> {
  return new Promise((resolve) => {
    agent.pending = { call, summary, resolve };
  });
}

/** UI modalının çağırdığı karar fonksiyonu. */
export function resolveApproval(decision: Decision): void {
  const p = agent.pending;
  if (!p) return;
  agent.pending = null;
  p.resolve(decision);
}

/** Bir araç çağrısı için insanca özet (onay kartında + akışta gösterilir). */
function toolSummary(call: ToolCall): string {
  const i = call.input as Record<string, string>;
  switch (call.name) {
    case 'read_file':
      return `oku: ${i.path}`;
    case 'list_dir':
      return `listele: ${i.sub || '(kök)'}`;
    case 'edit_file':
      return `düzenle: ${i.path}`;
    case 'write_file':
      return `yaz: ${i.path}`;
    case 'run_bash':
      return `komut: ${i.command}`;
    case 'run_xslt':
      return `dönüştür: ${i.xslt_path} × ${i.xml_path}`;
    default:
      return call.name;
  }
}

/** Onaylı bir araç çağrısını Rust'ta çalıştır. */
async function execTool(call: ToolCall): Promise<{ content: string; is_error: boolean }> {
  const root = agent.root;
  const i = call.input as Record<string, string>;
  try {
    switch (call.name) {
      case 'read_file':
        return { content: await invoke<string>('agent_read', { root, path: i.path }), is_error: false };
      case 'list_dir': {
        const entries = await invoke('agent_list', { root, sub: i.sub ?? '' });
        return { content: JSON.stringify(entries), is_error: false };
      }
      case 'edit_file':
        await invoke('agent_edit', { root, path: i.path, old: i.old, new: i.new });
        return { content: `Düzenlendi: ${i.path}`, is_error: false };
      case 'write_file':
        await invoke('agent_write', { root, path: i.path, content: i.content });
        return { content: `Yazıldı: ${i.path}`, is_error: false };
      case 'run_bash': {
        const r = await invoke<{ stdout: string; stderr: string; code: number; truncated: boolean }>(
          'agent_bash',
          { root, command: i.command },
        );
        const parts = [`(çıkış kodu ${r.code})`];
        if (r.stdout.trim()) parts.push(`stdout:\n${r.stdout}`);
        if (r.stderr.trim()) parts.push(`stderr:\n${r.stderr}`);
        return { content: parts.join('\n'), is_error: r.code !== 0 };
      }
      case 'run_xslt': {
        // İki dosyayı guard'lı oku, sidecar'dan geçir, HTML'i modele döndür.
        const xslt = await invoke<string>('agent_read', { root, path: i.xslt_path });
        const xml = await invoke<string>('agent_read', { root, path: i.xml_path });
        const html = await invoke<string>('xslt_transform', { xslt, xml });
        const capped = html.length > 40000 ? html.slice(0, 40000) + '\n…[kırpıldı]' : html;
        return { content: `Render edilen HTML (${html.length} bayt):\n${capped}`, is_error: false };
      }
      default:
        return { content: `Bilinmeyen araç: ${call.name}`, is_error: true };
    }
  } catch (e) {
    return { content: String((e as Error)?.message ?? e), is_error: true };
  }
}

/** Bir kullanıcı mesajıyla ajan döngüsünü sür. */
export async function runAgent(userText: string): Promise<void> {
  if (agent.running || !agent.root) return;
  const cfg = settings.aiProviders[settings.aiProvider];
  if (settings.aiProvider !== 'ollama' && !cfg.apiKey.trim()) {
    agent.error = 'API anahtarı gerekli (Ayarlar → AI).';
    return;
  }

  agent.error = '';
  agent.messages.push({ role: 'user', blocks: [{ type: 'text', text: userText }] });
  agent.feed.push({ kind: 'user', text: userText });
  agent.running = true;

  try {
    for (let iter = 0; iter < MAX_ITERS; iter++) {
      const resp = await invoke<{
        text: string;
        tool_calls: ToolCall[];
        stop_reason: string;
      }>('ai_agent', {
        request: {
          provider: settings.aiProvider,
          base_url: cfg.baseUrl,
          api_key: cfg.apiKey,
          model: cfg.model,
          system_prompt: SYSTEM_PROMPT,
          cached_context: '',
          tools: TOOLS,
          messages: agent.messages,
        },
      });

      // Asistan turunu geçmişe yaz (metin + tool_use blokları).
      const aBlocks: Block[] = [];
      if (resp.text.trim()) {
        aBlocks.push({ type: 'text', text: resp.text });
        agent.feed.push({ kind: 'assistant', text: resp.text });
      }
      for (const tc of resp.tool_calls) {
        aBlocks.push({ type: 'tool_use', id: tc.id, name: tc.name, input: tc.input });
      }
      agent.messages.push({ role: 'assistant', blocks: aBlocks });

      if (resp.tool_calls.length === 0) break; // end_turn / bitti

      // Her araç çağrısını onaydan geçirip çalıştır, sonuçları topla.
      const results: Block[] = [];
      for (const tc of resp.tool_calls) {
        const summary = toolSummary(tc);
        // Klasöre kalıcı güven veya bu oturumda "hep izin" → onay modalı atlanır.
        const folderTrusted = settings.agentTrustedFolders.includes(agent.root);
        let decision: Decision =
          folderTrusted || agent.trusted.has(tc.name) ? 'allow' : await requestApproval(tc, summary);
        if (decision === 'trust_folder') {
          if (!settings.agentTrustedFolders.includes(agent.root)) {
            updateSetting('agentTrustedFolders', [...settings.agentTrustedFolders, agent.root]);
          }
          decision = 'allow';
        }
        if (decision === 'always') {
          agent.trusted.add(tc.name);
          decision = 'allow';
        }
        if (decision === 'deny') {
          agent.feed.push({ kind: 'tool', text: '', tool: { name: tc.name, summary, ok: false } });
          results.push({
            type: 'tool_result',
            tool_use_id: tc.id,
            content: 'Kullanıcı bu işlemi reddetti.',
            is_error: true,
            name: tc.name,
          });
          continue;
        }
        const r = await execTool(tc);
        agent.feed.push({
          kind: 'tool',
          text: '',
          tool: { name: tc.name, summary, ok: !r.is_error },
        });
        results.push({
          type: 'tool_result',
          tool_use_id: tc.id,
          content: r.content,
          is_error: r.is_error,
          name: tc.name,
        });
      }
      agent.messages.push({ role: 'user', blocks: results });
    }
  } catch (e) {
    agent.error = String((e as Error)?.message ?? e);
    agent.feed.push({ kind: 'error', text: agent.error });
  } finally {
    agent.running = false;
    agent.pending = null;
  }
}

/** Sohbeti sıfırla (klasör ve güven korunur). */
export function resetAgentChat(): void {
  if (agent.running) return;
  agent.messages = [];
  agent.feed = [];
  agent.error = '';
}
