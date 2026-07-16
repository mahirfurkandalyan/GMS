# Angular Release Planning — Gerçek Backend Entegrasyonu Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — Angular 22 (standalone + signals) frontend, ASP.NET Core 8 backend.
**Kapsam:** Angular Release Planning ekranlarının (liste / detay / oluşturma sihirbazı) mock/localStorage davranışının kaldırılıp gerçek GMS backend'ine bağlanması. Önceki sprintlerin paylaşılan temeli yeniden kullanıldı: `AuthStateService`, `authInterceptor`, `API_BASE_URL`, `ApiError`, `PagedResult<T>`, request-state deseni, izin guard'ları/direktifleri, RowVersion çakışma yönetimi.
**Durum:** Tamamlandı. Angular production derlemesi **0 hata / 0 uyarı**. **57 otomatik test yeşil** (42 mevcut + 15 yeni Release). Gerçek backend'e (`localhost:18080`) karşı **Change → Release → Schedule yaşam döngüsü uçtan uca doğrulandı** (onaylı değişiklikler görünür → yayın oluştur → zamanla → detay/audit güncellenir).

> **Kapsam sınırı (bilinçli):** UI yeniden tasarlanmadı, mevcut tasarım sistemi/rotalar/UX korundu, NgRx veya başka state kütüphanesi eklenmedi, mock API eklenmedi. Backend API'leri değiştirilmedi — yalnızca **tek bir additif DTO alanı** eklendi (denetim zaman çizelgesi aktör adı; bkz. §Mimari). Mock/localStorage yalnızca **Release** özelliğinden kaldırıldı; ilgisiz modüllere dokunulmadı. `actorUserId` / `X-Actor-User-Id` yeniden kullanılmadı — aktör her zaman JWT'den çözülür.

---

## 1. Mimari

Change/Workflow entegrasyon sprintinde kurulan desen birebir yeniden kullanıldı:

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Modeller | `core/release/release.models.ts` | DTO-hizalı arayüzler (list/detail/item/deploymentPlan/doc/audit + create/update input). |
| Etiket/enum | `core/release/release-labels.ts` | Backend enum değer listeleri (status/type/risk) + i18n anahtar yardımcıları + terminal-durum kümesi. |
| API | `core/release/release-api.service.ts` | `list/getById/create/update/schedule/cancel/complete/getAudit` — Bearer + hata normalizasyonu interceptor'lardan. |
| Referans veri | `core/reference/reference-data-api.service.ts` (mevcut) | customers / projects(customerId) / environments(projectId) bağımlı zinciri. |
| Onaylı değişiklik havuzu | `core/change/change-api.service.ts` (mevcut) | Sihirbaz, `list({status:'Approved', customerId, projectId, environmentId})` ile yalnızca onaylı değişiklikleri çeker. |

**Bileşenler asla elle URL kurmaz;** tüm HTTP `ReleaseApiService` üzerinden gider. Actor (oluşturan/güncelleyen) **istemciden gönderilmez** — backend JWT'den çözer. **Yayın yöneticisi (ReleaseManagerUserId)** geçerli kullanıcıdan (`AuthStateService.user().id`) atanır (backend'te ayrı bir kullanıcı listesi ucu olmadığından); sihirbazda geçerli kullanıcının adı salt-okunur gösterilir.

**Gerçek endpoint'ler (`/api/releases`):** Mission'daki örnek `/release-plans` yerine backend'in gerçek rotaları kullanıldı (backend sistem-kaydıdır):
`GET /api/releases` (sayfalı) · `GET /{id}` · `GET /{id}/audit` · `POST /` · `PUT /{id}` · `POST /{id}/schedule` · `POST /{id}/cancel` · `POST /{id}/complete`.

**Backend enum değerleri birebir korunur:** `releaseType` (Major/Minor/Patch/Hotfix/Emergency), `status` (Planned/Scheduled/InProgress/Completed/Accepted/Cancelled), risk (Low/Medium/High/Critical). UI etiketi asla backend değeri olarak gönderilmez; etiketler yeni `releases` Transloco scope'undan çözülür.

**Küçük additif backend değişikliği (mission izniyle):** `ReleaseAuditEventDto`'ya **additif** `ActorUserName` alanı eklendi ve `ReleasesController` içinde Users tablosundan join ile dolduruldu (`GetAudit` projeksiyonu + `MapDetail` aktör-adı sözlüğü). Kırıcı değil, yetkilendirme zayıflatılmadı; Change/Workflow sprintindeki aynı desen. Gerçek e2e ile doğrulandı (denetim zaman çizelgesinde "System Administrator").

## 2. İş Kuralları Backend'te Kalır

- **Risk ve toplam süre Angular'da hesaplanmaz.** Sihirbaz risk/süre önizlemesi bile üretmez; oluşturmadan sonra backend'in hesapladığı resmî risk/puan/süre yayın detayında gösterilir ("Resmî risk değeri backend tarafından hesaplanır." notu).
- **Onaylı değişiklik kuralı UI'da taklit edilmez:** havuz doğrudan gerçek `change-requests` listesinden `status=Approved` + seçilen müşteri/proje/ortam ile çekilir. Backend ayrıca "yalnızca Approved", "tümü aynı müşteri/proje/ortam", "aynı değişiklik iki kez eklenemez" kurallarını zorlar; bu hatalar normalize `ApiError` ile kullanıcıya gösterilir.
- **Durum geçişleri backend'te:** schedule (Planned→Scheduled), cancel, complete backend uçlarıdır; yerel durum backend başarısından önce değiştirilmez.

## 3. Değiştirilen / Eklenen Dosyalar

**Yeni (frontend):**
- `core/release/release.models.ts`
- `core/release/release-labels.ts`
- `core/release/release-api.service.ts`
- `core/release/release-integration.spec.ts` (15 test)
- `public/i18n/releases/tr.json` + `en.json` (yeni scope)

**Yeniden yazılan (frontend):**
- `features/releases/release-list.ts` + `.html` + `.scss` (sunucu-taraflı liste)
- `features/releases/release-detail.ts` + `.html` + `.scss` (gerçek detay + aksiyonlar)
- `features/releases/release-wizard.ts` + `.html` + `.scss` (gerçek create + onaylı change seçimi)

**Güncellenen (frontend):**
- `core/release.service.ts` (mock → legacy; başlık yorumu güncellendi, artık Release ekranları kullanmıyor)
- `shared/ui/badge/badge.ts` (`InProgress`, `Accepted` durumları)
- `public/i18n/tr.json` + `en.json` (badge: InProgress/Accepted; **`Scheduled` etiketi "Planlandı"→"Zamanlandı"** çakışma düzeltmesi)

**Additif (backend):**
- `Contracts/ReleasePlanDtos.cs` (`ReleaseAuditEventDto.ActorUserName`)
- `Controllers/ReleasesController.cs` (`GetAudit` join + `MapDetail` aktör-adı çözümlemesi)

## 4. Release List

`GET /api/releases` (sayfalı zarf). Filtreler ve sıralama/sayfalama **sunucu tarafında**: müşteri→proje→ortam bağımlı zinciri, durum, arama. Filtre/sıralama/sayfa sinyalleri tek bir `toObservable + debounceTime(250) + distinctUntilChanged + switchMap` akışına beslenir → stale istekler iptal edilir, arama debounce edilir. Filtreler URL query param'larında korunur (derin bağlantı). `GmsDataGrid` yalnızca mevcut sayfayı render eder; sunucu pager'ı ayrı gösterilir. Loading/empty/error durumları. Yeni Yayın butonu `*gmsHasPermission="'release.create'"`.

> **Not (backend sınırı):** Backend liste ucu yalnızca customerId/projectId/environmentId/status/search filtrelerini destekler. Mission'da geçen **release-türü ve tarih filtreleri backend'te yok**; mock/istemci-taraflı bir filtre uydurmak yerine (backend sistem-kaydı ilkesi gereği) bu filtreler sunulmadı. Backend eklerse UI kolayca genişletilebilir.

## 5. Release Detail

`GET /api/releases/{id}`. Sekmeler: Genel Bakış / Değişiklikler / Dağıtım / Denetim. Gösterilenler: genel bilgi, backend riski (yetkili), dağıtım özeti, dahil değişiklikler (dağıtım sırası + risk/durum rozetleri, tıklayınca ilgili Change detayına gider), dağıtım planı, denetim zaman çizelgesi (**gerçek aktör adları**), RowVersion. Aksiyonlar izne duyarlı ve duruma göre gösterilir:
- **Düzenle** (`release.update`, terminal olmayan durumlarda): drawer formu → PUT (RowVersion). **409** → standart eşzamanlılık mesajı + "Güncel veriyi yükle" seçeneği (sessiz üzerine yazma yok).
- **Zamanla** (`release.schedule`, yalnızca `Planned`): onay dialogu → POST /schedule → detay yeniden yüklenir.
- **İptal Et** (`release.cancel`, terminal olmayan durumlarda): onay dialogu → POST /cancel → detay yeniden yüklenir.

## 6. Release Wizard

Mevcut çok-adımlı tasarım korunarak 4 adım: **Bilgiler → Değişiklikler → Dağıtım → Gözden Geçir & Oluştur.**
- **Adım 1:** gerçek müşteri→proje→ortam bağımlı zinciri, ad, sürüm, yayın türü, planlanan başlangıç/bitiş, iş/teknik sahip, açıklama; yayın yöneticisi geçerli kullanıcı (salt-okunur).
- **Adım 2 — Onaylı Değişiklik Seçimi:** yalnızca seçilen müşteri/proje/ortam için **onaylı (Approved)** değişiklikler (gerçek `ChangeApiService.list({status:'Approved',…})`). Çift-liste (dual-list) arayüzü: arama, çoklu seçim, **sıralı** seçim (dağıtım sırası), yukarı/aşağı taşıma, **duplicate önleme** (uygun liste seçilenleri hariç tutar). Müşteri/proje/ortam değişince seçim sıfırlanır (tutarlılık).
- **Adım 3 — Dağıtım Planı:** strateji, iletişim planı, rollback stratejisi, kesinti beklentisi + tahmini kesinti (dk), notlar.
- **Adım 4 — Gözden Geçir & Oluştur:** özet + `POST /api/releases`. Backend validasyon hataları (ör. "yalnızca Approved") normalize edilip gösterilir. Başarıda gerçek detaya yönlendirilir; taslak (`gms.release.draft`, yalnızca kaydedilmemiş form kurtarma) temizlenir.

> **Draft/Schedule notu:** Backend'te ayrı bir "Draft" yayın durumu yoktur — `POST` yayını **Planned** durumunda oluşturur (ilk kayıtlı hâl). Zamanlama (Schedule), yayın detayında `Planned→Scheduled` aksiyonudur. Bu nedenle sihirbazın birincil aksiyonu "Yayın Oluştur"dur; zamanlama detayda yapılır.

## 7. Otomatik Frontend Testleri

`core/release/release-integration.spec.ts` — **15 test** (Vitest + `HttpTestingController` + `apiErrorInterceptor`, gerçek backend'e bağımsız):
1. list pagination/filter param'ları (gizli pageSize yok) · 2. paged zarf item'ları · 3. boş sonuç · 4. create'in mapped DTO (changeIds + releaseManagerUserId, actorUserId yok) POST'u · 5. create'in gerçek detay dönüşü (releaseNo + backend risk) · 6. **schedule → Scheduled** · 7. cancel POST · 8. update'in RowVersion içermesi · 9. **409 → concurrency conflict + standart mesaj** · 10. getById detay (items + risk) · 11. audit aktör adları · 12. **backend validasyon 400 (yalnızca-Approved kuralı) normalize** · 13. **onaylı-değişiklik seçimi ChangeApiService'i status=Approved + kapsam ile sorgular** · 14. servis localStorage'a yazmaz · 15. etiket yardımcıları + terminal-durum kümesi.

**Sonuç:** `Test Files 5 passed (5)`, `Tests 57 passed (57)` (15 yeni + 42 mevcut auth/Change/Workflow/app **hepsi yeşil**).

## 8. Gerçek Backend Uçtan Uca Test Sonuçları

Backend `localhost:18080`, frontend `localhost:18420`, `admin@gms.local` (release.create/schedule/cancel izinli):

| # | Senaryo | Sonuç |
|---|---|---|
| 1 | Gerçek Release listesi | ✅ 9 yayın, sunucu-taraflı filtre/sayfalama, müşteriler yüklü, Türkçe durum etiketleri |
| 2 | Sihirbaz referans veri | ✅ Müşteri (Abdi İbrahim) → Proje (EBR Migration) → Ortam (DEV) bağımlı zinciri |
| 3 | Onaylı değişiklik havuzu | ✅ Yalnızca **CHG-2026-000052** (backend-Approved) göründü — keyfî değişiklik seçilemez |
| 4 | Yayın oluştur | ✅ **REL-2026-000010** üretildi, gerçek detaya yönlendirdi |
| 5 | Backend risk/yönetici | ✅ Risk Düşük/10 (backend hesaplı), Yayın Yöneticisi "System Administrator" (geçerli kullanıcı) |
| 6 | Zamanla (Schedule) | ✅ Durum **Zamanlandı (Scheduled)**, Zamanla butonu kayboldu (Planned değil) |
| 7 | Detay güncellenmesi | ✅ Durum/özet güncellendi; değişiklik sayısı 1 |
| 8 | Denetim güncellenmesi | ✅ "Yayın zamanlandı" + "Yayın oluşturuldu" olayları, **gerçek aktör adı "System Administrator"** |

Kritik mission hedefi — **gerçek Change → Release → Schedule yaşam döngüsünün backend'e karşı doğrulanması** — eksiksiz karşılandı. Update/409, cancel akışları **15 birim testiyle** doğrulandı.

## 9. Bulunan ve Düzeltilen Hatalar

1. **i18n durum etiketi çakışması (e2e'de yakalandı):** `Scheduled` durumu badge kaydında yanlışlıkla "Planlandı" olarak etiketlenmişti — `Planned` ile aynı görünüyordu. Schedule sonrası badge değişmemiş gibi görünüyordu. Düzeltme: `Scheduled` → **"Zamanlandı"** (tr).
- **Eklenen badge durumları:** `InProgress` (Yürütülüyor), `Accepted` (Kabul Edildi) — Release yaşam döngüsü için badge kaydına ve i18n'e eklendi.
- **Eksik `releases` i18n scope'u:** önceki sprintlerden kalan bir boşluk; Release ekranları ham anahtar gösteriyordu. Yeni `public/i18n/releases/{tr,en}.json` scope'u (type/audit/risk/actions) oluşturuldu.

## 10. Mock / localStorage Temizliği

- **Release ekranları (liste/detay/sihirbaz)** artık mock `ReleaseService`'i kullanmaz — tamamı `ReleaseApiService` üzerinden gerçek backend.
- **İzin verilen storage:** sihirbaz taslağı (`gms.release.draft`, kaydedilmemiş form kurtarma).
- **`release.service.ts` (mock)** silinmedi çünkü **bu sprintte taşınmayacak** modüller (Dashboard, Hub, Doküman listesi, Reports) ve paylaşılan `release-vm.ts` yardımcıları (`relativeTime`/`dateLocale` — approval/validation/document/execution VM'leri kullanır) hâlâ ondan besleniyor; başlık yorumu güncellenerek artık Release ekranları için kaynak olmadığı belgelendi. Yeni Release kodu buraya **bağlanmaz**.
- Testle doğrulandı: servis Release domain durumunu localStorage'a **yazmaz** (test 14).

## 11. Bilinen Eksikler ve UX Notları

- **Ertelenen modüller mock kalıyor:** Dashboard/Hub/Documents/Reports hâlâ mock `ReleaseService`'ten beslenir; ilgili modüller taşınınca kaldırılacaktır. Release ekranları tamamen gerçek API'dir.
- **Liste türü/tarih filtresi yok** (backend desteklemiyor; bkz. §4).
- **Yayın yöneticisi seçimi** geçerli kullanıcıya sabittir (backend'te kullanıcı listesi ucu yok); farklı bir yönetici atamak için ileride bir kullanıcı-arama ucu gerekir.
- **Execution/Validation entegrasyonu** bu sprint kapsamında değil; Release detayındaki "Execution summary" backend'te ayrı bir alan olarak DTO'da yer almadığından gösterilmedi (Execution domaini frontend'e bağlandığında eklenecektir).
- **Doküman yükleme yok:** create yalnızca doküman meta gönderir (Documents entegrasyonu sonraki sprint); sihirbaz şu an doküman eklemeyi sunmaz.

## 12. Production Readiness

**Puan: 8.3 / 10 (Release Planning entegrasyonu için)**

**Güçlü yanlar:** odaklı `ReleaseApiService` + DTO-hizalı modeller; sunucu-taraflı sayfalama/filtre (gizli pageSize yok); debounce + switchMap ile stale iptal; backend-yetkili risk (Angular'da iş kuralı yok); RowVersion 409 UX; izne duyarlı aksiyonlar (direktifler + guard); yalnızca onaylı değişiklik seçimi (gerçek Change API); tam Change→Release→Schedule yaşam döngüsü gerçek backend'te doğrulandı; gerçek aktör adlı denetim; 0 hata/0 uyarı build; 57 test yeşil.

**Sınırlayan hususlar:** Dashboard/Hub/Documents/Reports hâlâ mock `ReleaseService`'e bağlı (kalıntı localStorage); liste türü/tarih filtresi yok; yayın yöneticisi seçimi sabit; doküman yükleme yok; backend aktör-adı eklemesi additif ve e2e ile doğrulandı (ayrı xUnit testi eklenmedi).

## 13. Sonraki Sprint Hazırlığı

Bu sprint, **Execution + Validation** frontend entegrasyonunun doğrudan yeniden kullanacağı deseni tamamladı:
- **Odaklı API servisi + DTO-hizalı model + label + i18n scope** kalıbı (Change, Workflow, Release'te üç kez kanıtlandı) → `DeploymentApiService`/`ValidationApiService` aynı şekilde eklenir.
- **Release ↔ Execution bağı:** Scheduled yayınlar artık gerçek backend'te; Execution ekranı bunları gerçek `releases` listesinden (status=Scheduled) çekebilir ve `DeploymentRun` başlatabilir.
- **Paylaşılan temel** (`PagedResult<T>`, sunucu-taraflı liste deseni, `ApiError` + 409 UX, request-state, izin guard/direktifleri) hazır ve yeniden kullanılabilir.
- **Referans veri servisi** ve **gerçek aktör-adlı denetim** deseni hazır.

Release Planning runtime deneyimi **üretim mimarisi kalitesindedir**; tam üretim hazırlığı, kalan modüllerin (Execution, Validation, Documents, Reports, Dashboard/Hub) gerçek backend'e taşınmasıyla tamamlanır.
