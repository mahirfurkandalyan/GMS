# Angular Frontend ↔ Gerçek Backend Authentication Entegrasyonu — Sprint Raporu

**Platform:** GMS (Kurumsal Yönetişim Yönetim Sistemi) — Angular 22 (standalone + signals) frontend, ASP.NET Core 8 Identity/RBAC backend.
**Kapsam:** Frontend'in gerçek GMS Identity/RBAC backend'ine bağlanması. Gerçek login, JWT access token, dönen (rotating) refresh token, kimlik doğrulamalı API çağrıları, current-user state, izin/rol guard'ları, session restorasyonu, logout/logout-all, 401/403 yönetimi, paylaşılan API hata modeli ve mock authentication temizliği. Bu, sonraki tüm modül entegrasyonlarının tekrar kullanacağı **paylaşılan frontend API temelidir**.
**Durum:** Tamamlandı. Angular production derlemesi **0 hata / 0 uyarı**. **16 otomatik test yeşil** (14 yeni auth + 2 güncellenmiş app). Gerçek backend'e (`localhost:5080`) karşı **uçtan uca senaryo başarılı** (login, RBAC 403, session restore, refresh rotation, logout, admin erişimi).

> **Kapsam sınırı (bilinçli):** UI yeniden tasarlanmadı, yeni iş modülü eklenmedi, mevcut tasarım sistemi ve rotalar korundu, çalışan backend API'leri değiştirilmedi. Mock/localStorage davranışı **yalnızca authentication ve paylaşılan API temeli** için kaldırıldı; feature modüllerinin mock'ları bu sprintte taşınmadı.

---

## 1. Yapılan Geliştirmeler

- **Ayrık sorumluluklu auth mimarisi** (tek dev "AuthService" değil): `TokenStorageService`, `AuthApiService`, `AuthStateService`, `authInterceptor`, `apiErrorInterceptor`, `authGuard`/`permissionGuard`/`roleGuard`, UI directive'leri.
- **Gerçek login akışı:** e-posta + parola → backend `POST /api/auth/login` → token çifti + current user.
- **JWT access token** bellekte tutulur; her API isteğine `Authorization: Bearer` eklenir (yalnızca kendi API origin'ine).
- **Dönen refresh token:** `POST /api/auth/refresh` ile access token yenilenir; eski refresh token backend'te iptal edilir (rotation).
- **Tek-uçuş (single-flight) refresh:** eşzamanlı N adet 401 tek bir refresh isteğini paylaşır (`shareReplay(1)` + in-flight observable).
- **Current-user state:** signal tabanlı `AuthStateService` — `user`, `isAuthenticated`, `roles`, `permissions`, `hasPermission/hasAnyPermission/hasAllPermissions/hasRole`.
- **Route guard'ları:** `authGuard` (kimlik), `permissionGuard` (izin), `roleGuard` (rol) — rota `data`'sından beslenir.
- **UI authorization directive'leri:** `*gmsHasPermission`, `*gmsHasAnyPermission`, `*gmsHasRole`.
- **Session restorasyonu:** `provideAppInitializer` ile açılışta oturum geri yüklenir → shell titremesi (flicker) yok.
- **Logout ve logout-all:** backend revoke + yerel state temizliği.
- **Change password:** profil ekranında; başarıda backend tüm refresh token'ları iptal eder → yerel oturum kapatılır, login'e yönlendirilir.
- **401/403 ayrımı:** 401 → sessiz refresh + tek retry, başarısızsa login'e; **403 → oturum korunur**, `/forbidden` sayfası.
- **Paylaşılan API hata modeli:** `normalizeHttpError` ile normalize edilmiş `ApiError`; 409 için standart Türkçe eşzamanlılık mesajı.
- **Merkezî backend URL** (`api.config.ts`, dev `http://localhost:5080/api`).
- **Rota izinleri:** koruma altındaki iş rotalarına `data.permission` uygulandı.
- **Mock authentication temizliği:** mock login, sabit kodlu kullanıcılar, localStorage kimliği, `X-Actor-User-Id` başlığı ve statik rol-adı kararları kaldırıldı; tek aktif authentication sistemi.
- **14 odaklı Angular testi** + baseline build onarımı (34 hata → 0).

## 2. Frontend Authentication Mimarisi

Sorumluluklar bilinçli olarak ayrıldı; tek bir dev sınıf yoktur:

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Token kalıcılığı | `core/auth/token-storage.service.ts` | Token'ların saklandığı **tek** yer. Access bellekte, refresh sessionStorage'da. |
| Ham HTTP | `core/auth/auth-api.service.ts` | Backend `login/refresh/logout/logout-all/me/change-password` çağrıları — state bilmez. |
| State sahibi | `core/auth/auth-state.service.ts` | Current user + `isAuthenticated`/`roles`/`permissions` + login/logout/refresh/restore yaşam döngüsü. |
| Bearer + refresh | `core/auth/auth.interceptor.ts` | Token ekleme + 401'de tek-uçuş refresh + tek retry. |
| Hata normalizasyonu | `core/api-error.interceptor.ts` + `core/api-error.ts` | Ham `HttpErrorResponse` → normalize `ApiError`. |
| Guard'lar | `core/auth/guards.ts` | `authGuard`/`permissionGuard`/`roleGuard`. |
| UI directive'leri | `core/auth/has-permission.directive.ts` | `*gmsHasPermission`/`*gmsHasAnyPermission`/`*gmsHasRole`. |
| Uyum cephesi (facade) | `core/auth.service.ts` | Eski feature bileşenlerinin kullandığı `currentUser`/`isLoggedIn` API'sini `AuthStateService`'e delege eder. |

Bileşenler ve guard'lar state'i **her zaman `AuthStateService`'ten** okur, storage'a asla dokunmaz. `AuthApiService` state tutmaz; `AuthStateService` ham HTTP kurmaz. `AuthService` yalnızca bir **uyum cephesidir** — 10+ feature dosyasını bu sprintte taşımamak için (mission: "her modülü aynı anda taşımaya çalışma") korundu; içinde **mock login, localStorage kimliği veya rol-adı sezgisi yoktur**.

## 3. Token Storage ve Refresh Kararı

`TokenStorageService` token'ların saklandığı **tek** yerdir; feature bileşenleri storage'a doğrudan erişmez.

- **Access token → yalnızca bellekte** (hiç kalıcılaştırılmaz). Kısa ömürlüdür, sekme kapanınca gider ve reload'da refresh token'dan yeniden alınır. Storage üzerinden XSS ile erişilemez.
- **Refresh token + son kullanma metadata'sı → sessionStorage** (`gms.auth.refreshToken`, `gms.auth.accessExpiresAt`, `gms.auth.refreshExpiresAt`). Böylece sayfa yenilemede oturum geri yüklenebilir. `localStorage` yerine `sessionStorage` seçildi — sekmeler arası kalıcılık olmadığından **XSS maruziyet penceresi daralır**.
- **Asla saklanmayan:** parolalar, parola hash'leri, imzalama anahtarları, tam hassas kullanıcı nesneleri.

**XSS riski açıkça belgelendi:** JavaScript'in erişebildiği herhangi bir token başarılı bir XSS ile sızdırılabilir. Bu PoC'de backend httpOnly-cookie auth kullanmadığından refresh token sessionStorage'dadır. Azaltıcılar: **kısa access-token ömrü + refresh rotation** (eski refresh token kullanımda iptal edilir) blast radius'u sınırlar. Üretim sertleştirme adımı: **httpOnly, Secure, SameSite çerezler**. `hasValidAccessToken()` 5 sn skew ile çalışır; `hasUsableRefreshToken()` restorasyonun anlamlı olup olmadığını açılışta belirler.

## 4. AuthState ve Session Restoration

`AuthStateService`, signal tabanlıdır: `userSig` (private) → `user` (readonly), `isAuthenticated = computed(user !== null)`, `roles`/`permissions` computed. Bileshenler bu signal'lara bağlanır.

**Session restoration** açılışta `provideAppInitializer` ile çalışır — shell'in "titremesini" (flicker) engellemek için router render'ından önce çözülür:

```ts
provideAppInitializer(() => {
  const auth = inject(AuthStateService);
  return firstValueFrom(auth.restoreSession().pipe(catchError(() => of(false))));
});
```

`restoreSession()` üç yol izler: (1) **geçerli access token** varsa `/me` ile doğrular; (2) access süresi dolmuş ama **kullanılabilir refresh token** varsa `refresh()` ile yeniler ve dönen kullanıcıyı adopte eder; (3) aksi halde oturumu temizler. Hata durumunda oturum temizlenip `false` döner (initializer asla reddetmez). Gerçek backend'te **tam sayfa yenilemede** dashboard hâlâ "Günaydın, Requester" göstererek refresh-token yolu doğrulandı.

## 5. Interceptor Mimarisi

İki fonksiyonel interceptor, **sıralaması kritik** olacak şekilde kaydedildi:

```ts
provideHttpClient(withFetch(), withInterceptors([apiErrorInterceptor, authInterceptor]))
```

- **`apiErrorInterceptor` DIŞTA:** yanıt yolunda en son çalışır, böylece hem orijinal hem retry sonrası hataları normalize eder.
- **`authInterceptor` İÇTE:** backend'e en yakın; 401'i **ham** görür ve retry `next`'i doğrudan backend'e gider (döngü yok).

**`authInterceptor` davranışı:**
- Bearer token yalnızca **kendi API origin'ine** eklenir; `login`/`refresh` uçlarına, `noAuth` bağlamlı isteklere ve **harici URL'lere eklenmez** (test #4 ile doğrulandı).
- **401** alınınca (403 değil, `noAuth` değil): `auth.refresh()` çağrılır → yeni access token ile istek **tek kez** yeniden denenir. Refresh başarısızsa iç `catchError` oturumu temizler ve `/login`'e yönlendirir. **Sonsuz döngü yoktur** (retry doğrudan backend'e, ikinci 401 refresh tetiklemez).
- **Tek-uçuş refresh:** `AuthStateService.refresh()` in-flight observable'ı paylaşır; eşzamanlı iki 401 **tek** refresh isteği üretir (test #5 ile doğrulandı).
- **403 asla refresh tetiklemez ve oturumu kapatmaz** (test #10).

## 6. Route Guard ve Permission Sistemi

Üç fonksiyonel guard (`CanActivateFn`), tümü `AuthStateService`'ten okur (asla storage'dan):

- **`authGuard`:** kimlik doğrulanmamışsa `/login`'e `returnUrl` query param'ı ile yönlendirir (`UrlTree` döner).
- **`permissionGuard`:** rota `data`'sından `permission` (tek) **veya** `permissions` + `permissionMode: 'all' | 'any'` okur. Yetki yoksa `/forbidden`'a yönlendirir.
- **`roleGuard`:** rota `data.role`/`data.roles` ile rol kontrolü.

**Uygulanan rota izinleri** (`app.routes.ts`): releases (`release.read`), releases/new (`release.create`), changes (`change.read`), changes/new (`change.create`), approvals (`approval.read`), validation (`validation.read`), executions (`execution.read`), documents (`document.read`), audit (`audit.read`), reports (`report.read`), workflows (`workflow.definition.read`), notifications (`notification.read`), admin/notification-rules (`notification.template.manage`), administration (`['admin.users.read','admin.roles.read']`, mode `any`). Kök yönlendirme `isAuthenticated()`'e göre yapılır. Hub/profile/dashboard/assets/employees/organization/leave/training için `authGuard` korunur.

## 7. UI Authorization Directive'leri

`has-permission.directive.ts` içinde ortak bir `ToggleDirective` tabanı ve üç yapısal directive var; hepsi `effect()` ile `AuthStateService` signal'larına reaktiftir (kullanıcı/izin değişince DOM otomatik güncellenir):

- `*gmsHasPermission="'change.create'"` — tek izin.
- `*gmsHasAnyPermission="['a','b']"` — en az biri.
- `*gmsHasRole="'Admin'"` — rol.

Element yalnızca yetki varken DOM'a eklenir. Test #14: izin yokken `.create` butonu **yok**, izin verilince **görünür**.

## 8. Login / Logout / Change-Password Akışları

- **Login** (`features/auth/login.ts`): e-posta/parola signal'ları, `showPassword` toggle, `canSubmit` computed. `AuthStateService.login()` çağrılır; hata `messageFor(err)` ile güvenli/genel mesaja çevrilir (kimlik bilgisi sızdırılmaz). `returnUrl` yalnızca **göreli** olduğunda kullanılır (açık yönlendirme koruması).
- **Logout** (shell topbar `app.ts`): `authState.logout()` → backend revoke (best-effort) → yerel temizlik → `/login`. `logoutAll()` de mevcut.
- **Change password** (`features/profile/profile.ts`): `currentPassword`/`newPassword`/`confirmPassword` doğrulaması (min 8, eşleşme). Başarıda backend tüm refresh token'ları iptal ettiği için `clearSession()` + `/login?pwChanged=1`. 400/validation hatası "mevcut parola" mesajına eşlenir.

## 9. 401/403 ve Hata Yönetimi

**Paylaşılan API hata modeli** (`core/api-error.ts`): `ApiErrorKind` (`unauthenticated` | `forbidden` | `validation` | `conflict` | `notFound` | `server` | `network` | `unknown`), `ApiError` arayüzü, `normalizeHttpError(HttpErrorResponse)` ve `isConcurrencyError`. **409** için standart mesaj:

> "Kayıt başka bir kullanıcı tarafından güncellendi. Güncel veriyi yükleyip tekrar deneyin."

Bu, sonraki modüllerin (Change/Workflow) optimistic-concurrency yönetimi için **hazır bir temeldir**.

**401 vs 403 deneyimi:** 401, arkaplanda sessiz refresh + tek retry ile toparlanmaya çalışılır; kalıcıysa oturum temizlenip `/login`'e gidilir. **403 oturumu kapatmaz** — kullanıcı giriş yapmıştır ama o kaynağa yetkisi yoktur; `/forbidden` sayfası (`features/errors/forbidden.ts`) "Erişim reddedildi… Oturumunuz açık kalmaya devam ediyor" mesajıyla gösterilir ve `Location` ile geri dönüş sunar.

## 10. Mock Authentication Temizliği

Kaldırılanlar:
- **Silinen dosyalar:** `core/auth-jwt.service.ts`, `core/jwt.interceptor.ts`.
- **Mock login, sabit kodlu kullanıcı listeleri, localStorage kimliği, `X-Actor-User-Id` başlığı, statik rol-adı kararları** — hepsi kaldırıldı.
- Eski `AuthService`, `AuthStateService`'e delege eden ince bir **cepheye** dönüştürüldü (mock içermez).
- Frontend artık backend'in Development-only `/api/auth/mock-users` ucunu **kullanmıyor**.

**Doğrulama:** kod tabanında `X-Actor-User-Id`, `mock-users`, `auth-jwt.service`, `jwt.interceptor`, mock login için **hiçbir eşleşme yok**. Kalan `localStorage` kullanımları yalnızca **auth dışı** feature mock'ları (asset/approval/change/document/execution/release/validation/workflow/admin), UI tercihleri (sidebar, dil) ve sihirbaz taslaklarıdır — mission gereği bu sprintte **bilinçli korundu**. Böylece tek aktif authentication sistemi backend JWT/RBAC'tir.

## 11. Değiştirilen Dosyalar

**Yeni:**
- `core/api.config.ts` (API_BASE_URL/API_ORIGIN/PagedResult)
- `core/api-error.ts`, `core/api-error.interceptor.ts`
- `core/auth/token-storage.service.ts`
- `core/auth/auth.models.ts`
- `core/auth/auth-api.service.ts`
- `core/auth/auth-state.service.ts`
- `core/auth/auth.interceptor.ts`
- `core/auth/guards.ts`
- `core/auth/has-permission.directive.ts`
- `core/auth/auth.spec.ts` (14 test)
- `features/errors/forbidden.ts`

**Yeniden yazılan / güncellenen:**
- `core/auth.service.ts` (mock → cephe)
- `features/auth/login.ts` + `login.html` + `login.scss`
- `features/profile/profile.ts` + `profile.html`
- `app.config.ts` (interceptor'lar + `provideAppInitializer`)
- `app.routes.ts` (rota izinleri + `/forbidden` + kök yönlendirme)
- `app.ts` (shell logout/logout-all)
- `app.spec.ts` (storage polyfill + TranslocoTestingModule)
- `public/i18n/tr.json` + `en.json` (login anahtarları)
- `public/i18n/profile/tr.json` + `en.json` (security anahtarları)
- Baseline build için düzeltilen feature dosyaları: change-detail, change-wizard(.ts/.html), change-list.html, approval-list, approval-vm, asset-detail, asset-list, audit-detail(.ts/.html), document-detail, document-list, validation-detail(.ts/.html), workflow-list.

**Silinen:** `core/auth-jwt.service.ts`, `core/jwt.interceptor.ts`.

## 12. Otomatik Frontend Testleri ve Sonuçları

Test altyapısı: **Vitest** (`@angular/build:unit-test`), `HttpTestingController`, `TestBed.runInInjectionContext`, gerçek interceptor zinciri (`withInterceptors([apiErrorInterceptor, authInterceptor])`).

`core/auth/auth.spec.ts` — **14 test:**
1. Başarılı login oturumu ve kullanıcıyı saklar.
2. Başarısız login normalize hata (`kind='unauthenticated'`) döndürür, kullanıcı anonim kalır.
3. Interceptor API isteklerine `Bearer` ekler.
4. Harici URL'lere token **eklemez**.
5. Eşzamanlı 401'ler için **tek** refresh (ve retry'ler yeni token'ı taşır).
6. Başarısız refresh oturumu temizler.
7. `authGuard` anonim kullanıcıyı `/login`'e yönlendirir.
8. `permissionGuard` yetkili kullanıcıya izin verir.
9. `permissionGuard` yetkisizi `/forbidden`'a yönlendirir.
10. **403 refresh tetiklemez ve oturumu kapatmaz.**
11. Logout token'ları ve kullanıcıyı temizler.
12. Geçerli access token ile restore `/me` çağırır.
13. Süresi dolmuş access token refresh ile restore eder.
14. `HasPermissionDirective` izinle gösterir / izinsiz gizler.

`app.spec.ts` — **2 test:** uygulama oluşur; anonimken shell render edilmez.

**Sonuç:** `Test Files 2 passed (2)`, `Tests 16 passed (16)`, süre ~3.4 sn.

## 13. Gerçek Backend Uçtan Uca Test Sonuçları

Backend `localhost:5080`, frontend `localhost:4200`, seed kullanıcılar `@gms.local` / `Gms.Dev.2026!`. Tarayıcı üzerinden doğrulanan adımlar:

| # | Senaryo | Sonuç |
|---|---|---|
| 1 | `requester@gms.local` ile login | ✅ GMS Hub açıldı, "Günaydın, Requester" (gerçek kimlik doğrulanmış kullanıcı) |
| 2 | Requester → `/administration` | ✅ **403 Forbidden** sayfası, oturum korundu |
| 3 | Tam sayfa yenileme (`/dashboard`) | ✅ Oturum refresh token ile geri yüklendi ("Günaydın, Requester") |
| 4 | Refresh rotation (ağ) | ✅ `POST /api/auth/refresh → 200` (yenilemelerde otomatik) |
| 5 | Logout (topbar) | ✅ `/login`'e yönlendirme, "Giriş — GMS" |
| 6 | `admin@gms.local` ile login → `/administration` | ✅ Administration ekranı açıldı (permissionGuard izin verdi) |
| 7 | Kimliksiz `GET /api/auth/me` | ✅ **401** (backend gerçek JWT doğruluyor, mock yok) |
| 8 | sessionStorage refresh token | ✅ `gms.auth.refreshToken` mevcut; access token bellekte (persist edilmiyor) |

Backend denetim aktörü, önceki backend sprintlerinde JWT kullanıcısı olarak doğrulanmıştı; frontend artık `X-Actor-User-Id` göndermediğinden aktör **yalnızca JWT'den** çözülür.

## 14. Bulunan ve Düzeltilen Hatalar

- **34 baseline build hatası** (yarım kalmış i18n refactor'ından): template'lerde signal'lara `()`, `labelKey` → `| transloco`, VM yardımcılarına `TranslocoService`/`LanguageService` enjekte edip `t`/`locale` geçirme, `computed<Crumb[]>` tiplemesi, sahip/oluşturan alanları için `String(...)` zorlaması, `severityText` düzeltmesi. → **0 hata**.
- **2 kullanılmayan import uyarısı** (audit-detail/validation-detail'de `TranslocoPipe`; profile'da `GmsEmptyState`) → kaldırıldı → **0 uyarı**.
- **`auth.spec.ts` bozuk `describe`** (async olmayan `beforeEach` içinde `await import`) → statik import'a çevrildi.
- **`app.spec.ts` `localStorage.clear is not a function`** (Node 25 kısmi Storage stub'ı) → bellek-içi Storage polyfill.
- **`app.spec.ts` `No provider for TRANSLOCO_TRANSPILER`** → `TranslocoTestingModule.forRoot(...)` eklendi.

TypeScript strictness **zayıflatılmadı**; yalnızca auth entegrasyonunu veya baseline derlemeyi bloke eden hatalar düzeltildi.

## 15. Bilinen Eksikler ve Güvenlik Notları

- **Token depolama (XSS):** refresh token sessionStorage'da (bkz. Bölüm 3). Üretim sertleştirmesi: **httpOnly + Secure + SameSite çerezler**. Mevcut azaltıcılar: kısa access ömrü + refresh rotation.
- **Feature modülleri hâlâ mock:** Change/Release/Approval/Document/Execution/Validation/Workflow/Asset servisleri localStorage tabanlı mock'tur — mission gereği bu sprintte taşınmadı. Paylaşılan API temeli (base URL, interceptor'lar, `ApiError`, `PagedResult`) bunların taşınması için hazırdır.
- **`AuthService` cephesi:** eski bileşenlerin geçişini kolaylaştırmak için kasıtlı bir uyum katmanıdır; modül taşımaları ilerledikçe kaldırılabilir.
- **Frontend correlation-id üretimi:** bilinçli olarak eklenmedi (opsiyoneldi); backend `X-Correlation-Id` üretir/yansıtır.
- **`/api/auth/mock-users`:** backend'te Development-only olarak durmakta ama frontend artık kullanmıyor; ileride kaldırılabilir.

## 16. Change + Workflow Frontend Entegrasyonu İçin Hazır Olma Durumu

Bu sprint, sonraki modüllerin **doğrudan yeniden kullanacağı** temeli kurdu:
- **Merkezî `API_BASE_URL`** ve `API_ORIGIN` — yeni servisler tek yerden URL alır.
- **Kimlik doğrulamalı HTTP** — `authInterceptor` her API çağrısına Bearer ekler; yeni servis "sadece çalışır".
- **`ApiError` + 409 eşzamanlılık mesajı** — Change/Workflow'un optimistic-concurrency (RowVersion) yönetimi için hazır sözleşme.
- **`PagedResult<T>`** — listeleme uçları için ortak tip.
- **İzin/rol guard'ları + directive'ler** — Change/Workflow rotaları ve butonları RBAC'a hazır.
- **401/403 davranışı** — yeni uçlar ek kod olmadan tutarlı deneyim alır.

Change ve Workflow entegrasyonu için önerilen bir sonraki adım: mevcut `ChangeService`/`WorkflowService` mock'larını `HttpClient` + `API_BASE_URL` kullanan gerçek servislerle değiştirmek; `AuthService` cephesine olan bağımlılıkları kademeli olarak `AuthStateService`'e taşımak.

## 17. Frontend Production Readiness Puanı

**Puan: 8.0 / 10 (paylaşılan auth/API temeli için)**

**Güçlü yanlar:** ayrık sorumluluklu mimari; bellek-içi access + rotation'lı refresh; tek-uçuş refresh; net 401/403 ayrımı; APP_INITIALIZER ile titremesiz restorasyon; RBAC guard + directive'ler; normalize `ApiError`; mock auth tam temizliği; 0 hata/0 uyarı build; 16 test yeşil; gerçek backend e2e doğrulaması.

**Puanı sınırlayan (üretim öncesi) hususlar:** refresh token sessionStorage'da (httpOnly çerez hedefi); feature modülleri hâlâ mock (yalnızca auth temeli gerçek); `AuthService` cephesi geçici; e2e otomatik değil (manuel/tarayıcı doğrulaması).

Authentication ve paylaşılan API temeli **üretim mimarisi kalitesindedir**; tam üretim hazırlığı, çerez tabanlı token sertleştirmesi ve feature modüllerinin gerçek backend'e taşınmasıyla tamamlanır.
