import fs from 'node:fs'
import path from 'node:path'
import { execSync } from 'node:child_process'

const runtimePath = path.resolve(process.cwd(), 'e2e/.runtime-env.json')

type E2EGlobalState = {
  __E2E_PG_CONTAINER__?: { stop: () => Promise<void> }
  __E2E_API_PROCESS__?: { pid?: number; killed?: boolean; kill: (signal?: NodeJS.Signals) => boolean }
}

function killProcessTree(pid: number) {
  try {
    if (process.platform === 'win32') {
      execSync(`taskkill /pid ${pid} /T /F`, { stdio: 'ignore' })
    } else {
      try {
        process.kill(-pid, 'SIGTERM')
      } catch {
        process.kill(pid, 'SIGTERM')
      }
    }
  } catch {
    try {
      process.kill(pid, 'SIGKILL')
    } catch {
      // already gone
    }
  }
}

async function globalTeardown() {
  const state = globalThis as unknown as E2EGlobalState

  if (state.__E2E_API_PROCESS__?.pid) {
    console.log('[e2e] Stopping API process tree…')
    killProcessTree(state.__E2E_API_PROCESS__.pid)
  }

  if (state.__E2E_PG_CONTAINER__) {
    console.log('[e2e] Stopping Testcontainers PostgreSQL…')
    await state.__E2E_PG_CONTAINER__.stop()
  }

  if (fs.existsSync(runtimePath)) {
    fs.unlinkSync(runtimePath)
  }
}

export default globalTeardown
