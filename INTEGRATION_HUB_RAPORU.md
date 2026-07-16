# Integration Hub (Entegrasyon Merkezi) — Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — ASP.NET Core 8 modüler monolit
**Kapsam:** GMS'in tek ve merkezi dış entegrasyon altyapısı — güvenli, yeniden kullanılabilir provider/adaptör mimarisi.
**Durum:** Tamamlandı. Derleme **0 hata / 0 uyarı**. **89 otomatik test yeşil** (65 mevcut + 24 yeni). Gerçek SQL Server ve yerel mock HTTP sunucu ile **27 adımlık uçtan uca senaryo başarılı**.

---

## 1. Yapılan Geliştirmeler

- **8 domain entity** (IntegrationDefinition, IntegrationCredential, IntegrationEndpoint, IntegrationSubscription, IntegrationExecution, IntegrationExecutionAttempt, IntegrationEvent, ExternalObjectLink) + EF yapılandırması + indeksler.
- **Provider/adaptör mimarisi:** `IIntegrationProvider` arayüzü + DI tabanlı `IIntegrationProviderResolver` + 6 provider (GenericRest, IncomingWebhook, OutgoingWebhook, DummySmtp, AzureDevOpsSandbox, JiraSandbox).
- **Secret koruma:** `ISecretProtector` + ASP.NET Core Data Protection tabanlı `DataProtectionSecretProtector` (şifreleme + maskeleme).
- **Servis katmanı:** `IIntegrationService` (CRUD/credential/test/webhook/link), `IIntegrationDispatcher` (outbox işleyici + retry), `IIntegrationEventPublisher` (domain olay dikişi).
- **HTTP altyapısı:** `IHttpClientFactory` tabanlı tipli `IntegrationHttpClient` (timeout, redirect kapalı, yanıt gövdesi sınırlı, transient sınıflandırma) + `IIntegrationDelayStrategy` (üstel/immediate).
- **3 controller:** `IntegrationsController`, `IntegrationExecutionsController`, `IntegrationWebhooksController` (anonim, sertleştirilmiş).
- **Entegrasyonlar:** Change external-links, Workflow tetik temeli (flag), Notification (5 şablon), Unified Audit (INTEGRATION union), Reporting (`GET /api/reports/integrations`).
- **12 izin** + rol matrisi, **IntegrationHubDomain** migration + gerçek SQL Server uygulaması.
- **Minimal Angular API servisleri** (bağlanmadan, gelecek UI için hazır).

## 2. Integration Hub Mimarisi

Integration Hub, tüm mevcut ve gelecekteki modüllerin kullandığı **tek entegrasyon altyapısıdır**; hiçbir domain kendi ham HTTP entegrasyonunu yapmaz. Katmanlar:
- **Tanım katmanı:** `IntegrationDefinition` (aggregate root) provider/kategori/URL/kimlik doğrulama türünü taşır; credential/endpoint/subscription/event'lerini sahiplenir.
- **Çalışma zamanı katmanı:** `IntegrationExecution` (aggregate root) bir entegrasyon işlemini temsil eder ve **outbox** görevi görür; denemeler `IntegrationExecutionAttempt` olarak kaydedilir.
- **Adaptör katmanı:** Provider'lar kararlı bir arayüz arkasında; `IntegrationDefinition.Provider` değerine göre resolver tarafından çözülür.
- **Denetim:** Her anlamlı işlem `IntegrationEvent` (ekle-yalnızca) üretir ve Unified Audit'e beslenir.

Mikroservis, Kafka/RabbitMQ/MassTransit/MediatR veya ayrı entegrasyon platformu **yoktur**; modüler monolit korunur.

## 3. Provider/Adaptör Modeli

`IIntegrationProvider` sorumlulukları: sağlayıcı kimliği, yön yetenekleri (SupportsIncoming/Outgoing), yapılandırma doğrulama, bağlantı testi, giden çağrı yürütme, gelen webhook doğrulama, dış referans normalleştirme. Sonuç modelleri güvenli/sanitize edilmiştir (`ProviderConfigValidationResult`, `ProviderConnectionResult`, `ProviderExecuteResult`, `IncomingWebhookValidationResult`, `ExternalReference`).

`IntegrationProviderResolver` tüm kayıtlı provider'ları DI'dan toplar ve isme göre çözer — **servislerde büyük switch blokları yoktur**; yeni provider eklemek yalnızca bir `IIntegrationProvider` kaydı gerektirir.

Uygulanan 6 provider:
- **GenericRest / OutgoingWebhook:** gerçek giden HTTP (tipli client); OutgoingWebhook gövdeyi webhook secret ile HMAC imzalar.
- **IncomingWebhook:** yalnızca gelen; imza/secret + replay doğrulaması.
- **DummySmtp:** SMTP soyutlama hazırlığı; deterministik simülasyon (gerçek SMTP yok).
- **AzureDevOpsSandbox / JiraSandbox:** deterministik simüle yanıtlar + sağlayıcıya özel normalleştirme (Work Item/Pipeline Run/PR; Issue Key/Project Key) + izinli gelen eşleme.

## 4. Secret Protection ve Credential Yönetimi

- Ham secret **hiçbir zaman saklanmaz/döndürülmez/loglanmaz**. Yalnızca `EncryptedValue` (Data Protection çıktısı) saklanır; DTO'larda yalnızca `MaskedValue` görünür.
- Çözme (decrypt) **yalnızca provider yürütmesi içinde** yapılır (bağlantı testi ve dispatch anı); sonuç asla loglanmaz.
- `DataProtectionSecretProtector` özel bir "purpose" ile anahtar izolasyonu sağlar.
- Credential döndürme (rotate) `CredentialRotated` denetim olayı üretir ve Admin'lere bildirim gönderir.
- **Üretim notu:** Data Protection anahtar halkası kalıcı, paylaşılan ve dinlenmede şifreli bir depoya (bağlı disk / veritabanı / anahtar kasası) alınmalıdır; aksi hâlde anahtar sıfırlaması veya çok düğümlü dağıtımda şifreli credential'lar çözülemez hale gelir.

## 5. Incoming Webhook Güvenliği

`POST /api/integrations/webhooks/{integrationCode}` — dış sistemler çağırdığı için **anonim**, ancak sertleştirilmiş:
- Entegrasyon **Active** ve provider **Incoming** desteklemeli.
- **İmza/secret doğrulaması:** HMAC-SHA256 imza (`X-Gms-Signature`, sabit zamanlı karşılaştırma) veya paylaşılan secret (`X-Webhook-Secret`).
- **Content-Type** application/json kontrolü, **64 KB gövde sınırı** (`RequestSizeLimit` + manuel cap).
- **Replay/duplicate koruması:** delivery id (header veya payload) → aynı entegrasyon için mükerrer teslimat **409**.
- **Sıkı rate limiting** (entegrasyon koduna göre bölümlenmiş pencere).
- Dönüşler: **202** kabul, **400** geçersiz payload, **401** geçersiz imza, **409** mükerrer teslimat. İç istisna detayları sızdırılmaz (domain exception middleware eşler). Reddedilen teslimatlar denetime kaydedilir ve Admin'lere bildirilir.

## 6. Outgoing Delivery ve Retry Modeli

Giden teslimat `IntegrationHttpClient` üzerinden yapılır: `IHttpClientFactory`, yapılandırılabilir timeout, otomatik yönlendirme kapalı, yanıt gövdesi 512 karaktere sınırlı, `CancellationToken`. Non-success kodlar tutarlı sınıflandırılır.
Retry politikası (azami 3 deneme): yalnızca **transient** hatalar (timeout, 408/429/500/502/503/504, ağ hatası) yeniden denenir; doğrulama/kimlik hataları (ör. 400) yeniden **denenmez**, doğrudan ölü mektuba alınır. Üstel gecikme temeli `ExponentialDelayStrategy` ile hesaplanır ve denetime yazılır; testlerde `ImmediateDelayStrategy` ile gecikme devre dışıdır.

## 7. IntegrationExecution Outbox-Ready Yaklaşımı

Ayrı bir Outbox tablosu **eklenmedi**; `IntegrationExecution`'ın kendisi outbox görevi görür:
1. İş olayı (ör. WorkflowCompleted) `IIntegrationEventPublisher.PublishAsync` ile her aktif giden abonelik için **Pending** `IntegrationExecution` kaydı oluşturur (**kaydetmeden** — çağıran domain'in transaction'ına eklenir).
2. Veritabanı transaction'ı commit olur → dış HTTP yan etkisi **commit'ten önce gerçekleşmez** (rollback'te hayalet teslimat yok).
3. `IIntegrationDispatcher` bekleyen/başarısız yürütmeleri işler: Running'e alır, provider'ı çağırır, deneme kaydeder, Succeeded/Failed/DeadLetter durumlarını kalıcılaştırır ve azami deneme sınırını uygular.
4. Bu sprintte dağıtım **Admin tetiklidir** (`POST /api/integration-executions/dispatch-pending`); testlerde kontrolsüz polling yoktur. Devre dışı bir BackgroundService temeli (gelecek) belgelenmiştir.

**Tutarlılık sınırı:** Henüz gerçek bir Outbox işleyici arka plan servisi yoktur; teslimat açık dağıtım çağrısı veya retry uç noktası ile tetiklenir. Bu, bilinçli ve belgelenmiş bir sınırlamadır (Bölüm 17).

## 8. External Object Link Sistemi

`ExternalObjectLink`, dahili GMS nesnesi (ör. ChangeRequest) ile dış sistem nesnesi (Jira issue, Azure DevOps work item) arasındaki **normalleştirilmiş** ilişkidir. Provider'ın `NormalizeReference` çıktısı kullanılır (ör. Jira `EBR-421` → issue key). Benzersizlik kısıtı `(IntegrationDefinitionId, InternalObjectType, InternalObjectId, ExternalObjectType, ExternalObjectId)` mükerrer bağı **409** ile engeller. Değişikliğin `SourceSystem`/`SourceReference` alanları görüntü/geriye dönük uyumluluk için korunur.

## 9. Change ve Workflow Entegrasyonu

- **Change:** `POST /api/change-requests/{id}/external-links` bir değişikliği seçilen entegrasyon üzerinden dış nesneye bağlar (dahili tür/kimlik route'tan türetilir). Değişiklik oluştururken **sessizce ağ çağrısı yapılmaz**; bağ yalnızca açık istekle oluşturulur.
- **Workflow tetik temeli:** Gelen Jira/Azure DevOps sandbox webhook'u normalleştirilir, bağlı ChangeRequest bulunur ve **yalnızca izinli eşlemeler** (`JiraIssueReadyForReview`, `AzureDevOpsWorkItemReadyForRelease`) için işlem yapılır. Rastgele webhook payload'ları rastgele iş akışı başlatamaz. Otomatik iş akışı tetikleme, **varsayılan kapalı** bir yapılandırma bayrağı (`Integration:EnableWorkflowTrigger`) arkasındadır; bu sprintte yalnızca kayıt/log yapılır.
- **WorkflowCompleted olayı**, iş akışı tamamlanınca publisher aracılığıyla giden teslimatları (aynı transaction'da) sıraya alır.

## 10. Notification, Unified Audit ve Reporting Entegrasyonu

- **Notification:** 5 şablon (`IntegrationConnectionFailed`, `IntegrationExecutionFailed`, `IntegrationDeadLettered`, `IntegrationCredentialRotated`, `IncomingWebhookRejected`) — yalnızca **anlamlı** durumlarda (bağlantı hatası, ölü mektup, credential döndürme, webhook reddi) gönderilir; her başarılı yürütmede bildirim gönderilmez.
- **Unified Audit:** `vw_UnifiedAuditRecords` görünümüne 10. UNION eklendi: `IntegrationEvents` → `SourceModule='INTEGRATION'`, `ObjectType` tanım/yürütmeye göre, `ObjectNumber = IntegrationNo/ExecutionNo`, `Result = Succeeded/Failed` (uygun olduğunda). Çift yazım yoktur (tek kaynak IntegrationEvents).
- **Reporting:** `GET /api/reports/integrations` gerçek metrikler döndürür: sağlayıcı/durum dağılımları, yürütme durumları, başarı/başarısızlık/retry oranları, ölü mektup sayısı, ortalama süre, son başarısızlıklar, tarihe göre yürütmeler. Rapor kataloğuna eklendi.

## 11. Yetkilendirme Modeli

12 izin (`INTEGRATION` modülü): `read, create, update, activate, credential.manage, endpoint.manage, subscription.manage, execute, retry, cancel, audit.read, link.manage`.
Rol atamaları (merkezi matris, programatik):
- **Admin:** tüm izinler (katalogdan).
- **Auditor:** `integration.read`, `integration.audit.read`.
- **ReleaseManager:** `integration.read`, `integration.link.manage`.
- **Architect:** `integration.read`.
**Credential yönetimi geniş verilmedi** — yalnızca Admin (gelecekte bir IntegrationManager rolü eklenebilir). Webhook alma uç noktası anonimdir (imza ile korunur).

## 12. API Endpoint'leri

**`/api/integrations`:** GET (liste), GET providers, GET links, GET links/object/{type}/{id}, GET {id}, GET {id}/audit, GET {id}/executions, POST, PUT {id}, POST {id}/activate|deactivate|test-connection, credential POST/PUT/DELETE, endpoint POST/PUT/DELETE, subscription POST/PUT/DELETE, link POST/DELETE.
**`/api/integration-executions`:** GET (liste), GET {id}, GET {id}/audit, POST {id}/retry, POST dispatch-pending, POST {id}/cancel.
**`/api/integrations/webhooks/{code}`:** POST (anonim, rate-limited, boyut sınırlı).
**`/api/change-requests/{id}/external-links`:** POST (dış bağlama).
**`/api/reports/integrations`:** GET (metrikler).
Tüm controller'lar ince; iş mantığı servislerdedir. Hassas uç noktalar (test-connection/dispatch/retry) `integration-sensitive`, webhook `integration-webhook` rate-limit politikalarıyla korunur.

## 13. Migration ve Seed Bilgisi

**IntegrationHubDomain** migration: 8 tablo + tüm indeksler, 12 izin + rol atamaları, 5 bildirim şablonu, unified audit görünümü güncellemesi (INTEGRATION union). Gerçek SQL Server'a (`GmsDb`) uygulandı; test veritabanı (`GmsDb_Test`) her koşuda drop+migrate edilir. Doğrulama: **8 tablo, 12 izin, 5 şablon, rol atamaları** (Admin 12 / Auditor 2 / ReleaseManager 2 / Architect 1). Mevcut veri korunur; entegrasyon tanımları için seed yapılmadı (tanımlar runtime'da oluşturulur).

## 14. Otomatik Testler ve Sonuçları

Toplam **89 test yeşil** (0 başarısız). Yeni **24 entegrasyon testi** kapsamı: entegrasyon oluşturma; credential RBAC (non-admin 403); secret'ın dinlenmede şifreli olması ve DTO'da ham dönmemesi; bağlantı testi başarı/başarısızlık (+denetim); aktivasyonun geçerli yapılandırma gerektirmesi; gelen webhook geçerli/geçersiz secret (202/401), mükerrer (409), HMAC imza, **rate-limit (429)**; olayın Pending yürütme oluşturması; dispatcher başarı (Succeeded + deneme); transient retry → DeadLetter; kalıcı hata → tek denemede DeadLetter; dispatch-pending; dış bağ oluşturma + mükerrer (409); Change kaynak referansı bağlama; **workflow tetik bayrağı kapalıyken otomatik başlatmama**; bildirimlerin yalnızca hata/ölü mektupta oluşması; INTEGRATION denetim kayıtları; rapor metrikleri; RowVersion çakışması (409); özetlerde secret sızıntısı olmaması. Testler gerçek SQL Server + deterministik HTTP handler kullanır (**gerçek internet yok**). Mevcut 65 test yeşil kaldı.

## 15. Gerçek SQL Server Uçtan Uca Test Sonuçları

Çalışan API + yerel **HttpListener mock sunucu** ile 27 adımlık senaryo **tam başarılı**: mock ayakta → roller → sağlayıcı katalogu (6 adaptör) → Generic REST oluştur + credential (maskeli + **DB'de şifreli**) → bağlantı testi başarı → aktifleştir → başarısız bağlantı testi + denetim → WorkflowCompleted olayı **Pending outbox** yürütmesi → dispatch (başarılı + deneme) → transient 3 deneme → **DeadLetter** → kalıcı hata tek denemede DeadLetter → **DeadLetter bildirimi** → gelen webhook 202/401/409 → Jira sandbox + **ChangeRequest↔EBR-421** bağı + nesneye göre sorgu + mükerrer 409 → RBAC (Auditor credential 403, okuma 200) → **INTEGRATION** birleşik denetim (43 kayıt) → entegrasyon raporu → RowVersion çakışması 409 → **secret sızıntısı yok**.

## 16. Bulunan ve Düzeltilen Hatalar

- **Rol-izin seed'i:** `RolePermissionMatrix.Build()` içinde `IntegrationPermissionsFor(role)` concat edildi (workflow sprintindeki aynı hatanın tekrarını önlemek için); aksi hâlde Auditor/ReleaseManager/Architect entegrasyon izinlerini alamazdı. Migration bunları doğru üretti.
- **Rate-limit test yapılandırması:** Test factory'nin `ConfigureAppConfiguration` ile verdiği webhook limiti startup'ta okunmuyordu (geç config). `builder.UseSetting(...)` ile host ayarı olarak sağlanınca limit erkenden uygulandı ve 429 testi determinist geçti.
- **E2E PowerShell tekil-öğe tuzağı:** PowerShell 5.1'de tek elemanlı liste skalere düşüyor, `.Count` `$null` olup `$null -lt 1` yanlış hata veriyordu; `@(...)` ile sarılarak düzeltildi (mantık hatası değildi).

## 17. Bilinen Eksikler ve Background Worker/Outbox Planı

- **Arka plan işleyici yok:** Teslimat şu an Admin tetiklidir. Plan: `IntegrationExecution`'ı outbox olarak kullanan, üstel gecikme penceresini (`IIntegrationDelayStrategy`) uygunluk kriteri olarak dikkate alan, devre dışı-varsayılan bir `BackgroundService` eklemek. Kilit/eşzamanlılık için satır bazlı işaretleme (Running + RowVersion) kullanılabilir.
- **Ham payload saklama:** Gelen webhook ham gövdeleri **saklanmaz** (veri güvenliği). Gerekirse şifreli depolama + saklama politikası ile eklenmelidir.
- **Kapsam dışı (bilinçli):** Gerçek Jira/Azure DevOps senkronizasyonu, SAP, LDAP/SSO, Teams/Slack botları, dağıtık kuyruklar, kullanıcı yazımı scriptler.
- **Domain olay dikişi:** Bu sprintte yalnızca **WorkflowCompleted** gerçek dikiş olarak bağlandı (geniş coupling'den kaçınmak için). ReleaseScheduled/ExecutionFailed/ValidationFailed/DocumentUploaded aynı `IIntegrationEventPublisher` deseniyle tek satırda eklenebilir (temel hazır).

## 18. Frontend Gerçek API Entegrasyonu İçin Hazır Olma Durumu

İki minimal Angular servisi eklendi (**bağlanmadan**): `integration-api.service.ts` (tanım/credential/endpoint/subscription/link) ve `integration-execution-api.service.ts` (liste/detay/retry/dispatch/cancel). Tipler backend DTO'larını yansıtır. Mevcut placeholder Integration sayfalarına **bağlanmadı**. Gelecek UI notu: credential giriş alanları **yalnızca-yazılabilir** olmalı (saklı secret hiçbir zaman gösterilmez), ham değer yalnızca HTTPS üzerinden gönderilmeli; dispatch/retry Admin'e özel ve rate-limited uç noktalardır.

## 19. Production Readiness Puanı

**Genel: 7.5 / 10 (PoC için güçlü temel).**
- Mimari ve güvenlik dikişleri: 9/10 (tek altyapı, adaptör arkasında provider'lar, secret koruma, commit-öncesi yan etki yok, sanitize özetler).
- Test kapsamı: 8.5/10 (89 test + 27 adımlı e2e, gerçek SQL, internet bağımsız).
- Üretime hazırlık boşlukları (−): kalıcı Data Protection anahtar halkası yapılandırması, gerçek Outbox arka plan işleyicisi, gerçek provider senkronizasyonları, gözlemlenebilirlik/metrik ihracı ve dış çağrılar için circuit-breaker henüz yok. Bunlar Bölüm 17'de planlanmıştır.
**Kalite kapıları:** Derleme 0 hata / 0 uyarı; 89 otomatik test yeşil; gerçek SQL Server e2e başarılı; hiçbir domain ham HTTP entegrasyonu yapmıyor; secret'lar korunuyor ve döndürülmüyor.
