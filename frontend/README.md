# VaultLedger Frontend

React + Vite + TypeScript istemci. Backend: `VaultLedger.API`.

## Çalıştırma

```bash
# API (ayrı terminal)
dotnet run --project src/VaultLedger.API

# Frontend
cd frontend
cp .env.example .env   # isteğe bağlı
npm install
npm run dev
```

Varsayılan: http://localhost:5173  
Vite proxy, `/auth`, `/accounts`, `/cards`, `/transactions` isteklerini `https://localhost:7126` adresine iletir.

## Yapı

```
frontend/src/
├── api/           # axios client + endpoint fonksiyonları
├── auth/          # AuthContext, login/register
├── components/    # AppShell + paylaşılan UI
├── features/      # accounts, cards, transactions, admin
├── routes/        # ProtectedRoute, AdminRoute, GuestRoute
├── types/         # backend DTO karşılıkları
└── App.tsx
```

## Notlar

### Idempotency-Key
Submit (buton) anında **bir kez** `crypto.randomUUID()` üretilir ve API’ye parametre olarak verilir. Axios 401 retry aynı request config’i yeniden gönderdiği için key değişmez. API katmanı kendi başına yeni GUID üretmez.

Çift tıklama: `busy` disable + senkron `inFlightRef` guard.

### Token saklama (bilinçli karar)
Access + refresh token şu an **`localStorage`** içinde. XSS varsa çalınabilir; açık kaynak / demo için kabul edilebilir başlangıç. Production’da tercih: refresh’i **httpOnly Secure cookie**, access’i memory (veya kısa ömürlü) tutmak.

### 401 refresh single-flight
Eşzamanlı 401’ler tek bir paylaşılan `refreshPromise` üzerinden refresh eder; rotation altında yarışla logout olmayı önler.

### E2E (Playwright)

```bash
cd frontend
npm run test:e2e
```

Localde Testcontainers ile izole Postgres ayağa kalkar; CI’da `E2E_DATABASE_URL` + Postgres service kullanılır. Access token E2E’de 5 sn (`Jwt__AccessTokenSeconds`) — refresh senaryosu için.
