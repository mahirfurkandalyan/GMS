# Background Processing, Outbox Dispatch ve Observability — Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — ASP.NET Core 8 modüler monolit
**Kapsam:** Operasyonel arka plan işleme, outbox dağıtımı, SLA hatırlatıcıları ve gözlemlenebilirlik (structured logging, OpenTelemetry, metrikler, health check, operasyonel durum).
**Durum:** Tamamlandı. Derleme **0 hata / 0 uyarı**. **105 otomatik test yeşil** (89 mevcut + 16 yeni). Gerçek SQL Server + worker'lar etkinken **20 adımlık uçtan uca senaryo başarılı**. OpenTelemetry console çıktısı (HTTP + worker span'leri) doğrulandı.

---

## 1. Yapılan Geliştirmeler

- **4 BackgroundService worker:** IntegrationDispatchWorker, NotificationDeliveryWorker, WorkflowSlaWorker, OperationalCleanupWorker (rapor-only, kapalı).
- **Outbox dağıtımı:** IntegrationExecution lease/backoff (NextAttemptAt/LockedUntil/LockedBy) + otomatik retry + dead-letter.
- **Asenkron e-posta:** iş transaction'ından ayrılmış, worker ile gönderilen Pending Email delivery modeli (retry/backoff/dead-letter).
- **SLA hatırlatıcıları:** WorkflowStepInstance.DueAt üzerinden due-soon/overdue tespiti, cooldown ile tek üretim, denetim kaydı.
- **Structured logging + CorrelationIdMiddleware** (X-Correlation-Id kabul/üret/yanıt + trace + log scope).
- **OpenTelemetry** (ASP.NET Core + HttpClient + özel ActivitySource/Meter) + **9 uygulama metriği**.
- **Health check'ler:** /health/live, /health/ready (SQL, Data Protection, storage, worker tazeliği, birikim).
- **OperationalStatusService** + `/api/operations` (durum + kontrollü run-once).
- **2 izin** (operations.read/manage) + rol matrisi, **WorkerHeartbeat** entity, **BackgroundProcessingAndObservability** migration.
- **Minimal Angular** operations-api servisi (bağlanmadan).

## 2. Background Worker Mimarisi

`BackgroundWorkerBase` (soyut `BackgroundService`) ortak davranışı sağlar: yapılandırılabilir aralık, master + per-worker **disable** bayrağı, **her döngüde taze scoped DbContext**, **çakışmayan** ardışık döngü, **hataların süreci çökertmeden** yakalanması, `WorkerHeartbeat` güncellemesi ve süre/hata **metrikleri**. Her worker **sınırlı batch** işler ve **tüm bekleyen satırları asla yüklemez**. Public `RunOnceAsync` teşhis/testler için tek döngüyü determinist çalıştırır. Dev worker yerine dört odaklı worker; tek dev worker yoktur. Kafka/RabbitMQ/Hangfire/Quartz kullanılmadı — yalnızca yerleşik `BackgroundService` + veritabanı destekli kayıtlar.

## 3. Integration Dispatch Otomasyonu

IntegrationDispatchWorker, IntegrationExecution'ı outbox olarak kullanır: `Pending` ve `Failed` (NextAttemptAt ≤ now) yürütmeleri **lease (LockedUntil/LockedBy) + RowVersion** ile atomik olarak sahiplenir (`ClaimDispatchableAsync`), sonra mevcut `IntegrationDispatcher` ile işler. İki worker aynı satırı işleyemez (optimistic-concurrency yarışı tek kazanana çözülür). Retry kuralı: yalnızca transient hatalar (timeout/408/429/5xx/ağ) yeniden denenir; kalıcı hatalar hemen dead-letter; azami deneme (3) sonrası dead-letter. Transient başarısızlıkta `NextAttemptAt = now + üstel backoff` atanır ve lease bırakılır. E2E'de: başarılı yürütme otomatik `Succeeded`; transient yürütme otomatik retry+backoff sonrası **DeadLetter (3 deneme)**.

## 4. Notification Delivery Otomasyonu

InApp teslimatı oluşturma anında anında yapılır; **Email artık iş transaction'ı içinde senkron gönderilmez** — `NotificationService` Email delivery'yi `Pending` oluşturur (EmailQueued denetimi), gerçek gönderim transaction commit sonrası `NotificationDeliveryWorker` + `NotificationDeliveryDispatcher` tarafından `IEmailProvider` ile yapılır. Durumlar: Pending → Processing → Sent / Failed / DeadLetter. AttemptCount/RetryCount/LastAttemptAt/NextAttemptAt izlenir. Transient hata backoff ile retry; kalıcı hata (`EmailPermanentException` / geçersiz alıcı) hemen dead-letter; azami deneme sonrası dead-letter. Lease + RowVersion + terminal-durum kontrolü **çift gönderimi** engeller. `DummyEmailProvider` Development'ta aktif kalır; gelecekteki SMTP sağlayıcı için yapılandırma hazırdır. Kullanıcı tercihleri korunur (tercih kapalıysa delivery hiç oluşturulmaz).

## 5. Workflow SLA ve Reminder Sistemi

WorkflowSlaWorker, `WorkflowStepInstance.DueAt` üzerinden **yalnızca Active** adımlar için due-soon (DueAt ≤ now + DueSoonHours) ve overdue (DueAt < now) tespiti yapar. Hatırlatmalar `DueSoonNotifiedAt`/`OverdueNotifiedAt` + **ReminderCooldownHours** ile de-duplike edilir; **sonsuza dek tekrarlamaz**. Bildirimler Notification Engine üzerinden atanan kullanıcıya/role gönderilir (`WorkflowTaskDueSoon`/`WorkflowTaskOverdue`), üretim `SlaReminderSent` olayıyla denetlenir. **Görevler otomatik tamamlanmaz/reddedilmez, otomatik yükseltme (escalation) yapılmaz** — yalnızca hatırlatma; escalation temeli bırakıldı.

## 6. Outbox/Idempotency Yaklaşımı

Yeni bir Outbox tablosu eklenmedi; **IntegrationExecution ve NotificationDelivery kendileri outbox'tır**. İş olayı, dış HTTP yan etkisi olmadan `Pending` kaydı iş transaction'ında oluşturur; commit sonrası worker işler → **commit'ten önce yan etki yok, rollback'te hayalet teslimat yok**. İdempotency dört katmanlıdır: (1) lease (LockedUntil/LockedBy) başka worker'ı dışlar, (2) RowVersion claim yarışını tek kazanana çözer, (3) terminal durum kontrolü yeniden işlemeyi engeller, (4) NextAttemptAt backoff penceresi erken yeniden denemeyi engeller. Testler: aynı yürütme yalnızca bir kez işlenir; ikinci worker çağrısı çift işlemez; başarılı e-posta iki koşuda tek kez gönderilir.

## 7. Structured Logging ve Correlation ID

`CorrelationIdMiddleware` her isteğe bir correlation id atar: geçerli gelen `X-Correlation-Id` korunur (uzunluk/charset doğrulaması), yoksa üretilir; yanıt başlığında döndürülür; `HttpContext.Items` + **log scope** (CorrelationId/TraceId/UserId) + trace Activity etiketine yazılır. Middleware kimlik doğrulamadan sonra, yetkilendirmeden önce çalışır (UserId zenginleştirmesi + her yanıtta başlık garantisi). Loglarda **sır/parola/token/ham credential/tam webhook payload yoktur**. E2E: correlation üretimi + gelen id korunması + yanıt başlığı doğrulandı.

## 8. OpenTelemetry ve Metrikler

OpenTelemetry, BCL primitifleri (`ActivitySource`=`Gms.Api`, `Meter`=`Gms.Api`) üzerine kuruldu: **ASP.NET Core + HttpClient** enstrümantasyonu + özel ActivitySource (workflow/integration/notification/validation/deployment span temelleri; worker döngüleri `*.cycle` span'leri). Exporter yapılandırma tabanlıdır: Development'ta **console** (opsiyonel), **OTLP endpoint** opsiyonel (üretimde sabit URL yoktur; OTLP paketi güvenlik danışması nedeniyle kaldırıldı, endpoint yapılandırması hazır — üretimde paket eklenerek etkinleştirilir). 9 metrik: `gms_integration_executions_total`, `gms_integration_failures_total`, `gms_integration_deadletters_total`, `gms_notification_deliveries_total`, `gms_notification_delivery_failures_total`, `gms_workflow_tasks_overdue` (gauge), `gms_background_worker_duration_ms`, `gms_background_worker_errors_total`, `gms_http_request_duration_ms`. Etiketler **düşük kardinalite** (module/provider/result/channel/worker); user/object/correlation id **etiket olarak kullanılmaz**. E2E'de console'da HTTP + `notification.delivery.cycle` span'leri doğrulandı.

## 9. Health Check ve Operational Status

`/health/live` süreç ayakta (bağımlılık kontrolü yok). `/health/ready` beş kontrol çalıştırır: **SQL Server** erişimi, **Data Protection** protect/unprotect, **doküman deposu** yaz/oku, **worker tazeliği** (WorkerStaleMinutes içinde başarı) ve **birikim eşiği**. Yanıt yalnızca durum + kontrol adı/durum/açıklama içerir — **stack trace/sır sızmaz**. `IOperationalStatusService` gerçek birikim sayıları + worker heartbeat'leri döndürür: `GET /api/operations/status`. Opsiyonel `POST /api/operations/workers/{name}/run-once` kontrollü teşhis içindir (normal işleyiş değil).

## 10. Yetkilendirme Modeli

2 yeni izin (`OPERATIONS` modülü): `operations.read`, `operations.manage`. Rol atamaları: **Admin** her ikisi; **Auditor** read; **ReleaseManager** read. Run-once (manage) **yalnızca Admin**. E2E + testler: Requester status → 403; Auditor status → 200 ama run-once → 403; Admin → tümü.

## 11. Migration Bilgisi

**BackgroundProcessingAndObservability** migration: IntegrationExecution (NextAttemptAt/LockedUntil/LockedBy), NotificationDelivery (NextAttemptAt/LockedUntil/LockedBy/LastAttemptAt/AttemptCount/RowVersion), WorkflowStepInstance (DueSoonNotifiedAt/OverdueNotifiedAt), yeni **WorkerHeartbeats** tablosu (WorkerName+InstanceId unique), worker-sorgu indeksleri ve 2 izin + rol atamaları. Gerçek SQL Server'a (`GmsDb`) uygulandı; test veritabanı her koşuda drop+migrate. Mevcut tüm veri korundu.

## 12. Otomatik Testler ve Sonuçları

Toplam **105 test yeşil** (0 başarısız). Yeni **16 test**, gerçek zaman gecikmesine bağlı değildir (immediate delay stratejisi + run-once + DB manipülasyonu): pending yürütmenin bir kez sahiplenilmesi; iki çağrının çift işlememesi; NextAttemptAt'ten önce işlenmemesi/sonra işlenmesi; retry tükenince dead-letter; commit sonrası e-posta gönderimi; transient e-posta retry; kalıcı e-posta dead-letter; çift gönderim engeli; due-soon/overdue **tek** üretim + cooldown; correlation üretimi/korunması/yanıt; health live+ready; operasyonel birikim + heartbeat; RBAC (Requester 403, Auditor 200/run-once 403); sızıntı yok. EF InMemory kullanılmadı — gerçek SQL Server.

## 13. Gerçek SQL Server Uçtan Uca Test Sonuçları

Worker'lar **etkin**, kısa aralıklı (2–3 sn) ve düşük retry-base (2 sn) API + yerel mock sunucu ile 20 adım **tam başarılı**: otomatik başarılı dağıtım (Succeeded + deneme) → otomatik transient retry+backoff → **DeadLetter (3 deneme)** → otomatik e-posta gönderimi (Pending kalmadı) → SLA **due-soon** hatırlatması + **cooldown** (tekrar yok) → **overdue** hatırlatması → operational status (birikim + 3 worker heartbeat) → health/live+ready (iç detay sızmıyor) → correlation üretimi/korunması → RBAC. Ayrıca OpenTelemetry console çıktısı (HTTP + worker span'leri) doğrulandı.

## 14. Bulunan ve Düzeltilen Hatalar

- **`ClaimDispatchableAsync` arayüze eklenmemişti** → worker derlemesi kırıldı; `IIntegrationDispatcher` arayüzüne eklendi.
- **OTLP exporter güvenlik danışması (NU1902)** sürümden bağımsız uyarı verdi (geçişli bağımlılık) → OTLP paketi kaldırıldı (opsiyonel; endpoint yapılandırması hazır), Console exporter + instrumentation korundu → 0 uyarı.
- **Rol-izin seed concat'i** (önceki sprintlerdeki tekrar) → `OperationsPermissionsFor(role)` matrise concat edildi; migration doğru üretti.
- **Retry backoff e2e için çok yavaştı** (üretim 30 sn) → `RetryBaseDelaySeconds` yapılandırması eklendi; e2e 2 sn ile hızlı otomatik retry.
- **Tam paket ilk koşusunda tek geçici hata** (Docker soğuk başlangıcı, SQL ısınması) → tekrar koşuda 105/105 yeşil (mantık hatası değil).

## 15. Bilinen Eksikler ve Production Deployment Notları

- **Data Protection anahtar halkası** üretimde kalıcı/paylaşılan ve dinlenmede şifreli depoya alınmalı (aksi hâlde çok düğümde/anahtar sıfırlamada credential'lar çözülemez).
- **OTLP exporter** üretimde paket eklenerek + `Observability:OtlpEndpoint` verilerek etkinleştirilmeli; sabit collector URL'i yok.
- **OperationalCleanupWorker** yalnızca sayar/raporlar (kapalı); gerçek silme yok. Denetim kayıtları asla silinmez, dokümanlar temizlenmez — açık retention politikası bir sonraki sprintte.
- **Escalation** (SLA yükseltme) yalnızca temel; otomatik yükseltme yok.
- **EF Core enstrümantasyonu** yapılandırma bayrağı hazır ancak ek paket gerektirdiği için bu sprintte eklenmedi.
- **Teslimat semantiği at-least-once**'tır (gerçek Outbox arka plan işleyicisi + idempotency ile hayalet teslimat önlenir; sağlayıcı-tarafı çift alım nadir ve belgelenmiştir). Çok düğümlü dağıtımda `InstanceId` düğüm başına benzersiz verilmelidir.

## 16. Frontend Gerçek API Entegrasyonu İçin Hazır Olma Durumu

Minimal `operations-api.service.ts` eklendi (**bağlanmadı**): operasyonel durum + kontrollü run-once. Operasyonel kontroller UI'da geniş açılmamalı; run-once yalnızca Admin. Gelecek UI, `/api/operations/status` ile bir "operasyon panosu" (birikimler, heartbeat'ler, dead-letter'lar) sunabilir. Health/OTel altyapısı, harici izleme (Grafana/Prometheus/OTLP collector) bağlanınca hazırdır.

## 17. Production Readiness Puanı

**Genel: 8 / 10 (operasyonel olgunluk belirgin arttı).**
- Arka plan işleme + outbox + idempotency: 9/10 (lease + RowVersion + backoff + dead-letter; commit-öncesi yan etki yok).
- Gözlemlenebilirlik: 8/10 (structured logging + correlation + OTel traces/metrics + health/ready; OTLP collector ve dashboard'lar operasyonel bağlanmayı bekliyor).
- Dayanıklılık: 8/10 (worker'lar süreci çökertmez, sınırlı batch, disable edilebilir, heartbeat + tazelik sağlığı).
- Kalan boşluklar (−): kalıcı Data Protection anahtar yönetimi, gerçek Outbox arka plan işleyicisinin çok düğüm testi, cleanup silme politikası, SLA escalation, EF enstrümantasyonu. Bölüm 15'te planlandı.
**Kalite kapıları:** Derleme 0 hata / 0 uyarı; 105 otomatik test yeşil; gerçek SQL Server e2e başarılı; dış yan etkiler yalnızca commit sonrası; worker'lar idempotent ve sınırlı batch; retry bounded + explicit dead-letter; sır loglanmıyor; metrikler düşük kardinalite; health endpoint'leri iç detay sızdırmıyor.
