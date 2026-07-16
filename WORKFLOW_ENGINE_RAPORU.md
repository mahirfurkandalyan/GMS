# Workflow Engine (İş Akışı Motoru) — Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — ASP.NET Core 8 modüler monolit
**Kapsam:** Sürümlü, yeniden kullanılabilir yönetişim iş akışı motoru; ilk entegrasyon hedefi Change Management.
**Durum:** Tamamlandı. Derleme **0 hata / 0 uyarı**. **65 otomatik test yeşil** (39 mevcut + 26 yeni). Gerçek SQL Server üzerinde **25 adımlık uçtan uca senaryo başarılı**.

---

## 1. Genel Bakış ve Amaç

Bu sprint, GMS platformuna **gerçek bir iş akışı yürütme motoru** kazandırır. Motor yalnızca görsel bir tasarımcı değildir; **sürümlenmiş** yönetişim akışlarını çalıştırır: adımları otomatik işler, manuel (onay/görev) adımlarda durur, koşullara göre dallanır ve akış tamamlandığında ilgili yönetişim nesnesinin (ilk hedef: değişiklik talebi) durumunu günceller.

Tasarım ilkeleri:
- **Sertleştirilmiş mimarinin yeniden kullanımı:** JWT kimlik doğrulama, izin tabanlı RBAC, `ICurrentUser`, `StatusTransition` guard'ları, `RowVersion` iyimser eşzamanlılık, `SequentialNumberGenerator`, `PagedResult`, `DomainExceptionMiddleware`, `AsSplitQuery`, merkezi Bildirim Motoru ve Birleşik Denetim.
- **Pragmatik ve anlaşılır ilk sürüm:** BPMN motoru, dağıtık işçiler, ifade dili, dinamik C#/script yok.
- **Modüler monolit korunur:** İnce controller'lar, iş mantığı servislerde. Mevcut domainler yeniden tasarlanmadı.

## 2. Mimari Kararlar

**Karar 1 — Change entegrasyonu için “Strateji A” (tek doğruluk kaynağı).**
Yeni değişiklik gönderimleri artık iş akışıyla yönetilir. Onay kararları workflow görev uç noktaları üzerinden verilir. Akış **tamamlanınca değişiklik `Approved`**, **reddedilince/iptal edilince `Submitted`** olur. Eski `ApprovalService` yeni gönderimlerde **çağrılmaz**; ancak `/api/approvals` uç noktaları ve kayıtları **geçmiş için okunabilir kalır**.
Sahiplik sınırı: **Workflow** = orkestrasyon/yönlendirme/zamanlama/koşullar; **Approval** = tarihsel karar/onaycı/imza kayıtları. Bu, tek bir orkestrasyon ve tek bir durum sahibi sağlar.

**Karar 2 — Sınırlı kapsam.** Adım tipleri yalnızca: `Start`, `ManualTask`, `Approval`, `Condition`, `Notification`, `End`. Rastgele döngü, paralel dallar, BPMN ayrıştırma, script yürütme yoktur.

**Karar 3 — Güvenli koşul değerlendirme.** Koşullar yalnızca **izinli (allowlist) alanları** okur; dinamik kod/refleksiyon yoktur.

**Karar 4 — Değişmez sürümler.** Yayınlanan (`Published`) sürümlerin adım/geçiş grafiği **değiştirilemez**; çalışan örnekler her zaman kararlı bir tanıma referans verir.

## 3. Domain Modeli (7 Entity)

| Entity | Rol |
| --- | --- |
| `WorkflowDefinition` | Kararlı, sürümlenen akış kabı (kod, kategori, tetikleyici, `ActiveVersionId`, `RowVersion`). |
| `WorkflowVersion` | Grafiğin değişmez anlık görüntüsü (`Draft`/`Published`/`Retired`, `StartStepKey`). |
| `WorkflowStepDefinition` | Grafik düğümü (tip, sıra, atama rolü/kullanıcısı, SLA saati, bildirim şablonu). |
| `WorkflowTransitionDefinition` | Yönlü kenar (koşul tipi/alanı/operatör/beklenen değer, öncelik). |
| `WorkflowInstance` | Çalışan yürütme (kök; `InstanceNo`, tetikleyici nesne, durum, bağlam JSON, `RowVersion`). |
| `WorkflowStepInstance` | Bir adımın çalışma zamanı durumu (atanan, `DueAt`, sonuç, yorum). |
| `WorkflowEvent` | Ekle-yalnızca denetim olayı (WORKFLOW modülüne beslenir). |

Cascade tasarımı: `Definition → Versions → Steps/Transitions` (tek sahiplenen yol, Cascade); `Instance → StepInstances` Cascade; **`Instance → Events` Restrict** (denetim değişmezliği). Döngüsel cascade’i önlemek için `ActiveVersionId`, `CurrentStepInstanceId`, `StepDefinitionId`, `WorkflowStepInstanceId` **FK’sız düz referanslardır**. `Instance → Definition/Version` Restrict.

## 4. Durum Makineleri

- **Definition:** `Draft → {Active, Archived}`, `Active → {Inactive, Archived}`, `Inactive → {Active, Archived}`, `Archived` terminal.
- **Version:** `Draft → Published → Retired` (Published değişmez).
- **Instance:** `Created → {Running, Cancelled}`, `Running/Waiting → {Running, Completed, Rejected, Failed, Cancelled}`. Terminal anlamları: **Completed** = End’e ulaştı (nesne onaylandı), **Rejected** = onay adımı reddedildi (nesne geri gönderildi), **Cancelled** = elle iptal, **Failed** = motor hatası/adım sınırı aşımı.
- **StepInstance:** `Waiting/Active/Completed/Rejected/Skipped/Failed/Cancelled`.

Tüm geçişler `StatusTransition.Ensure(...)` guard’ından geçer; geçersiz geçiş `InvalidStatusTransitionException → 400`.

## 5. Adım Tipleri ve Motor Yürütme

Motor tek geçişte **sınırlı bir döngü** (üst sınır **50 otomatik adım**) çalıştırır:
- **Otomatik adımlar** (`Start`, `Condition`, `Notification`, `End`) anında oluşturulup tamamlanır; sonraki adım geçişlerle belirlenir.
- **Manuel adımlar** (`ManualTask`, `Approval`) `Active` olarak oluşturulur, örnek `Waiting`’e geçer, atanana bildirim gider ve motor **durur** (bekler).
- `End`’e ulaşınca örnek `Completed` olur ve değişiklik `Approved`’a taşınır.
- Sınır aşımı veya eşleşen geçiş bulunamaması `Failed` üretir (runaway koruması).

Görev tamamlanınca (`complete`) motor kaldığı yerden devam eder; koşul adımları aynı çağrıda değerlendirilir (ör. Normal/Orta risk akışı QA onayından sonra `RISK → END`’i otomatik yürütür).

## 6. Koşul Değerlendirici (Güvenli, Sınırlı)

`WorkflowConditionEvaluator` yalnızca **önceden hazırlanmış bağlam sözlüğündeki izinli alanları** okur. İzinli Change alanları: `changeClass, changeType, priority, riskLevel, riskScore, environmentName, status, readinessScore` (bunlardan `riskScore`, `readinessScore` sayısaldır). Operatörler: `Equals, NotEquals, GreaterThan(OrEqual), LessThan(OrEqual), Contains`.
- `Always` → koşulsuz yedek.
- İzinli olmayan alan / bilinmeyen operatör → **yayın (publish) sırasında reddedilir**; çalışma zamanında hiçbir zaman istisna fırlatmaz (eşleşmezse `false`).
- Sayısal alanlar sayısal karşılaştırma; sıralama operatörleri **yalnızca sayısal alanlarda** geçerli (aksi hâlde doğrulama hatası).
Böylece yönlendirme kararları tek yerde, denetlenebilir ve yayın zamanında doğrulanabilir biçimde verilir.

## 7. Atama Kuralları

Manuel adımlar bir **role** veya doğrudan bir **kullanıcıya** atanır. Çalışma zamanında rol ataması, o rolü taşıyan ilk aktif kullanıcıya çözümlenir (`AssignedUserId`). Görev işleminde yetki: kullanıcı **atanan kullanıcı** olmalı **veya** atanan **role** sahip olmalı; `workflow.admin.override` iznine sahip kullanıcı atama dışı müdahale edebilir (denetlenir).

## 8. Change Management Entegrasyonu (Strateji A)

`POST /api/change-requests/{id}/submit` akışı:
1. Kritik hazırlık bulgusu varsa gönderim engellenir (400).
2. Değişiklik `Draft → Submitted` (audit: `ChangeSubmitted`).
3. `WorkflowRuntimeService.StartForChangeAsync` çağrılır: `ChangeClass`’a göre aktif workflow tanımı çözülür, aktif `Published` sürüm yüklenir, bağlam (izinli alanlar) JSON olarak anlık görüntülenir, örnek oluşturulur ve ilk manuel adıma kadar otomatik işlenir.
4. Değişiklik `Submitted → UnderReview` (audit: onay akışı başlatıldı).
5. Tüm işlem **tek transaction**’da; controller `SaveChanges` çağırır.

Reddetme → değişiklik `Submitted`’a döner; iptal → değişiklik `Submitted`’a döner ve çalışan örnek iptal edilir. Değişiklik iptalinde (`/cancel`) ilgili çalışan workflow örnekleri de aynı transaction’da iptal edilir (`CancelForChangeAsync`, kaydetmez). Eski Approval domaini tarihsel okunabilirliğini korur.

## 9. Seed Edilen Workflow’lar

Üç sistem varsayılanı deterministik olarak seed edilir (her biri `Definition` + tek `Published v1`, `ActiveVersionId` işaretli):

- **CHANGE_STANDARD_DEFAULT** — `START → ARCH(Onay) → END`.
- **CHANGE_NORMAL_DEFAULT** — `START → ARCH → QA → RISK(Koşul) → [Critical/High → RM] / [aksi → END] → END`. Koşul öncelik sırasıyla: `riskLevel=Critical` (öncelik 1), `riskLevel=High` (öncelik 2), `Always → END` (öncelik 3).
- **CHANGE_EMERGENCY_DEFAULT** — `START → ARCH → RM → ADMIN → END`.

`ChangeClass` eşlemesi: `Standard → Standart akış`, `Emergency → Acil akış`, aksi → `Normal akış`. Doğrulama: 3 tanım Active, 14 adım, 13 geçiş.

## 10. İzinler ve Rol Matrisi

15 yeni izin (`WORKFLOW` modülü): tanım `read/create/update/publish/activate/archive`; örnek `read/start/cancel/pause/resume`; görev `read/complete/reject`; `admin.override`.
Rol atamaları merkezi matriste (`RolePermissionMatrix`) programatik olarak eklenir:
- **Architect / QA / ReleaseManager:** örnek okuma + görev okuma/tamamlama/reddetme.
- **Requester / Executor / Validator:** örnek okuma + görev okuma.
- **Auditor:** tanım okuma + örnek okuma + görev okuma.
- **Admin:** tüm izinler (katalogdan otomatik).

## 11. Bildirim Entegrasyonu

Yeni `WORKFLOW` bildirim modülü ve 6 şablon: `WorkflowTaskAssigned`, `WorkflowTaskDueSoon`, `WorkflowTaskOverdue`, `WorkflowCompleted`, `WorkflowRejected`, `WorkflowCancelled`. Manuel adım aktifleşince atanana `WorkflowTaskAssigned`; tamamlanınca talep edene `WorkflowCompleted`; reddetme/iptalde ilgili şablon gönderilir. Tümü merkezi `NotificationService` üzerinden (kullanıcı tercihleri ve kanal mantığı korunur).

## 12. Birleşik Denetim (Unified Audit)

`vw_UnifiedAuditRecords` görünümüne **WORKFLOW** için 9. `UNION ALL` eklendi: `WorkflowEvents` → `WorkflowInstances` join’i ile `SourceModule='WORKFLOW'`, `ObjectType='WorkflowInstance'`, `ObjectNumber = InstanceNo`, ilgili proje/ortam alanları. Görünüm migration’da güvenli biçimde **DROP + yeniden CREATE** edilir; `Down` önceki (8 kaynaklı) görünümü geri yükler. E2E’de WORKFLOW modülünde 131 kayıt (Started=15, Completed=7) gözlemlendi.

## 13. API Uç Noktaları

**`/api/workflows` (tanım):** `GET` (liste/sayfalı), `GET {id}`, `POST` (oluştur), `PUT versions/{vid}` (taslak düzenle), `POST versions/{vid}/validate`, `POST versions/{vid}/publish`, `POST {id}/versions/{vid}/activate`, `POST {id}/clone`, `POST {id}/archive`.
**`/api/workflow-instances` (çalışma zamanı):** `GET` (liste/filtre), `GET {id}`, `GET tasks/mine`, `POST {id}/tasks/complete`, `POST {id}/tasks/reject`, `POST changes/{changeId}/start`, `POST {id}/cancel`, `POST {id}/pause`, `POST {id}/resume`.
Tümü ince; iş mantığı `WorkflowDefinitionService` ve `WorkflowRuntimeService`’tedir. Her uç nokta ilgili izin politikasıyla korunur.

## 14. Veritabanı / Migration / İndeksler

İki migration eklendi: **`WorkflowEngineDomain`** (7 tablo + seed + görünüm güncellemesi) ve **`WorkflowRoleGrants`** (rol-izin atamaları). Her ikisi de gerçek SQL Server’a (`GmsDb`) uygulandı; test veritabanı (`GmsDb_Test`) factory tarafından her koşuda drop+migrate edilir.
İndeksler: `Definition(Code unique, Category, Status, TriggerObjectType+ChangeClass)`, `Version(DefinitionId+VersionNumber unique, Status)`, `StepDefinition(VersionId+StepKey unique, VersionId+StepOrder)`, `TransitionDefinition(VersionId, VersionId+FromStepKey+Priority)`, `Instance(InstanceNo unique, TriggerObjectType+TriggerObjectId, Status, DefinitionId, CreatedAt)`, `StepInstance(InstanceId, InstanceId+Status, AssignedUserId, AssignedRole)`, `Event(InstanceId, CreatedAt)`.

## 15. Otomatik Test Sonuçları

Toplam **65 test yeşil** (0 başarısız). Yeni **26 workflow testi** kapsamı:
- Seed doğrulama (3 aktif akış, Normal grafiği 6 adım / 7 geçiş).
- Change entegrasyonu: gönderim akışı başlatır, değişiklik `UnderReview`; Standart akış onayı `Completed` + `Approved`.
- Koşul yönlendirme: Normal/Orta risk RM’yi atlar; Normal/Kritik risk RM’ye yönlenir.
- Emergency 3’lü onay zinciri.
- Reddetme (`Rejected` + değişiklik geri) ve yorumsuz reddin 400 olması.
- RBAC: yetkisiz tamamlama 403; başka role atanmış görev 403; Admin override; `tasks/mine`.
- Yaşam döngüsü: cancel/pause/resume; değişiklik iptali workflow’u iptal eder.
- Tanım yaşam döngüsü: geçersiz grafik doğrulama hatası, publish immutability, activate, clone (v2), sistem tanımı arşivlenemez, özel tanım arşivlenir.
- İzinli olmayan koşul alanı publish’te reddedilir.
- Birleşik denetimde WORKFLOW kayıtlarının görünmesi.
Mevcut 39 testin ikisi (onay akışı) Strateji A’ya uyarlandı; biri (bildirim) `WorkflowTaskAssigned` beklentisine güncellendi.

## 16. Gerçek SQL Server Uçtan Uca Senaryo (25 Adım)

Çalışan API’ye (`http://localhost:18080`, gerçek `GmsDb`) karşı 25 adımlık senaryo **tam başarılı**:
giriş → 3 sistem akışı → Normal/Orta (Architect→QA→END, değişiklik `Approved`) → Normal/Kritik (Architect→QA→RM→`Approved`) → Emergency (Architect→RM→Admin) → reddetme → iptal → RBAC (403) → özel tanım oluştur/doğrula/yayınla/aktifleştir/klonla/arşivle → sistem tanımı arşiv koruması → birleşik denetim WORKFLOW (131 kayıt). Tek doğruluk kaynağı (workflow ↔ change durumu) her senaryoda doğrulandı.

## 17. Kapsam Dışı, Notlar ve Gelecek Çalışmalar

**Bilinçli kapsam dışı:** BPMN/ifade dili, dinamik C#/script, paralel dallar, rastgele döngüler, dağıtık işçiler, sürükle-bırak tasarımcı arayüzü, `Temporal/Camunda/Elsa/MassTransit/MediatR/Kafka/RabbitMQ`, Event Sourcing, mikroservis. Bu sprintte **frontend eklenmemiştir** (backend odaklı).
**Arka plan hazırlığı:** SLA alanları (`DueInHours`/`DueAt`) ve `WorkflowTaskDueSoon`/`Overdue` şablonları, ileride bir zamanlanmış hatırlatıcı işçisinin bağlanabilmesi için hazırdır (bu sprintte işçi çalıştırılmaz).
**Kalite:** Derleme 0 hata / 0 uyarı; 65 otomatik test yeşil; gerçek SQL Server e2e başarılı.
**Gelecek:** Workflow tasarımcı arayüzü, SLA hatırlatıcı arka plan servisi, ek tetikleyici nesne türleri (Release/Document), koşul alan kümesinin genişletilmesi.
