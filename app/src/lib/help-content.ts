/**
 * Yardım dokümantasyonu içerik modeli.
 * Sidebar navigasyonu + arama için yapılandırılmış bölümler.
 */

export interface HelpSection {
  id: string;
  title: string;
  icon: string;
  /** Arama için düz metin (HTML olmadan) — content'ten otomatik türetilir gerekmez, elle yazılır. */
  keywords?: string;
  /** HTML içerik (basit etiketler: p, ul, li, code, kbd, table, strong, h4) */
  html: string;
}

export const HELP_SECTIONS: HelpSection[] = [
  {
    id: 'genel-bakis',
    title: 'Genel Bakış',
    icon: '📖',
    keywords: 'giriş başlangıç nedir ne işe yarar',
    html: `
      <p><strong>e-Fatura Dizayn Editörü</strong>, Türkiye e-Fatura / e-Arşiv / e-İrsaliye
      XSLT tasarım dosyalarını düzenlemek, canlı önizlemek ve yazdırmak için
      geliştirilmiş bir masaüstü uygulamasıdır.</p>
      <p>Uygulama üç ana bölgeden oluşur:</p>
      <ul>
        <li><strong>Sol panel — Snippet'ler:</strong> Hazır XSLT/HTML/UBL-TR kod parçacıkları. Tıkla veya sürükle-bırak ile editöre eklenir.</li>
        <li><strong>Orta panel — Editörler:</strong> Üstte XSLT şablonu, altta XML kaynak verisi. Syntax highlight, satır numarası, autocomplete içerir.</li>
        <li><strong>Sağ panel — Önizleme:</strong> XSLT dönüşümünün canlı HTML çıktısı. Responsive boyut ve zoom seçenekleri.</li>
      </ul>
      <p>Değişiklikler otomatik olarak (ayarlanabilir gecikmeyle) dönüştürülür ve önizlemeye yansır.</p>
    `,
  },
  {
    id: 'dosya-islemleri',
    title: 'Dosya İşlemleri',
    icon: '📂',
    keywords: 'aç kaydet farklı kaydet dosya open save',
    html: `
      <h4>Açma</h4>
      <ul>
        <li><strong>📂 XSLT</strong> — Bilgisayarından bir <code>.xslt</code>/<code>.xsl</code> dosyası açar.</li>
        <li><strong>📄 XML</strong> — Bir <code>.xml</code> kaynak dosyası açar.</li>
        <li><strong>🕒 Son ▼</strong> — Son açılan 10 dosyaya hızlı erişim. 🗑 ile listeyi temizleyebilirsin.</li>
      </ul>
      <h4>Kaydetme</h4>
      <ul>
        <li><strong>💾 Kaydet</strong> (<kbd>Cmd/Ctrl+S</kbd>) — XSLT ve XML dosyalarını <em>birlikte</em> kaydeder. Sadece değişen (kirli/dirty) dosyalar yazılır.</li>
        <li><strong>💾 Farklı</strong> — XSLT'yi yeni bir dosya adıyla kaydeder.</li>
        <li>Butonun üzerinde <strong>turuncu * işareti</strong> görürsen, kaydedilmemiş değişiklik var demektir.</li>
      </ul>
      <h4>Syntax Kontrolü</h4>
      <p>Kaydetmeden önce dosya otomatik olarak XML syntax kontrolünden geçer.
      Hata varsa <strong>dosya kaydedilmez</strong> ve imleç otomatik olarak
      hatalı satıra konumlanır.</p>
      <h4>Sekmeler</h4>
      <p>Her sekme bir <strong>çalışmadır</strong>: kendi XSLT'si, XML'i ve önizlemesi. Birden çok
      fatura/şablon üzerinde aynı anda çalışabilirsin.</p>
      <p><strong>Her panonun kendi şeridi var, ama şeritler evli:</strong> üstteki şerit şablon
      adlarını, alttaki veri adlarını gösterir; ikisi de aynı sekmeyi işaret eder. XML şeridinden
      2. sekmeyi seçersen XSLT şeridi de 2'ye geçer — şablon verisinden ayrılmaz. XSLT'yi tek başına
      açıp varsayılan veriyle eşlendiğinde XML sekmesi <em>(varsayılan veri)</em> yazar.</p>
      <ul>
        <li><kbd>Cmd/Ctrl+T</kbd> — yeni sekme (<strong>+</strong> düğmesi de aynı işi yapar).</li>
        <li><kbd>Cmd/Ctrl+1</kbd>…<kbd>9</kbd> — N'inci sekmeye git.</li>
        <li><kbd>Ctrl+Tab</kbd> / <kbd>Ctrl+Shift+Tab</kbd> — sonraki / önceki sekme.</li>
        <li>Kapatmak için <strong>×</strong> düğmesi veya sekmeye <strong>orta tık</strong>.
        Kaydedilmemiş değişiklik varsa sorar.</li>
        <li>Sekmeler sürükle-bırakla yeniden sıralanır.</li>
      </ul>
      <p>Bir dosya veya örnek yüklerken aktif sekmede <strong>kaydedilmemiş</strong> içerik varsa,
      o içerik ezilmez — dosya <strong>yeni bir sekmede</strong> açılır. Çıkışta kaydetme kontrolü
      <strong>tüm sekmeleri</strong> kapsar.</p>
    `,
  },
  {
    id: 'ornekler',
    title: 'Örnek Faturalar',
    icon: '🎲',
    keywords: 'ubl-tr örnek sample gib senaryo',
    html: `
      <p><strong>🎲 Örnek ▼</strong> menüsü, GİB (Gelir İdaresi Başkanlığı) resmi
      UBL-TR test senaryolarından 17 örnek XML içerir — 6 kategoriye ayrılmıştır:
      Temel Fatura, Ticari Fatura, Özel Faturalar, Uygulama Yanıtları,
      Sistem Yanıtları, Kullanıcı İşlemleri.</p>
      <p>Bir örnek seçtiğinde, varsayılan XSLT şablonu (<code>default.xslt</code>)
      ile birlikte yüklenir ve (ayarlarda açıksa) otomatik dönüştürülür.</p>
      <p><strong>🌟 Varsayılan</strong> seçeneği hem örnek XSLT hem örnek XML'i birlikte yükler —
      hızlı başlangıç için idealdir.</p>
      <p><strong>📄 Nakli Yekûnlü — çok sayfalı</strong> seçeneği aynı örnek faturayı
      (<code>default.xml</code>, 25 kalem) <em>sayfalayan</em> bir şablonla yükler
      (<code>default-nakli-yekun.xslt</code>): her sayfaya sabit sayıda kalem düşer, sayfanın altında
      <em>NAKLİ YEKÛN (sonraki sayfaya devir)</em>, sonraki sayfanın başında
      <em>NAKLİ YEKÛN (önceki sayfadan devir)</em> yazar; gerçek toplamlar yalnızca son sayfada basılır.
      Aynı faturayı iki şablonla açıp karşılaştırarak farkı görebilirsin.</p>
    `,
  },
  {
    id: 'toplu-test',
    title: 'Toplu Test (Regresyon)',
    icon: '🧪',
    keywords: 'toplu test regresyon batch anlık görüntü snapshot sha256 değişti bozdum mu klasör',
    html: `
      <h4>Hangi derde deva?</h4>
      <p>Editör sana <strong>tek seferde tek fatura</strong> gösterir. Şablonda yaptığın bir düzeltme
      ekrandaki faturayı düzeltirken <strong>başka bir senaryoyu bozabilir</strong> — iskontolu faturada
      hizaladığın sütun, tevkifatlı faturada kayabilir. Editörde bunu göremezsin, çünkü o faturaya
      bakmıyorsun. <strong>Toplu Test</strong> tam olarak bu kör noktayı kapatır: şablonunu bir
      klasördeki <strong>bütün</strong> faturalara karşı çalıştırır ve <em>"bir şeyi düzeltirken başka
      bir şeyi bozdum mu?"</em> sorusunu tahminle değil <strong>ölçümle</strong> yanıtlar.</p>

      <h4>Hazırlık: fatura klasörü</h4>
      <p>Bir klasöre, şablonunun karşılaşacağı <strong>farklı senaryoları</strong> temsil eden gerçek
      <code>.xml</code> faturaları koy. Klasördeki <strong>tüm <code>.xml</code> dosyaları</strong>
      (alfabetik sırayla) taranır; alt klasörlere inilmez.</p>
      <pre><code>~/Belgeler/fatura-testleri/
├── 01-temel-fatura.xml
├── 02-iskontolu.xml
├── 03-tevkifatli.xml
├── 04-istisna-kdv0.xml
├── 05-doviz-usd.xml
├── 06-25-kalemli-iki-sayfa.xml
└── 07-iade-fatura.xml</code></pre>
      <p>Windows'ta örnek: <code>C:\\Users\\ahmet\\Belgeler\\fatura-testleri</code></p>
      <p><strong>Klasörü nasıl doldurursun?</strong> En değerli test faturaları <strong>senin gerçek
      faturalarındır</strong> — özellikle geçmişte "bu fatura bozuk basılıyor" diye geri dönen örnekler.
      Her birini bu klasöre at; artık bir daha sessizce bozulamazlar.</p>
      <p>Uygulamayla gelen GİB örneklerinden de yararlanabilirsin: <strong>🎲 Örnek ▼</strong> menüsünden
      bir senaryo yükle, sonra <strong>💾 Farklı</strong> de. Bu, çifti birlikte kaydeder — <em>önce</em>
      XSLT için, <em>sonra</em> XML için pencere açılır; XML penceresinde test klasörünü göster.
      (Paketli örnekler uygulamanın içine gömülüdür; diskte gezilebilir bir klasörleri yoktur.)</p>

      <h4>Adım adım çalıştırma</h4>
      <ul>
        <li><strong>1.</strong> Test etmek istediğin <strong>XSLT şablonunu editöre yükle</strong>
        (Toplu Test, editördeki şablonu kullanır — diskteki hâlini değil, <em>o an ekranda olanı</em>).</li>
        <li><strong>2.</strong> Araç çubuğunda <strong>🧪 Toplu Test</strong> düğmesine bas.</li>
        <li><strong>3.</strong> <strong>📁 Fatura Klasörü Seç</strong> → yukarıdaki klasörü seç.
        Seçer seçmez koşu <strong>kendiliğinden başlar</strong>. Faturalar <strong>sırayla</strong>
        işlenir (aynı anda 50 dönüşüm başlatmak makineyi çökertirdi); ilerleme çubuğu hangi dosyada
        olduğunu gösterir.</li>
        <li><strong>4.</strong> Sonuç tablosu: her satırda <strong>dosya adı</strong>,
        <strong>sonuç</strong> (✓ / ✗ ve hata mesajı), <strong>süre</strong> ve <strong>çıktı boyutu</strong>.
        Başarısız satırlarda <strong>Saxon'un gerçek hata metni</strong> yazar (satır numarasıyla) —
        "bilinmeyen hata" demeyiz.</li>
        <li><strong>5.</strong> Bir satıra <strong>çift tıkla</strong> → o fatura editöre yüklenir,
        hatayı gözünle görürsün.</li>
      </ul>

      <h4>📸 Anlık Görüntü — asıl güç burada</h4>
      <p>Koşu başarılıysa <strong>📸 Anlık Görüntü Al</strong> düğmesine bas. Uygulama her faturanın
      HTML çıktısının <strong>sha256 imzasını</strong> saklar. Bu, o anki çıktının "fotoğrafıdır".</p>
      <p>Şimdi şablonda değişiklik yap, <strong>▶ Tekrar Çalıştır</strong> de. Her satır artık
      etiketlenir:</p>
      <ul>
        <li><strong>aynı</strong> — çıktı bit bit aynı. Bu faturaya <strong>dokunmadın</strong>.</li>
        <li><strong>DEĞİŞTİ</strong> — çıktı farklı. <strong>Beklediğin faturalar mı değişti?</strong>
        Sadece iskontoyu düzelttiysen ama tevkifatlı fatura da "DEĞİŞTİ" diyorsa, farkında olmadan
        bir şey bozmuşsun demektir — <em>işte yakalamak istediğin an budur.</em></li>
        <li><strong>yeni</strong> — bu fatura anlık görüntü alındığında yoktu (klasöre sonradan eklendi).</li>
      </ul>
      <p>Değişiklik <strong>kasıtlıysa</strong> yeniden <strong>📸 Anlık Görüntü Al</strong> diyerek yeni
      hâli referans yap. Böylece bir sonraki düzenlemede yalnızca <em>ondan sonraki</em> farklar görünür.</p>

      <h4>Tipik akış</h4>
      <ol>
        <li>Şablonu yükle → 🧪 Toplu Test → klasörü seç → hepsi ✓ mi, bak.</li>
        <li>📸 Anlık Görüntü Al (temiz başlangıç noktası).</li>
        <li>Şablonda değişikliği yap (örn. nakli yekûn satırının sütununu düzelt).</li>
        <li>▶ Tekrar Çalıştır → <strong>yalnızca çok kalemli faturalar "DEĞİŞTİ" demeli.</strong>
        Başkası da değiştiyse dur ve bak.</li>
        <li>Sonuç doğruysa 📸 Anlık Görüntü Al ve devam et.</li>
      </ol>

      <h4>Anlık görüntüler nerede saklanıyor?</h4>
      <p>Klasör bazında, uygulama veri klasöründeki <code>batch-baseline.json</code> dosyasında.
      Her klasörün kendi anlık görüntüsü vardır — farklı müşteri/şablon setleri birbirine karışmaz.</p>
      <ul>
        <li><strong>macOS:</strong> <code>~/Library/Application Support/com.zaferbilgisayar.efaturaedit/batch-baseline.json</code></li>
        <li><strong>Windows:</strong> <code>%APPDATA%\\com.zaferbilgisayar.efaturaedit\\batch-baseline.json</code></li>
        <li><strong>Linux:</strong> <code>~/.config/com.zaferbilgisayar.efaturaedit/batch-baseline.json</code></li>
      </ul>
      <p>Dosyayı silmek yalnızca referansı sıfırlar; faturalarına veya şablonuna hiçbir şey olmaz.</p>
    `,
  },
  {
    id: 'snippetler',
    title: "Snippet'ler",
    icon: '🧩',
    keywords: 'kod parçası ekle sürükle bırak drag drop',
    html: `
      <p>Sol paneldeki <strong>149 snippet</strong>, kategorilere ayrılmış hazır
      XSLT/HTML/UBL-TR kod parçacıklarıdır: HTML Öğeleri, XSLT Komutları,
      UBL-TR e-Fatura, e-Arşiv, e-İrsaliye ve daha fazlası.</p>
      <h4>Kullanım</h4>
      <ul>
        <li><strong>Tıkla</strong> — Snippet, XSLT editöründeki imleç konumuna eklenir.</li>
        <li><strong>Sürükle-bırak</strong> — Snippet'i tut, XSLT veya XML editörünün istediğin
        satırına sürükle, bırak. Sürüklerken mavi bir "hedef" göstergesi (📌) hedefin
        üzerinde olduğunu gösterir.</li>
        <li><strong>Arama kutusu</strong> — Snippet adı, anahtar kelime veya açıklamaya göre filtreler.</li>
        <li><strong>Kategori sekmeleri</strong> — Snippet'leri gruplarına göre gezinmeni sağlar.</li>
      </ul>
      <p>Her snippet üzerine <strong>fareyle gelirsen</strong>, tam açıklamasını
      (tooltip olarak) görebilirsin.</p>
    `,
  },
  {
    id: 'editor',
    title: 'Editör Özellikleri',
    icon: '📝',
    keywords: 'codemirror syntax autocomplete arama undo redo',
    html: `
      <h4>Syntax Highlight</h4>
      <p>XSLT/XML etiketleri, öznitelikler, değerler ve yorumlar renklendirilir.
      Açık temada özel renklendirme; koyu temalarda kendi renk paleti kullanılır.</p>
      <h4>Autocomplete (Otomatik Tamamlama)</h4>
      <ul>
        <li><kbd>&lt;</kbd> karakteri yazınca XSLT etiket önerileri (16 adet) çıkar.</li>
        <li><code>select="</code> veya <code>test="</code> içindeyken XPath önerileri (77 adet UBL-TR alanı) öncelikli gösterilir.</li>
        <li><kbd>Ctrl+Space</kbd> ile tüm öneriler (XSLT + XPath + 149 snippet) listelenir.</li>
        <li>Snippet önerileri ★ işaretiyle gösterilir; seçildiğinde tam kod eklenir.</li>
      </ul>
      <h4>Arama (Cmd/Ctrl+F)</h4>
      <p>Editör içi arama paneli Türkçeleştirilmiştir: Ara, Değiştir, Sonraki,
      Önceki, Tümü, BÜYÜK/küçük harf duyarlılığı, kelime bazlı arama, regex desteği.</p>
      <h4>Geri Al / İleri Al</h4>
      <p>Toolbar'daki <strong>↩ ↪</strong> butonları veya <kbd>Cmd/Ctrl+Z</kbd> /
      <kbd>Cmd/Ctrl+Shift+Z</kbd> ile geri al / yinele.</p>
      <h4>Kod Katlama</h4>
      <p>Satır numarası solundaki oklarla XSLT bloklarını (xsl:if, xsl:for-each vb.) katlayabilirsin.</p>
    `,
  },
  {
    id: 'onizleme',
    title: 'Önizleme',
    icon: '🖼️',
    keywords: 'preview responsive zoom yazdır pdf print',
    html: `
      <h4>Responsive Boyutlar</h4>
      <p>Sağ üstteki butonlarla önizlemeyi farklı ekran genişliklerinde test edebilirsin:</p>
      <ul>
        <li><strong>📱 320</strong> — Mobil genişlik</li>
        <li><strong>📱 768</strong> — Tablet genişlik</li>
        <li><strong>🖥️ 1200</strong> — Masaüstü genişlik</li>
        <li><strong>⬜ Full</strong> — Panel genişliği kadar (varsayılan)</li>
      </ul>
      <h4>Zoom</h4>
      <p><strong>− / % / +</strong> butonları veya <kbd>Cmd/Ctrl +</kbd> / <kbd>Cmd/Ctrl −</kbd>
      ile yakınlaştır/uzaklaştır. <kbd>Cmd/Ctrl+0</kbd> ile %100'e sıfırla.</p>
      <h4>Yazdırma ve PDF</h4>
      <p><strong>🖨</strong> butonu (veya <kbd>Cmd/Ctrl+P</kbd>) önizlemeyi geçici bir
      HTML dosyası olarak sistem tarayıcısında (Safari) açar. Orada
      <kbd>Cmd+P</kbd> ile native yazdırma penceresini açıp
      <strong>"PDF olarak Kaydet"</strong> seçeneğini kullanabilirsin.</p>
      <p><em>Not: Tauri'nin uygulama içi WebView'i güvenilir native yazdırma
      desteklemediği için bu adım tarayıcıya devredilmiştir.</em></p>
      <h4>Sağ Tık Menüsü</h4>
      <p>Önizleme üzerinde sağ tıklayarak: Yazdır/PDF, HTML kopyala, yeniden dönüştür
      ve Geliştirici Araçları (DevTools) seçeneklerine ulaşabilirsin.</p>
    `,
  },
  {
    id: 'kisayollar',
    title: 'Klavye Kısayolları',
    icon: '⌨️',
    keywords: 'shortcut kbd tuş kombinasyonu',
    html: `
      <table class="help-table">
        <tr><th>Kısayol</th><th>İşlev</th></tr>
        <tr><td><kbd>Cmd/Ctrl + S</kbd></td><td>XSLT + XML kaydet (sözdizimi hatası yoksa)</td></tr>
        <tr><td><kbd>Cmd/Ctrl + R</kbd></td><td>Dönüştür (önizlemeyi yenile)</td></tr>
        <tr><td><kbd>Cmd/Ctrl + P</kbd></td><td>Yazdır (tarayıcıda aç)</td></tr>
        <tr><td><kbd>Cmd/Ctrl + F</kbd></td><td>Editörde ara (editör odaktayken)</td></tr>
        <tr><td><kbd>Cmd/Ctrl + Z</kbd></td><td>Geri al</td></tr>
        <tr><td><kbd>Cmd/Ctrl + Shift + Z</kbd></td><td>İleri al (yinele)</td></tr>
        <tr><td><kbd>Cmd/Ctrl + +</kbd></td><td>Önizlemeyi yakınlaştır</td></tr>
        <tr><td><kbd>Cmd/Ctrl + −</kbd></td><td>Önizlemeyi uzaklaştır</td></tr>
        <tr><td><kbd>Cmd/Ctrl + 0</kbd></td><td>Önizleme zoom'unu sıfırla (%100)</td></tr>
        <tr><td><kbd>Ctrl + Space</kbd></td><td>Autocomplete tam liste</td></tr>
        <tr><td><kbd>Esc</kbd></td><td>Sürüklemeyi iptal et / menüyü kapat</td></tr>
        <tr><td><kbd>F1</kbd></td><td>Bu yardım penceresini aç</td></tr>
      </table>
    `,
  },
  {
    id: 'ayarlar',
    title: 'Ayarlar',
    icon: '⚙️',
    keywords: 'settings tema font autosave debounce',
    html: `
      <p>Toolbar'daki <strong>⚙️ Ayarlar</strong> butonundan ulaşılan sayfa
      şu kategorileri içerir:</p>
      <h4>Editör</h4>
      <ul>
        <li><strong>Yazı Tipi Boyutu</strong> — 10-24px arası</li>
        <li><strong>Sekme Genişliği</strong> — 2/4/8 boşluk</li>
        <li><strong>Kelime Kaydırma</strong> — uzun satırları sar</li>
        <li><strong>Satır Numarası</strong> — göster/gizle</li>
      </ul>
      <h4>Görünüm</h4>
      <p><strong>Tema</strong> — 11 seçenek: Açık (varsayılan), One Dark, Dracula,
      Cobalt, Espresso, Solarized Light, Ayu Light, Noctis Lilac, Rosé Pine Dawn,
      Clouds, Smoothy.</p>
      <h4>Davranış</h4>
      <ul>
        <li><strong>Otomatik dönüştür (yükleme)</strong> — dosya/örnek açılınca otomatik önizleme.</li>
        <li><strong>Otomatik dönüştür (kaydetme)</strong> — kaydettikten sonra otomatik önizleme.</li>
        <li><strong>Autocomplete</strong> — açık/kapalı.</li>
      </ul>
      <p>Tüm ayarlar tarayıcı belleğinde (localStorage) saklanır ve uygulama
      yeniden açıldığında korunur. <strong>Varsayılana Sıfırla</strong> ile
      tüm ayarları fabrika değerlerine döndürebilirsin.</p>
    `,
  },
  {
    id: 'panel-boyutlari',
    title: 'Panel Boyutları',
    icon: '↔️',
    keywords: 'splitter resize boyutlandır bölme',
    html: `
      <p>Panel sınırlarındaki ince çizgileri fareyle sürükleyerek boyutlarını
      değiştirebilirsin:</p>
      <ul>
        <li><strong>Snippet paneli ↔ Editör kolonu</strong> arasındaki dikey çizgi</li>
        <li><strong>Editör kolonu ↔ Önizleme</strong> arasındaki dikey çizgi</li>
        <li><strong>XSLT editörü ↔ XML editörü</strong> arasındaki yatay çizgi</li>
      </ul>
      <p>Fare imleci çizginin üzerine geldiğinde <strong>mavi</strong> renk alır —
      bu, sürüklenebilir olduğunun işaretidir. Boyutlar otomatik olarak kaydedilir.</p>
    `,
  },
  {
    id: 'sorun-giderme',
    title: 'Sorun Giderme',
    icon: '🔧',
    keywords: 'hata error problem çözüm troubleshooting',
    html: `
      <h4>"XML ayrıştırılamadı" hatası</h4>
      <p>XML veya XSLT dosyanızda bir sözdizimi hatası var. İmleç otomatik
      olarak hatalı satıra konumlanır — satır numarasına bakarak düzeltin.
      Yaygın nedenler: kapatılmamış etiket, eşleşmeyen tırnak, geçersiz karakter.</p>
      <h4>Dönüşüm hiçbir şey üretmiyor</h4>
      <p>XSLT şablonunun kök şablonu (<code>&lt;xsl:template match="/"&gt;</code>)
      olduğundan emin olun. XML kaynağının kök elemanı XSLT'nin beklediği
      namespace ile eşleşmelidir (örn. UBL-TR için <code>n1:Invoice</code>).</p>
      <h4>Yazdırma çalışmıyor</h4>
      <p>Yazdırma, önizlemeyi sistem tarayıcısında (Safari) açar. Tarayıcı
      açılmıyorsa, işletim sisteminde varsayılan tarayıcının ayarlı olduğundan
      emin olun. Tarayıcıda açıldıktan sonra <kbd>Cmd+P</kbd> ile yazdırın.</p>
      <h4>Sürükle-bırak çalışmıyor</h4>
      <p>Snippet'i editöre bırakırken fareyi <strong>editör alanının içinde</strong>
      bırakmalısınız. Sürüklerken görünen küçük etiket (👻 ghost) hedef editörün
      üzerindeyken mavi renge döner — bu, bırakabileceğinizin işaretidir.</p>
      <h4>Autocomplete önerileri gelmiyor</h4>
      <p>Ayarlar sayfasında "Autocomplete" seçeneğinin açık olduğunu kontrol edin.
      Ayrıca <kbd>Ctrl+Space</kbd> ile manuel tetikleyebilirsiniz.</p>
      <h4>Değişiklikler kaybolmuş gibi görünüyor</h4>
      <p>Ayarlar sayfasına gidip geri döndüğünüzde editör içeriği korunur
      (global state). Eğer içerik gerçekten kayboldu ise, kaydetmeden önce
      pencereyi kapatmış olabilirsiniz — "Son Açılanlar" listesinden dosyayı
      tekrar açabilirsiniz.</p>
    `,
  },
  {
    id: 'hakkinda',
    title: 'Hakkında',
    icon: 'ℹ️',
    keywords: 'version sürüm lisans about',
    html: `
      <p><strong>e-Fatura Dizayn Editörü</strong> — Zafer Bilgisayar</p>
      <p>Bu yazılım ile Türkiye e-Fatura/e-Arşiv/e-İrsaliye XSLT tasarım
      dosyalarınızı düzenleyebilir ve görsel çıktıyı anlık izleyebilirsiniz.</p>
      <p>Kullanılan açık kaynak bileşenler: Tauri, SvelteKit, CodeMirror 6.</p>
      <p>Sorularınız için: <a href="mailto:hzkucuk@gmail.com">hzkucuk@gmail.com</a></p>
    `,
  },
];

/** Bölüm HTML'inden etiketleri temizleyip düz metin arama için normalize eder. */
export function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
}

/** Arama terimine göre bölümleri filtreler (başlık, anahtar kelime, içerik). */
export function searchHelpSections(term: string): HelpSection[] {
  const q = term.trim().toLowerCase();
  if (!q) return HELP_SECTIONS;
  return HELP_SECTIONS.filter((s) => {
    const haystack = `${s.title} ${s.keywords ?? ''} ${stripHtml(s.html)}`.toLowerCase();
    return haystack.includes(q);
  });
}
