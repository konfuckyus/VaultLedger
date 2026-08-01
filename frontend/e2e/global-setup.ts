import { spawn, type ChildProcess } from 'node:child_process'
import fs from 'node:fs'
import path from 'node:path'
import { PostgreSqlContainer } from '@testcontainers/postgresql'

const apiProject = path.resolve(process.cwd(), '../src/VaultLedger.API')
const runtimePath = path.resolve(process.cwd(), 'e2e/.runtime-env.json')
const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5154'

export type E2ERuntimeEnv = {
  databaseUrl: string
  usedTestcontainers: boolean
}

type E2EGlobalState = {
  __E2E_PG_CONTAINER__?: { stop: () => Promise<void> }
  __E2E_API_PROCESS__?: ChildProcess
}

function toNpgsql(host: string, port: number, database: string, user: string, password: string) {
  return `Host=${host};Port=${port};Database=${database};Username=${user};Password=${password};Include Error Detail=true`
}

async function waitForHealth(url: string, timeoutMs: number) {
  const started = Date.now()
  let lastError = ''
  while (Date.now() - started < timeoutMs) {
    try {
      const res = await fetch(url)
      if (res.ok) return
      lastError = `status ${res.status}`
    } catch (err) {
      lastError = err instanceof Error ? err.message : String(err)
    }
    await new Promise((r) => setTimeout(r, 500))
  }
  throw new Error(`API health check failed for ${url}: ${lastError}`)
}

async function globalSetup() {
  const state = globalThis as unknown as E2EGlobalState
  let databaseUrl = process.env.E2E_DATABASE_URL
  let usedTestcontainers = false

  if (!databaseUrl) {
    console.log('[e2e] Starting Testcontainers PostgreSQL…')
    const container = await new PostgreSqlContainer('postgres:16-alpine')
      .withDatabase('vaultledger_e2e')
      .withUsername('postgres')
      .withPassword('postgres')
      .start()

    databaseUrl = toNpgsql(
      container.getHost(),
      container.getMappedPort(5432),
      container.getDatabase(),
      container.getUsername(),
      container.getPassword(),
    )
    usedTestcontainers = true
    state.__E2E_PG_CONTAINER__ = container
  } else {
    console.log('[e2e] Using E2E_DATABASE_URL from environment (CI service).')
  }

  const runtime: E2ERuntimeEnv = { databaseUrl, usedTestcontainers }
  fs.mkdirSync(path.dirname(runtimePath), { recursive: true })
  fs.writeFileSync(runtimePath, JSON.stringify(runtime, null, 2), 'utf8')
  // Also export for this process; workers read the file via cwd.
  process.env.E2E_DATABASE_URL = databaseUrl
  console.log('[e2e] Runtime env written to', runtimePath)

  console.log('[e2e] Starting API…')
  const child = spawn(
    'dotnet',
    ['run', '--project', apiProject, '--no-launch-profile'],
    {
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: apiUrl,
        ConnectionStrings__DefaultConnection: databaseUrl,
        Jwt__Issuer: 'VaultLedger',
        Jwt__Audience: 'VaultLedger',
        Jwt__Secret: 'VaultLedger_E2E_Secret_Key_ChangeMe_32chars!!',
        Jwt__AccessTokenMinutes: '15',
        // Long enough for multi-step flows; refresh test forces expiry manually.
        Jwt__AccessTokenSeconds: '120',
        Jwt__RefreshTokenDays: '7',
        MigrateOnStartup: 'true',
        DisableHttpsRedirection: 'true',
        Cors__AllowedOrigins__0: 'http://localhost:5173',
        RateLimiting__AuthPermitLimit: '1000',
        RateLimiting__AuthWindowSeconds: '60',
        RateLimiting__TransactionsPermitLimit: '1000',
        RateLimiting__TransactionsWindowSeconds: '60',
      },
      stdio: 'inherit',
      shell: false,
      windowsHide: true,
    },
  )
  state.__E2E_API_PROCESS__ = child

  child.on('exit', (code, signal) => {
    if (code && code !== 0) {
      console.error(`[e2e] API exited early code=${code} signal=${signal}`)
    }
  })

  await waitForHealth(`${apiUrl}/health`, 180_000)
  console.log('[e2e] API is healthy at', apiUrl)
}

export default globalSetup
