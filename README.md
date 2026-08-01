# VaultLedger

Kapalı devre (closed-loop) ödeme sistemi — kurum/kampüs/etkinlik alanı içinde geçerli bakiye ve harcama yönetimi.

## Teknoloji yığını

- .NET 8 (C#) — RESTful API
- PostgreSQL + Entity Framework Core (Code-First, Migrations)
- JWT (access + refresh token)
- Clean Architecture / katmanlı mimari

## Solution yapısı

```
VaultLedger/
├── src/
│   ├── VaultLedger.Domain/
│   ├── VaultLedger.Application/
│   ├── VaultLedger.Infrastructure/
│   └── VaultLedger.API/
├── frontend/                     # React + Vite + TypeScript
├── tests/
│   ├── VaultLedger.UnitTests/
│   └── VaultLedger.IntegrationTests/
└── .github/workflows/tests.yml
```

### Bağımlılık yönü

`API → Application → Domain`  
`Infrastructure → Application → Domain`  
`API → Infrastructure` (DI kayıtları)  
`frontend → API` (HTTP / JWT)

## Geliştirme durumu

- [x] Adım 1 — Solution / katman iskeleti
- [x] Adım 2 — Domain modelleri + DbContext (+ System Clearing seed)
- [x] Adım 3 — İlk migration (`InitialCreate`) — henüz DB'ye uygulanmadı
- [x] Adım 4 — Repository + Unit of Work
- [x] Adım 5a — TransactionService (Spend / TopUp / Refund) + FOR UPDATE
- [x] Adım 5b — Transfer (ID-sıralı kilitleme, deadlock önleme)
- [x] Adım 6 — API (Auth JWT, middleware, controllers, Swagger)
- [x] Adım 7 — Integration tests (Testcontainers + Respawn + GitHub Actions)
- [x] Adım 8 — Güvenlik sertleştirme (rate limit, headers, CORS, secrets, refresh rotation)
- [x] Frontend iskeleti — React/Vite, auth, accounts/cards/transactions/admin
- [x] Playwright E2E — Testcontainers/CI Postgres + 5 kritik senaryo

## Hızlı başlangıç

```bash
dotnet restore
dotnet build
dotnet test

# Frontend
cd frontend
npm install
npm run dev
```

API varsayılan: `https://localhost:7126` · UI: `http://localhost:5173`

### Gizli değerleri ayarlama (zorunlu)

`appsettings.json` içinde JWT secret / DB connection string **boş** bırakılır; gerçek değerler GitHub'a commit edilmez.

Yerel geliştirme:

1. Örneği kopyalayın:
   ```bash
   cp src/VaultLedger.API/appsettings.Development.json.example src/VaultLedger.API/appsettings.Development.json
   ```
2. Dosyadaki `DefaultConnection`, `Jwt:Secret` ve `Cors:AllowedOrigins` değerlerini kendi ortamınıza göre düzenleyin.
3. `appsettings.Development.json` ve `appsettings.Production.json` `.gitignore` içindedir.

Alternatifler:

- **User Secrets** (`dotnet user-secrets set "Jwt:Secret" "..."`)
- **Environment variables** (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Cors__AllowedOrigins__0`, …)

Production'da aynı secret'ları User Secrets / ortam değişkenleri / gitignore'lu `appsettings.Production.json` ile sağlayın.
