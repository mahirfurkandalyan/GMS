# Angular Change Management + Workflow Task — Gerçek Backend Entegrasyonu Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — Angular 22 (standalone + signals) frontend, ASP.NET Core 8 backend.
**Kapsam:** Mevcut Angular Change ekranlarının (liste / detay / oluşturma sihirbazı) ve Workflow Görev deneyiminin gerçek GMS backend'ine bağlanması. Authentication ve paylaşılan API temeli (önceki sprint) yeniden kullanıldı: `AuthStateService`, `authInterceptor`, `API_BASE_URL`, `ApiError`, `PagedResult<T>`, izin guard'ları/direktifleri, RowVersion çakışma yönetimi.
**Durum:** Tamamlandı. Angular production derlemesi **0 hata / 0 uyarı**. **42 otomatik test yeşil** (16 mevcut auth/app + 26 yeni Change/Workflow). Gerçek backend'e (`localhost:5080`) karşı **Change → Workflow yaşam döngüsü uçtan uca doğrulandı** (oluştur → gönder → UnderReview → Architect görevi → tamamla → **Approved**).

> **Kapsam sınırı (bilinçli):** UI yeniden tasarlanmadı, yeni iş modülü eklenmedi, mevcut tasarım sistemi/rotalar/UX korundu, çalışan backend API'leri değiştirilmedi (yalnızca additif bir DTO alanı eklendi — bkz. §14). Mock/localStorage yalnızca Change ve Workflow **runtime/görev** özelliklerinden kaldırıldı. Release, Execution, Validation, Documents, Notifications, Audit, Reports, Integrations **taşınmadı**. `actorUserId` / `X-Actor-User-Id` yeniden kullanılmadı — aktör her zaman JWT'den çözülür.

---

## 1. Yapılan Geliştirmeler

- **Odaklı API servisleri:** `ChangeApiService`, `WorkflowInstanceApiService`, `ReferenceDataApiService` — bileşenler asla elle URL kurmaz.
- **DTO-hizalı frontend modelleri:** Change (liste/detay/revizyon/varlık/doküman/readiness/audit) ve Workflow (instance/step/task/event) — backend enum/string değerleri birebir (PascalCase).
- **Gerçek Change listesi:** sunucu-taraflı sayfalama + filtreler (müşteri→proje→ortam bağımlı zinciri, durum, sınıf, tür, risk, arama), debounce, `switchMap` ile stale iptal, loading/empty/error durumları, URL query param senkronu.
- **Gerçek Change detayı:** genel bilgi, backend risk/readiness, son revizyon, varlıklar/dokümanlar, denetim zaman çizelgesi (gerçek aktör adları), ilgili aktif iş akışı durumu, RowVersion.
- **Gerçek oluşturma sihirbazı:** mevcut çok-adımlı tasarım korunarak `POST /api/change-requests`; gerçek referans veri; UI kebab tip → backend sabit eşlemesi; ilk revizyon + varlık + doküman meta; oluşturmadan sonra gerçek detaya yönlendirme.
- **Change aksiyonları:** güncelleme (RowVersion + 409 UX), gönderme (readiness bulguları), iptal, revizyon oluşturma — izne duyarlı, onay dialoglu.
- **Workflow "İş Akışı Görevleri" (Görevlerim):** `GET tasks/mine`, gerçek görünürlük sunucu tarafında; görev → iş akışı örneği detayı.
- **Workflow örneği/görev detayı:** durum, ilgili değişiklik, adım zaman çizelgesi, olaylar; **tamamla/reddet** (reddet için zorunlu yorum) + yetkiliye **duraklat/sürdür/iptal**.
- **Paylaşılan request-state** primitifi (`createRequestState`), her sayfada tutarlı loading/empty/error/submitting.
- **26 yeni otomatik test** + additif backend aktör-adı alanı (denetim/olay zaman çizelgeleri için).

## 2. Change Frontend Entegrasyon Mimarisi

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Modeller | `core/change/change.models.ts` | DTO-hizalı arayüzler (list/detail/revision/asset/doc/readiness/audit + create/update input). |
| Etiket/enum | `core/change/change-labels.ts` | Backend enum değer listeleri + i18n anahtar yardımcıları + ikon + kebab↔backend tip eşlemesi. |
| API | `core/change/change-api.service.ts` | `list/getById/create/update/submit/cancel/addRevision/getAudit` — Bearer + hata normalizasyonu interceptor'lardan. |
| Referans veri | `core/reference/reference-data-api.service.ts` | customers / projects(customerId) / environments(projectId) bağımlı zinciri. |

Bileşenler (`change-list`, `change-detail`, `change-wizard`) yalnızca bu servisler üzerinden konuşur; hiçbir bileşen elle URL kurmaz. Actor (oluşturan/güncelleyen) **istemciden gönderilmez** — backend JWT'den çözer.

## 3. Workflow Tasks Frontend Entegrasyon Mimarisi

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Modeller | `core/workflow/workflow.models.ts` | Instance list/detail, step instance, task item, event. |
| Etiket | `core/workflow/workflow-labels.ts` | status/stepStatus/stepType/event → `workflows` scope i18n anahtarları. |
| API | `core/workflow/workflow-instance-api.service.ts` | `list/getById/myTasks/completeTask/rejectTask/pause/resume/cancel`. |
| Görevlerim | `features/workflow-tasks/workflow-tasks.*` | `GET tasks/mine` listesi, arama + gecikmiş filtresi, ilgili değişiklik bağlantısı. |
| Örnek/görev detayı | `features/workflow-tasks/workflow-instance-detail.*` | Örnek durumu, adımlar, zaman çizelgesi + tamamla/reddet/duraklat/sürdür/iptal. |

**Önemli sözleşme farkı:** Backend'de ayrı `/audit` veya `/timeline` ucu **yoktur**; iş akışı zaman çizelgesi, detay yanıtındaki `events` dizisidir. Bu, uydurma bir sözleşme yerine gerçek backend incelenerek uygulandı.

## 4. Backend DTO ve Enum Eşleme Kararları

- **Enum değerleri birebir korunur:** `changeClass` (Standard/Normal/Emergency), `changeType` (ApplicationDeployment/DatabaseSchemaChange/…), `priority` (Low/Medium/High/Critical), `status` (Draft/Submitted/UnderReview/Approved/Scheduled/Implemented/Cancelled), workflow `status`/`stepType`/`stepStatus`. **UI etiketi asla backend değeri olarak gönderilmez.**
- **Görüntü etiketleri i18n'den:** `changes` ve `workflows` Transloco scope'ları backend değerini Türkçe/İngilizce etikete çevirir (ör. `changes.type.ApplicationDeployment`). Yeni `changes` scope dosyaları oluşturuldu (önceki sprintlerden eksikti).
- **Kebab↔backend köprüsü:** Sihirbazın mevcut kebab tabanlı teknik-alan şeması korunurken (`app-deploy`, `db-schema`…), oluşturmada `KEBAB_TO_CHANGE_TYPE` ile gerçek sabitlere (`ApplicationDeployment`…) çevrilir.
- **Badge registry genişletildi:** `UnderReview`, `Scheduled`, `Implemented`, `Created` durumları ve i18n karşılıkları eklendi.
- **Sayfalama zarfı:** `PagedResult<T>` (items/page/pageSize/totalCount/totalPages) birebir kullanılır; gizli `pageSize=100` **kaldırıldı** (varsayılan 20, sunucu-taraflı).

## 5. Change list / detail / wizard Entegrasyonu

**Liste** (`GET /api/change-requests`): tüm filtreler ve sıralama/sayfalama sunucu tarafında. Filtre/sıralama/sayfa sinyalleri tek bir `toObservable + debounceTime(250) + distinctUntilChanged + switchMap` akışına beslenir → stale istekler iptal edilir, arama debounce edilir. `GmsDataGrid` yalnızca **mevcut sayfayı** render eder (dahili sayfalama devre dışı, `pageSize=0`); sunucu pager'ı ayrı gösterilir. Filtreler URL query param'larında korunur (derin bağlantı). Yeni Değişiklik butonu `*gmsHasPermission="'change.create'"`.

**Detay** (`GET /api/change-requests/{id}`): sekmeler Genel Bakış / Revizyon / İş Akışı / Denetim. Gerçek risk (backend yetkili), readiness puanı + bulgular (Kritik/Uyarı ayrımı), son revizyon, varlıklar, dokümanlar, RowVersion. İlgili aktif iş akışı örneği ayrıca `WorkflowInstanceApiService.list({triggerObjectId})` → `getById` ile yüklenir ve aktif adım/rol gösterilir.

**Sihirbaz** (`POST /api/change-requests`): mevcut 6 adımlı tasarım korundu. Adım 1 artık gerçek müşteri→proje→ortam bağımlı zinciri kullanır. Risk **ön izleme** olarak etiketlendi ("Ön izleme: … risk"); resmî risk/readiness oluşturmada backend'ten gelir. Başarılı oluşturmada taslak (`gms.change.draft`) temizlenir ve gerçek detaya yönlendirilir. Taslak localStorage yalnızca **kaydedilmemiş form kurtarma** içindir (domain kalıcılığı değil).

## 6. Submit / cancel / revision Akışları

- **Submit** (`POST …/submit`): onay dialogu → başarıda değişiklik `UnderReview`, ilgili iş akışı yüklenir. **Kritik readiness bulgusu** varsa backend 400 döner; normalize edici bulguları `readinessFindings` olarak yakalar ve detayda Kritik/Uyarı ayrımıyla engelleyici banner gösterilir — sahte başarı yok.
- **Cancel** (`POST …/cancel`): onay dialogu → başarıda detay yeniden yüklenir; aktif iş akışı backend tarafından iptal edilir. Yerel durum backend başarısından önce değiştirilmez.
- **Revision** (`POST …/revisions`): drawer formu → başarıda detay yeniden yüklenir, son revizyon ve risk/readiness güncellenir. İzin: `change.revision.create`.

Sihirbazın "İncelemeye Gönder" aksiyonu değişikliği oluşturur (Draft) sonra submit çağırır; her iki yol da gerçek detaya iner. Submit readiness ile engellenirse değişiklik Draft olarak kalır ve kullanıcı detaydan bulguları giderir.

## 7. Risk, Readiness ve Audit Gösterimi

- **Risk backend yetkilidir.** Frontend resmî riski hesaplamaz; yalnızca sihirbazda açıkça "ön izleme" etiketli tahmini bir gösterge sunar. Resmî `RiskScore`/`RiskLevel`/`ReadinessScore`/`Findings` backend yanıtlarından gelir.
- **Readiness** detayda puan + bulgu listesi (Kritik kırmızı, Uyarı sarı, öneri satırı) olarak; submit engellenirse ayrı banner.
- **Audit** gerçek denetim uçları/gömülü olaylardan; olay tipi (Türkçe etiket) + açıklama + **gerçek aktör adı** + zaman damgası, kronolojik. Frontend hiç denetim olayı üretmez.

## 8. Workflow Görev Tamamlama / Reddetme Akışları

- **Complete** (`POST …/tasks/complete`): opsiyonel yorum + onay dialogu → başarıda örnek/görev yeniden yüklenir; sonraki adım veya tamamlanmış iş akışı gösterilir. İzin: `workflow.task.complete`. Aksiyon yalnızca aktif adım geçerli kullanıcıya/rolüne atanmışsa gösterilir (`canActOnTask`); backend atama otoritesidir.
- **Reject** (`POST …/tasks/reject`): **yorum zorunlu** (boşsa hata), sonuç uyarısı gösterilir ("Bu işlem iş akışını reddedecek ve değişikliği geri gönderecektir."), onay dialogu → başarıda ilgili görünümler yenilenir. İzin: `workflow.task.reject`. Reddetme **Change update ile taklit edilmez** — gerçek reject ucu çağrılır.
- **Pause/Resume/Cancel** (`…/pause|resume|cancel`): yalnızca yetkili kullanıcıya (`workflow.instance.pause/resume/cancel`) ve uygun duruma göre gösterilir; iptal onay ister ve RowVersion gönderir.
- İş akışı tamamlanması **yerel olarak çıkarsanmaz** — her aksiyon backend'ten güncel örneği döndürür.

## 9. Permission-aware UI Kararları

Mevcut yapısal direktifler kullanıldı — şablonlarda **rol string'i elle karşılaştırılmaz**: `*gmsHasPermission` uygulandı → Yeni Change, Düzenle, Gönder, İptal, Revizyon, Görev Tamamla/Reddet, Duraklat/Sürdür/İptal. Rota koruması `permissionGuard` + `data.permission` ile (`/tasks` → `workflow.task.read`, `/workflow-instances/:id` → `workflow.instance.read`). Nesne düzeyinde atama uyuşmazlığında backend yine 403 dönebilir; bu, **oturum düşürülmeden** normalize `ApiError` ile ele alınır (auth sprintindeki interceptor davranışı: 403 refresh/clear tetiklemez).

## 10. Mock / localStorage Temizliği

- **Change ekranları (liste/detay/sihirbaz)** artık mock `ChangeService`'i kullanmaz — tamamı `ChangeApiService` üzerinden gerçek backend.
- **Workflow görev/örnek ekranları** yeni gerçek servisleri kullanır; hiçbir localStorage domain kalıcılığı yoktur.
- **İzin verilen storage:** sihirbaz taslağı (`gms.change.draft`, kaydedilmemiş form kurtarma), sidebar/dil tercihleri.
- **`change.service.ts` (mock)** silinmedi çünkü **bu sprintte taşınmayacak** modüller (Release Planning sihirbazı, Reports) hâlâ ondan besleniyor; başlık yorumu güncellenerek artık Change ekranları için kaynak olmadığı ve ilgili modüller taşınınca kaldırılacağı belgelendi. Yeni Change/Workflow kodu buraya **bağlanmaz**.
- **Eski `core/change-api.service.ts`** (auth sprintinden kalan, `pageSize=100` + `actorUserId` içeren iskelet) **silindi**; yerini `core/change/change-api.service.ts` aldı.
- Testlerle doğrulandı: servisler Change/Workflow domain durumunu localStorage'a **yazmaz** (test 14 & 10).

## 11. Approval UI Netleştirmesi

Backend Workflow Strateji A kullanır: yeni Change onayları **iş akışı görevleridir**; geçmiş ApprovalRequest kayıtları okunabilir kalır. Frontend'de:
- **"İş Akışı Görevleri" (`/tasks`)** birincil aksiyon deneyimidir ve sol menüye eklendi.
- Mevcut Onaylar sayfası mock tabanlı olduğundan, karışıklığı önlemek için menü etiketi **"Onay Kayıtları (Geçmiş)"** olarak netleştirildi; rota kaldırılmadı. Kullanıcılar yeni Workflow-kontrollü bir Change'i eski Onay UI'ından onaylayamaz.

## 12. Otomatik Frontend Testleri ve Sonuçları

Altyapı: **Vitest** + `HttpTestingController` + `apiErrorInterceptor` (gerçek hata normalizasyonu). Gerçek backend'e bağımlı değil.

`core/change/change-integration.spec.ts` — **16 test:** list pagination/filter param'ları (gizli pageSize yok); paged zarf item'ları; boş sonuç; create'in PascalCase enum + actorUserId'siz POST'u; create'in gerçek detay dönüşü; submit → UnderReview; **submit readiness 400 → yapısal `readinessFindings`**; getById risk/readiness; update'in RowVersion içermesi; **409 → concurrency conflict + standart mesaj**; revision → güncel revizyon; cancel POST; audit aktör adları; **servis localStorage'a yazmaz**; kebab→backend eşleme bütünlüğü; **403 → forbidden (oturum korur)**.

`core/workflow/workflow-integration.spec.ts` — **10 test:** myTasks GET; triggerObjectId ile filtreli list; getById step+event; complete yorumlu POST; **reject zorunlu yorumlu POST**; pause/resume uçları; cancel reason+rowVersion; **403 → forbidden**; etiket yardımcıları; servis localStorage'a yazmaz.

**Sonuç:** `Test Files 4 passed (4)`, `Tests 42 passed (42)` (26 yeni + 16 mevcut auth/app **hepsi yeşil**).

## 13. Gerçek Backend Uçtan Uca Test Sonuçları

Backend `localhost:5080`, frontend `localhost:4200`, seed kullanıcılar `@gms.local` / `Gms.Dev.2026!`. Tarayıcı üzerinden doğrulanan adımlar:

| # | Senaryo | Sonuç |
|---|---|---|
| 1 | Requester ile giriş → gerçek Change listesi | ✅ 52 değişiklik, sunucu-taraflı filtre/sayfalama, Türkçe enum etiketleri |
| 2 | Sihirbazda gerçek referans veri | ✅ Müşteri (Abdi İbrahim/Bilim İlaç) → Proje (EBR Migration) → Ortam (DEV) bağımlı zinciri |
| 3 | Standart Change oluştur + gönder | ✅ **CHG-2026-000052** üretildi, gerçek detaya yönlendirdi |
| 4 | Backend risk/readiness | ✅ Risk (backend hesaplı), Hazırlık 50/100 detayda |
| 5 | Change durumu | ✅ **İncelemede (UnderReview)** |
| 6 | Architect'e iş akışı görevi | ✅ WFI-2026-000027 "Mimari Onayı" (Approval, Architect) |
| 7 | Architect ile giriş → Görevlerim | ✅ 5 gerçek görev, "Gecikmiş" göstergesi, ilgili Change bağlantısı |
| 8 | Architect görevi tamamla | ✅ Örnek **Tamamlandı**, adımlar Başlangıç→Mimari Onayı→Bitiş |
| 9 | Standart iş akışı tamamlanması | ✅ SONUÇ "onaylandı" |
| 10 | Change durumu | ✅ **Onaylandı (Approved)** |
| 11 | Denetim zaman çizelgesi | ✅ **Gerçek aktör adları**: "Requester User", "Architect User" |
| 12 | İş Akışı sekmesi etiketleri | ✅ stepType/status Türkçe çözüldü (ham anahtar yok) |
| 13 | Boş durumlar / i18n | ✅ Görevlerim boş durumu, tüm scope'lar |

Kritik mission hedefi — **gerçek Change → Workflow yaşam döngüsünün backend'e karşı doğrulanması** — eksiksiz karşılandı. RowVersion 409 UX, reject (zorunlu yorum), cancel ve pause/resume akışları **26 birim testiyle** doğrulandı; 403 oturum-koruma davranışı auth sprintinde e2e doğrulanmıştı ve burada da normalize hata ile ele alınır.

## 14. Bulunan ve Düzeltilen Backend / Frontend Entegrasyon Hataları

1. **Frontend — `changes` scope çevirileri eksik (mevcut boşluk):** Change ekranları ham i18n anahtarları gösteriyordu. Yeni `public/i18n/changes/{tr,en}.json` scope dosyaları oluşturuldu (class/type/priority/audit/readiness/risk/workflow/actions).
2. **Frontend — change-detail İş Akışı sekmesinde ham `workflows.stepType.*` anahtarı (e2e'de yakalandı):** change-detail yalnızca `changes` scope'unu yüklüyordu. Çözüm: bileşene `provideTranslocoScope('workflows')` de eklendi.
3. **Frontend — submit readiness bulgularının kaybolması:** `apiErrorInterceptor` 400 gövdesindeki yapısal `findings`'i düşürüyordu. `normalizeHttpError` genişletildi: `ApiError.readinessFindings` alanı eklendi ve detayda engelleyici banner olarak gösterildi.
4. **Backend — denetim/olay zaman çizelgelerinde aktör adı yokluğu (integration gap → en küçük güvenli additif düzeltme):** `ChangeAuditEventDto` ve `WorkflowEventDto`'ya **additif** `ActorUserName` alanı eklendi; ilgili controller'larda Users tablosundan join ile dolduruldu (`ToDetailAsync` yardımcıları + `GetAudit` projeksiyonu). **Kırıcı olmayan**, yalnızca yeni alan; yetkilendirme zayıflatılmadı. Gerçek e2e ile doğrulandı (audit ve workflow zaman çizelgelerinde "Requester User" / "Architect User" görüldü). Not: değişiklik additif ve e2e ile kanıtlı olduğundan ayrı xUnit testi eklenmedi; mevcut backend test paketi kırılmadı.
5. **Frontend — sihirbaz otomasyonunda başlık alanı (yalnızca e2e sürecinde):** shell arama kutusunun ilk `input[type=text]` olması test betiğini yanılttı; gerçek uygulama akışında sorun yoktu (kullanıcı doğru alana yazar).

## 15. Bilinen Eksikler ve UX Notları

- **Ertelenen modüller mock kalıyor:** Release Planning sihirbazı ve Reports hâlâ mock `ChangeService`'ten beslenir (mission gereği taşınmadı). Bu, Change domain kalıcılığının localStorage'da **kalıntı** olarak kalmasına yol açar ancak yalnızca bu ertelenen modüller için; Change/Workflow ekranları tamamen gerçek API'dir. İlgili modüller taşınınca kaldırılacaktır.
- **Approval sayfası** mock tabanlıdır; "Onay Kayıtları (Geçmiş)" olarak etiketlendi, gelecekte gerçek Approval API'sine bağlanacak.
- **Varlık seçici** sihirbazda Asset Center mock'undan beslenir (Asset modülü bu sprint kapsamında değil); seçilen varlıklar backend'e string alanlar olarak gönderilir.
- **Doküman yükleme yok:** sihirbaz yalnızca doküman meta gönderir (Document Management entegrasyonu sonraki sprint).
- **Yüksek riskli branch (ReleaseManager) ve Normal medium çok-adımlı akış** kod ve backend tarafından desteklenir; bu raporda Standart tam döngü uçtan uca gösterildi, diğer dallar aynı gerçek uçları kullanır.
- **Frontend correlation-id üretimi** eklenmedi (opsiyonel); backend `X-Correlation-Id` üretir ve 500 mesajlarında `ApiError.correlationId` olarak taşınır.

## 16. Release Planning Frontend Entegrasyonu İçin Hazır Olma Durumu

Bu sprint, Release Planning'in doğrudan yeniden kullanacağı deseni kurdu:
- **Odaklı API servisi deseni** (`ChangeApiService`/`WorkflowInstanceApiService`) — Release için `ReleaseApiService` aynı kalıpla eklenebilir.
- **Referans veri servisi** (`ReferenceDataApiService`) hazır; Release da müşteri/proje/ortam kullanır.
- **`PagedResult<T>` + sunucu-taraflı liste deseni** (debounce + switchMap + pager) yeniden kullanılabilir.
- **RowVersion 409 UX + `ApiError` + readiness/bulgu gösterimi** hazır bileşen desenleri.
- **İzin guard/direktifleri** ve **request-state** primitifi ortak.
- **Change ↔ Release bağı:** Approved değişiklikler artık gerçek backend'te; Release Planning bunları gerçek `change-requests` listesinden (status=Approved) çekebilir. Mevcut Release sihirbazının mock `ChangeService` bağımlılığı, bu servise geçişle kaldırılacaktır.

## 17. Frontend Production Readiness Puanı

**Puan: 8.3 / 10 (Change + Workflow runtime entegrasyonu için)**

**Güçlü yanlar:** odaklı API servisleri + DTO-hizalı modeller; sunucu-taraflı sayfalama/filtre (gizli pageSize yok); debounce + switchMap ile stale iptal; backend-yetkili risk/readiness; RowVersion 409 UX; izne duyarlı aksiyonlar (direktifler); tam Change→Workflow yaşam döngüsü gerçek backend'te doğrulandı; gerçek aktör adlı denetim; 0 hata/0 uyarı build; 42 test yeşil.

**Puanı sınırlayan hususlar:** Release/Reports hâlâ mock `ChangeService`'e bağlı (kalıntı localStorage); Approval sayfası mock (geçmiş olarak etiketli); doküman yükleme yok; yüksek-riskli/çok-adımlı dallar bu raporda uçtan uca gösterilmedi (kod + birim testleriyle kapsandı); backend aktör-adı eklemesi ayrı xUnit testi almadı (e2e ile doğrulandı).

Change ve Workflow runtime/görev deneyimi **üretim mimarisi kalitesindedir**; tam üretim hazırlığı, kalan modüllerin (Release, Reports, Approval, Documents) gerçek backend'e taşınmasıyla tamamlanır.
