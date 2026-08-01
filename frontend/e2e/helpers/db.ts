import fs from 'node:fs'
import path from 'node:path'
import pg from 'pg'

const runtimePath = path.resolve(process.cwd(), 'e2e/.runtime-env.json')

function databaseUrl() {
  if (process.env.E2E_DATABASE_URL) return process.env.E2E_DATABASE_URL
  if (!fs.existsSync(runtimePath)) {
    throw new Error(`Missing ${runtimePath}. Did globalSetup run?`)
  }
  const runtime = JSON.parse(fs.readFileSync(runtimePath, 'utf8')) as { databaseUrl: string }
  return runtime.databaseUrl
}

export async function withDb<T>(fn: (client: pg.Client) => Promise<T>): Promise<T> {
  const client = new pg.Client({ connectionString: toPgUri(databaseUrl()) })
  await client.connect()
  try {
    return await fn(client)
  } finally {
    await client.end()
  }
}

function toPgUri(connectionString: string): string {
  if (connectionString.startsWith('postgres://') || connectionString.startsWith('postgresql://')) {
    return connectionString
  }

  const map = Object.fromEntries(
    connectionString.split(';').filter(Boolean).map((part) => {
      const idx = part.indexOf('=')
      return [part.slice(0, idx).trim().toLowerCase(), part.slice(idx + 1).trim()]
    }),
  )

  const user = encodeURIComponent(map.username ?? map.user ?? 'postgres')
  const password = encodeURIComponent(map.password ?? '')
  const host = map.host ?? 'localhost'
  const port = map.port ?? '5432'
  const database = map.database ?? 'postgres'
  return `postgresql://${user}:${password}@${host}:${port}/${database}`
}

export async function createUserAccount(
  userId: string,
  openingBalance = 0,
): Promise<{ id: string; accountNumber: string }> {
  const accountId = crypto.randomUUID()
  const accountNumber = String(Math.floor(1_000_000_000 + Math.random() * 9_000_000_000))
  await withDb(async (client) => {
    const user = await client.query(`SELECT 1 FROM users WHERE "Id" = $1`, [userId])
    if (user.rowCount === 0) {
      throw new Error(`Cannot create account: user ${userId} not found in E2E database`)
    }

    await client.query(
      `INSERT INTO accounts ("Id", "UserId", "AccountNumber", "Balance", "Currency", "AccountType", "Status", "CreatedAt", "CategoryId", "IsTransferable")
       VALUES ($1::uuid, $2::uuid, $3, $4, 'TRY', 'User', 'Active', NOW(), 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1'::uuid, TRUE)`,
      [accountId, userId, accountNumber, openingBalance],
    )
  })
  return { id: accountId, accountNumber }
}

export async function createCardForAccount(
  accountId: string,
  label = 'E2E',
): Promise<string> {
  const cardId = crypto.randomUUID()
  const hash = `e2e-hash-${cardId.replace(/-/g, '')}`
  await withDb(async (client) => {
    await client.query(
      `INSERT INTO cards ("Id", "AccountId", "CardNumberHash", "LastFourDigits", "Status", "IssuedAt", "ExpiresAt", "CreatedAt", "Label")
       VALUES ($1::uuid, $2::uuid, $3, '4242', 'Active', NOW(), NOW() + INTERVAL '3 years', NOW(), $4)`,
      [cardId, accountId, hash, label],
    )
  })
  return cardId
}

export async function countCompletedTransfersBetween(
  sourceAccountId: string,
  destinationAccountId: string,
): Promise<number> {
  return withDb(async (client) => {
    const result = await client.query<{ count: string }>(
      `SELECT COUNT(*)::text AS count
       FROM transaction_records
       WHERE "Type" = 'Transfer'
         AND "Status" = 'Completed'
         AND "SourceAccountId" = $1::uuid
         AND "DestinationAccountId" = $2::uuid`,
      [sourceAccountId, destinationAccountId],
    )
    return Number(result.rows[0]?.count ?? 0)
  })
}
