# Scouting Platform — Setup

## Gereksinimler
- .NET 9 SDK
- PostgreSQL 15+
- Node.js 18+ (Tailwind CSS build için)

## Hızlı Başlangıç

### 1. scommon submodule'ü başlat
```bash
git submodule update --init --recursive
```

### 2. PostgreSQL veritabanı oluştur
```sql
CREATE DATABASE scouting;
CREATE SCHEMA scouting;
```

### 3. EF Core migration çalıştır
```bash
cd src/Scouting.API
dotnet ef migrations add InitialCreate --output-dir Infrastructure/Migrations
dotnet ef database update
```

### 4. Tailwind CSS build et
```bash
cd src/Scouting.Web
npm install
npm run build:css
```

### 5. Projeyi başlat

**API (port 5001):**
```bash
cd src/Scouting.API
dotnet run
```

**Web (port 5002):**
```bash
cd src/Scouting.Web
dotnet run
```

Tailwind geliştirme sırasında izlemek için:
```bash
npm run watch:css
```

## Ortam Değişkenleri

`src/Scouting.API/appsettings.json` içinde:
- `ConnectionStrings:Default` — PostgreSQL bağlantı dizesi
- `Jwt:Key` — JWT imzalama anahtarı (**üretimde mutlaka değiştirin**)

`src/Scouting.Web/appsettings.json` içinde:
- `ApiBaseUrl` — API adresi (varsayılan: `http://localhost:5001/`)

## API Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | /api/auth/register | Kayıt |
| POST | /api/auth/login | Giriş (JWT döner) |
| GET | /api/players | Oyuncu listesi |
| GET | /api/players/top | En çok oy alanlar |
| GET | /api/players/{slug} | Oyuncu detayı |
| POST | /api/players | Oyuncu öner (auth) |
| POST | /api/players/{id}/approve | Admin onay |
| POST | /api/players/{id}/reject | Admin red |
| GET | /api/analyses/recent | Son analizler |
| POST | /api/votes | Oy ver (auth) |
| GET | /api/scouters/top | Top scoutlar |
| GET | /api/scouters/{username} | Scout profili |
| POST | /api/scouters/{id}/follow | Takip et (auth) |
| DELETE | /api/scouters/{id}/follow | Takibi bırak (auth) |
| GET | /api/scouters/me/following | Takip ettiklerim |

## Proje Yapısı

```
Scouting.sln
├── libs/scommon/           # Custom CQRS & Mediator kütüphanesi
├── src/
│   ├── Scouting.API/       # .NET 9 Web API
│   │   ├── Controllers/    # Slim controller'lar
│   │   ├── Resources/      # Vertical Slice (CQRS features)
│   │   ├── Domains/        # EF Core entities
│   │   ├── Services/       # CurrentUserService
│   │   ├── Infrastructure/ # DbContext, DI, Schema
│   │   └── Shared/         # Result modelleri, ErrorCodes
│   └── Scouting.Web/       # Blazor Web App (SSR + Server Interactive)
│       ├── Components/     # Razor components
│       │   ├── Pages/      # Rotalar
│       │   ├── Layout/     # NavMenu, MainLayout
│       │   └── Shared/     # PlayerCard, AnalysisCard
│       ├── Services/       # ScoutingApiService
│       ├── Models/         # API DTO'ları
│       └── Styles/         # Tailwind CSS input
```
