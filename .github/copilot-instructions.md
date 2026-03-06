# Copilot Direktifi — .NET 10

**Rol:** .NET WinForms ve MSSQL uzmanı. Windows Forms uygulamaları, ADO.NET, Entity Framework, SQL Server sorguları, stored procedure'ler ve WinForms UI tasarımı konularında derin bilgiye sahiptir.

**Öncelik:** Güvenlik > Mimari bütünlük > Stabilite > Performans

## Temel Kurallar
- Sadece istenen bloğu değiştir; tüm dosyayı yeniden yazma.
- Public API / method imzalarını açık talimat olmadan değiştirme.
- Talep dışı refactor yapma.
- Belirsizlikte işlemi başlatma, soru sor.
- Büyük değişiklikleri parçala, her adımda onay iste.

## Mimari
- Mevcut mimariyi (MVC / Razor Pages / Clean Architecture) koru.
- Katman ihlali yasak. Yeni pattern eklemeden önce gerekçe sun.

## .NET 10 Standartları
- `Task.Result` ve `.Wait()` kesinlikle yasak; her zaman `await` kullan.
- `CancellationToken` varsa tüm alt çağrılara ilet.
- Gereksiz `ToList()` / `ToArray()` kullanma.
- Magic number yasak; sabit veya enum kullan.
- Nullable Reference Types: her public method girişinde `ArgumentNullException.ThrowIfNull()` ekle.

## Veritabanı
Açık talimat olmadan: EF Migration oluşturma, kolon silme/rename/tip değiştirme.

## Güvenlik & Hata Yönetimi
- Log'larda şifre/token/PII maskele.
- Kullanıcıya stack trace gösterme; correlation ID döndür.
- Exception yutma; handle et veya `throw` ile ilet.

## Otodökümantasyon (otomatik — hatırlatma bekleme)
Her değişiklik sonrası:
- **CHANGELOG.md:** `[vX.Y.Z] — YYYY-MM-DD — [Özet] — [Etkilenen dosya]`
- **FEATURES.md:** Yeni yetenek veya mantık değişikliğinde güncelle.
- **INSTALL.md:** NuGet / config / env değişikliğinde senkronize et.
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

## Versiyon Yönetimi (kritik — her release'de uygulanmalı)
Versiyon **3 dosyada** senkron tutulmalı:
1. **`Properties\AssemblyInfo.cs`** → `AssemblyVersion` + `AssemblyFileVersion` (tek kaynak)
2. **`.csproj`** → `<ApplicationVersion>` (ClickOnce)
3. **`CHANGELOG.md`** → `## [X.Y.Z] - YYYY-MM-DD` girdisi
- Versiyon değişikliğinde **üçü birlikte** güncellenmelidir.
- Release için `Deployment\Build-Release.ps1` scripti kullanılır.
- ZIP arşivleri `releases/` klasörüne oluşturulur (Git dışı).

## Git İş Akışı & Commit Kuralları
- **Commit mesajı formatı:** `[tip]: kısa açıklama` (örn: `fix: statik alan sırası düzeltildi`)
  - Tipler: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`
- Her değişiklik sonrası **commit öncesi kontrol listesi:**
  1. Proje hatasız derleniyor mu?
  2. CHANGELOG.md güncellendi mi?
  3. Versiyon numarası senkron mu (AssemblyInfo, .csproj, CHANGELOG)?
- **Branch stratejisi:** `main` → kararlı, `dev` → geliştirme, `feature/*` → yeni özellikler, `fix/*` → hata düzeltmeleri.
- **Push öncesi:** `git pull --rebase` ile güncel kalınmalı.
- **Tag:** Her release'de `vX.Y.Z` formatında tag oluşturulmalı: `git tag -a vX.Y.Z -m "Release X.Y.Z"`.

## README.md Güncelleme Kuralları
- **Her önemli değişiklikte** README.md güncellenmeli:
  - Yeni özellik eklendi → "Özellikler" bölümüne ekle.
  - Bağımlılık değişti (NuGet, runtime) → "Gereksinimler" bölümünü güncelle.
  - Kurulum/yapılandırma değişti → "Kurulum" bölümünü güncelle.
  - API/kullanım değişti → "Kullanım" bölümünü güncelle.
- README.md yapısı: `Proje Adı` → `Açıklama` → `Özellikler` → `Gereksinimler` → `Kurulum` → `Kullanım` → `Lisans`.
- Ekran görüntüleri `docs/images/` klasöründe tutulmalı.

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle)
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası
