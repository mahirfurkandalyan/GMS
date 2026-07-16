# GMS — Governance Management System

**Kurumsal Yönetişim Yönetim Sistemi** — Centra Yazılım

Bu depo, GMS platformunun **PoC (Proof of Concept)** iskeletidir. Amaç, gerçek
teknolojiler kullanarak çalışan, kurumsal düzeyde bir proje temeli oluşturmaktır.
Bu adımda yalnızca **proje kurulumu** yapılmıştır; iş modülleri henüz eklenmemiştir.

> Bu bir SaaS ürünü değildir. Kurum içi bir yönetişim platformu PoC'sidir.

---

## Teknoloji Yığını

| Katman        | Teknoloji                              |
| ------------- | -------------------------------------- |
| Frontend      | Angular (standalone, SCSS)             |
| Backend       | ASP.NET Core Web API (.NET 8)          |
| Veritabanı    | SQL Server 2022                        |
| ORM           | Entity Framework Core                  |
| API Dokümanı  | Swagger / OpenAPI                      |
| Kimlik Doğrulama (PoC) | Mock login / rol simülasyonu (ilerleyen adımda) |

---

## Proje Yapısı

```
gms-poc/
├── backend/
│   └── Gms.Api/            # ASP.NET Core Web API
│       ├── Data/           # EF Core DbContext
│       ├── Properties/     # launchSettings.json
│       ├── Program.cs      # Uygulama giriş noktası, CORS, Swagger, /health
│       ├── appsettings.json
│       └── Gms.Api.csproj
├── frontend/
│   └── gms-ui/             # Angular uygulaması (sidebar + topbar + içerik)
├── docker-compose.yml      # SQL Server servisi
└── README.md
```

---

## Ön Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) ve npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (SQL Server için)
- (Opsiyonel) EF Core araçları: `dotnet tool install --global dotnet-ef`

---

## Hızlı Başlangıç

```bash
# 1) SQL Server
docker compose up -d

# 2) Backend (yeni terminal)
cd backend/Gms.Api
dotnet restore
dotnet ef migrations add InitialIdentity
dotnet ef database update
dotnet run

# 3) Frontend (yeni terminal)
cd frontend/gms-ui
npm install
npm start
```

Ardından `http://localhost:4200` adresinde giriş ekranı açılır.
Ayrıntılar için aşağıdaki bölümlere bakın.

---

## 1) SQL Server'ı Başlatma (Docker)

SQL Server, `docker-compose` ile yerel olarak çalıştırılır:

```bash
# Depo kök dizininde
docker compose up -d

# Durumu kontrol et
docker compose ps

# Durdurmak için
docker compose down
```

Bağlantı bilgileri (varsayılan):

| Ayar     | Değer                    |
| -------- | ------------------------ |
| Sunucu   | `localhost,1433`         |
| Kullanıcı| `sa`                     |
| Şifre    | `Your_strong_Passw0rd`   |
| Veritabanı | `GmsDb`                |

> Bağlantı dizesi `backend/Gms.Api/appsettings.json` içinde tanımlıdır.

---

## 2) Backend'i Çalıştırma (ASP.NET Core Web API)

```bash
cd backend/Gms.Api

# Paketleri geri yükle
dotnet restore

# API'yi çalıştır
dotnet run
```

Çalıştıktan sonra:

- Swagger UI: `http://localhost:5080/swagger`
- Sağlık kontrolü: `http://localhost:5080/health`

`/health` yanıtı:

```json
{ "status": "Healthy", "service": "GMS API", "timestamp": "..." }
```

---

## 3) Frontend'i Çalıştırma (Angular)

```bash
cd frontend/gms-ui

# Bağımlılıkları yükle (ilk seferde)
npm install

# Geliştirme sunucusunu başlat
npm start
```

Uygulama: `http://localhost:4200`

Uygulama Türkçedir. Giriş sonrası kullanıcı **GMS Hub** (`/hub`) sayfasına düşer;
Hub, çalışma alanının merkezidir (karşılama, profil kartı, iş özeti, hızlı aksiyonlar,
aktif projeler ve son aktiviteler).

Gezinme (gruplu sidebar + üstte global arama ve bildirim çanı):

- **GMS Hub** (`/hub`) — giriş sonrası varsayılan sayfa
- **Gösterge Paneli** (`/dashboard`) — yenilenmiş; karşılama, hızlı aksiyonlar, KPI'lar, görevler, aktiviteler, duyurular
- **Yayın Yönetimi** (`/releases`)
- **Eğitimler** (`/training`) — atanan/tamamlanan/yaklaşan eğitimler + sertifikalar
- **Bildirimler** (`/notifications`) — bildirim merkezi
- **Çalışanlar** (`/employees`, detay `/employees/:id`) — kart listesi, filtreler, profil
- **Departmanlar** (`/organization/departments`), **Takımlar** (`/organization/teams`), **Organizasyon Şeması** (`/organization/chart`)
- **İzin Takvimi** (`/leave`) — rol bazlı görünürlük (bakiyeler yalnızca yönetici/admin)
- **Profilim** (`/profile`)
- Değişiklik/Onay/Yürütme modülleri sidebar'da *Yakında* olarak işaretlidir

Kök yol (`/`) girişe göre yönlenir: oturum varsa `/hub`, yoksa `/login`.

> **Tasarım:** Ortak bir tasarım sistemi (`src/styles.scss` içindeki tasarım token'ları
> + paylaşılan `.gms-*` sınıfları) kullanılır. Employees, Organizasyon, İzin, Eğitim,
> Bildirim ve Arama modülleri şu an **mock veri** ile çalışır (backend değişikliği yok).

---

## 4) Veritabanı Migrasyonları

Model iki aşamada oluşur: kimlik/rol (`AppUser`, `Role`, `UserRole`) ve iş temeli
(`Customer`, `Project`, `Environment`, `Release`). Migrasyonları sırasıyla oluşturun:

```bash
cd backend/Gms.Api

# EF Core araçları kurulu değilse (tek seferlik):
dotnet tool install --global dotnet-ef

# 1) Kimlik/rol modeli
dotnet ef migrations add InitialIdentity

# 2) İş temeli (Customer / Project / Environment / Release)
dotnet ef migrations add InitialBusinessFoundation

# 3) Change Management domaini (ChangeRequest + Revision/Asset/Document/AuditEvent)
dotnet ef migrations add InitialChangeDomain

# 4) Approval Management domaini (ApprovalRequest + Step/Decision/AuditEvent)
dotnet ef migrations add InitialApprovalDomain

# 5) Release Planning domaini (ReleasePlan + Item/DeploymentPlan/Document/AuditEvent)
#    NOT: eski ince "Release" entity'si kaldırıldı; ReleasePlan artık tek gerçek kaynaktır.
dotnet ef migrations add ReleasePlanningDomain

# Veritabanına uygula (tüm seed verileri de eklenir)
dotnet ef database update
```

> Migrasyon çalıştırılmadan önce SQL Server konteynerinin ayakta olması gerekir
> (bkz. adım 1). `database update` sonrası kullanıcılar/roller ile birlikte
> 2 müşteri, 2 proje, 8 ortam, 2 örnek yayın ve **3 örnek değişiklik (ChangeRequest)**
> seed edilir (Approval kaydı seed edilmez; onaylar submit ile runtime'da oluşur).
>
> **Change** endpoint'leri Swagger'da `ChangeRequests` etiketi altındadır:
> `GET/POST /api/change-requests`, `GET /api/change-requests/{id}`,
> `POST /api/change-requests/{id}/submit | cancel | revisions`,
> `GET /api/change-requests/{id}/audit`.
> Submit artık bir **onay talebi** oluşturur; değişiklik `UnderReview` durumuna geçer.
>
> **Approval** endpoint'leri `Approvals` etiketi altındadır:
> `GET /api/approvals`, `GET /api/approvals/{id}`,
> `GET /api/approvals/by-change/{changeRequestId}`,
> `POST /api/approvals/{id}/approve | reject | request-revision`,
> `GET /api/approvals/{id}/audit`.
>
> **Release Planning** (`ReleasePlan` = tek gerçek kaynak) endpoint'leri `Releases`
> etiketi altındadır: `GET/POST /api/releases`, `GET/PUT /api/releases/{id}`,
> `GET /api/releases/{id}/audit`,
> `POST /api/releases/{id}/schedule | complete | cancel`.
> Bir yayın **yalnızca `Approved` durumundaki** değişikliklerden oluşturulabilir;
> oluşturulunca değişiklikler `Scheduled`, tamamlanınca `Implemented`, iptal edilince
> yeniden `Approved` olur.

---

## 5) Mock Kimlik Doğrulama / Rol Simülasyonu

Bu PoC'de **gerçek kimlik doğrulama yoktur** (Entra ID / JWT kullanılmaz).
Kullanıcı, giriş ekranındaki listeden seçilir ve `localStorage`'da saklanır.

**API uç noktaları:**

| Metot | Yol                     | Açıklama                                   |
| ----- | ----------------------- | ------------------------------------------ |
| GET   | `/api/auth/mock-users`  | Seed edilmiş kullanıcıları rolleriyle döner |
| POST  | `/api/auth/mock-login`  | Seçilen kullanıcı ile giriş simüle eder     |

`GET /api/auth/mock-users` yanıtı:

```json
[
  { "id": "...", "fullName": "Requester User", "email": "requester@gms.local", "roles": ["Requester"] }
]
```

`POST /api/auth/mock-login` gövdesi: `{ "userId": "<guid>" }` — kullanıcı yoksa **404**.

Yanıt:

```json
{ "userId": "...", "fullName": "...", "email": "...", "roles": ["Requester"] }
```

**Seed edilen kullanıcılar / roller:**

| Kullanıcı              | E-posta                | Rol       |
| ---------------------- | ---------------------- | --------- |
| Requester User         | requester@gms.local    | Requester |
| Architect User         | architect@gms.local    | Architect |
| Executor User          | executor@gms.local     | Executor  |
| QA Specialist          | qa@gms.local           | QA        |
| System Administrator   | admin@gms.local        | Admin     |

**Akış:** Giriş ekranı → kullanıcı seç → *Giriş Yap* → Gösterge Paneli.
Topbar aktif kullanıcı adını ve rolünü gösterir; *Çıkış Yap* ile oturum kapatılır.
Aktif kullanıcı yoksa panel rotası otomatik olarak giriş ekranına yönlendirir.

---

## 6) Yayın Yönetimi (Release Management)

İlk iş temeli: **Müşteri → Proje → Ortam → Yayın** hiyerarşisi.

**API uç noktaları:**

| Metot | Yol                                        | Açıklama                          |
| ----- | ------------------------------------------ | --------------------------------- |
| GET   | `/api/customers`                           | Müşteriler                        |
| GET   | `/api/projects` `?customerId=`             | Projeler (müşteriye göre filtre)  |
| GET   | `/api/environments` `?projectId=`          | Ortamlar (projeye göre filtre)    |
| GET   | `/api/releases`                            | Tüm yayınlar                      |
| GET   | `/api/releases/{id}`                        | Tek yayın (yoksa 404)             |
| POST  | `/api/releases`                            | Yeni yayın (durum: Draft)         |
| PUT   | `/api/releases/{id}`                        | Yayın güncelle                    |

Yayın durumları: `Draft`, `Planned`, `InReview`, `Approved`, `Completed`, `Cancelled`.

Sidebar'daki **Yayın Yönetimi** (`/releases`) sayfasında yayınlar listelenir ve
**Yeni Yayın Oluştur** formu ile yeni yayın eklenir (ortam listesi, seçilen projeye
göre filtrelenir). Gösterge Paneli'ndeki **Planlanan Yayınlar** KPI'ı, durumu
`Planned` olan yayın sayısını gösterir.

---

## Notlar

- Değişiklik talebi, onay ve yürütme modülleri bu adımda **kasıtlı olarak** eklenmemiştir.
- CORS, Angular yerel geliştirme sunucusu (`http://localhost:4200`) için açıktır.
- Backend ve frontend bu aşamada tam olarak dockerize edilmemiştir; yalnızca
  SQL Server konteyner üzerinden çalışır.

---

## Planlanan Modüller

- Gösterge Paneli
- Sürüm Yönetimi (Release Management)
- Değişiklik Talep Yönetimi (Change Request Management)
- Şablon Seçimi
- Risk / Doğrulama Simülasyonu
- Onay Simülasyonu
- Yürütme Simülasyonu
- Denetim Zaman Çizelgesi (Audit Timeline)
