#!/usr/bin/env node
import { spawnSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { dirname, join, relative, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const frontendRoot = resolve(__dirname, '..')
const repoRoot = resolve(frontendRoot, '..')
const generatedRoot = join(frontendRoot, 'src/generated')
const openApiRoot = join(generatedRoot, 'openapi')
const apiOutputRoot = join(generatedRoot, 'api')
const pluginOutputRoot = join(generatedRoot, 'plugins')

await main()

async function main() {
  const args = parseArgs(process.argv.slice(2))
  const openApiUrl = args.openapiUrl ?? process.env.ADMINSHELL_OPENAPI_URL
  const openApiFile = args.openapiFile ? resolve(frontendRoot, args.openapiFile) : null
  const apiUrl = args.apiUrl ?? process.env.ADMINSHELL_API_URL ?? 'http://127.0.0.1:5000'

  mkdirSync(openApiRoot, { recursive: true })
  rmSync(join(openApiRoot, 'plugins'), { recursive: true, force: true })
  mkdirSync(join(openApiRoot, 'plugins'), { recursive: true })
  mkdirSync(apiOutputRoot, { recursive: true })
  rmSync(pluginOutputRoot, { recursive: true, force: true })
  mkdirSync(pluginOutputRoot, { recursive: true })

  const mainSpecPath = join(openApiRoot, 'adminshell.json')

  if (openApiFile) {
    writeFileSync(mainSpecPath, readFileSync(openApiFile, 'utf8'))
  } else if (openApiUrl) {
    writeFileSync(mainSpecPath, await fetchText(openApiUrl))
  } else {
    writeFileSync(mainSpecPath, await fetchText(`${apiUrl.replace(/\/$/, '')}/openapi/v1.json`))
  }

  const mainSpec = JSON.parse(readFileSync(mainSpecPath, 'utf8'))
  const pluginSpecs = splitPluginSpecs(mainSpec)

  for (const [pluginId, spec] of Object.entries(pluginSpecs)) {
    writeFileSync(join(openApiRoot, 'plugins', `${pluginId}.json`), `${JSON.stringify(spec, null, 2)}\n`)
  }

  writeFileSync(
    join(frontendRoot, 'orval.generated.config.mjs'),
    buildOrvalConfig(pluginSpecs, mainSpecPath),
  )

  const result = spawnSync('npx', ['orval', '--config', 'orval.generated.config.mjs'], {
    cwd: frontendRoot,
    stdio: 'inherit',
    shell: process.platform === 'win32',
  })

  if (result.status !== 0) {
    process.exit(result.status ?? 1)
  }

  writeIndex(pluginSpecs)
  writeReadme()
  await generatePluginFrontendClients(pluginSpecs, frontendRoot, repoRoot, openApiRoot)

  console.log(`Generated TypeScript API client from ${mainSpecPath}`)
}

function parseArgs(argv) {
  const args = {}
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i]
    if (!arg.startsWith('--')) continue
    const key = arg.slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase())
    const next = argv[i + 1]
    if (next && !next.startsWith('--')) {
      args[key] = next
      i += 1
    } else {
      args[key] = true
    }
  }
  return args
}

async function fetchText(url) {
  const response = await globalThis.fetch(url)
  if (!response.ok) {
    throw new Error(`Failed to fetch OpenAPI from ${url}: ${response.status} ${response.statusText}`)
  }
  return response.text()
}

function splitPluginSpecs(spec) {
  const pluginSpecs = new Map()

  for (const [path, operations] of Object.entries(spec.paths ?? {})) {
    if (!path.startsWith('/api/plugins/')) continue
    if (path.startsWith('/api/plugins/install')) continue
    if (path.startsWith('/api/plugins/{pluginId}/frontend')) continue

    const parts = path.split('/')
    const pluginId = parts[3]
    if (!pluginId || pluginId.startsWith('{')) continue

    if (!pluginSpecs.has(pluginId)) {
      pluginSpecs.set(pluginId, {
        openapi: '3.1.0',
        info: {
          title: `${pluginId} plugin API`,
          version: spec.info?.version ?? '1.0.0',
        },
        servers: spec.servers ?? [],
        paths: {},
        components: spec.components,
      })
    }

    pluginSpecs.get(pluginId).paths[path] = operations
  }

  return Object.fromEntries([...pluginSpecs.entries()].sort(([a], [b]) => a.localeCompare(b)))
}

function buildOrvalConfig(pluginSpecs, mainSpecPath) {
  const mutator = {
    path: './src/api/orval-client.ts',
    name: 'httpClient',
  }

  const outputOptions = (target, { clean = true } = {}) => ({
    target,
    client: 'axios',
    baseUrl: '',
    clean,
    override: {
      mutator,
      fetch: {
        includeHttpResponseReturnType: false,
      },
    },
  })

  const projects = {
    adminShell: {
      input: mainSpecPath,
      output: outputOptions('src/generated/api/adminshell.ts'),
    },
  }

  for (const pluginId of Object.keys(pluginSpecs)) {
    projects[`${pluginId}Plugin`] = {
      input: join(openApiRoot, 'plugins', `${pluginId}.json`),
      output: outputOptions(`src/generated/plugins/${pluginId}.ts`, { clean: false }),
    }
  }

  return `export default ${JSON.stringify(projects, null, 2)}\n`
}

function writeIndex(pluginSpecs) {
  const exports = [
    "export * from './api/adminshell'",
    ...Object.keys(pluginSpecs).map((pluginId) => `export * as ${toPascalCase(pluginId)}Plugin from './plugins/${pluginId}'`),
  ].join('\n')

  writeFileSync(
    join(generatedRoot, 'index.ts'),
    `${exports}\n`,
  )
}

function writeReadme() {
  writeFileSync(
    join(generatedRoot, 'README.md'),
    [
      '# Generated API client',
      '',
      'This folder is generated from the AdminShell OpenAPI document.',
      '- `openapi/adminshell.json` is the full backend contract.',
      '- `openapi/plugins/*.json` contains one filtered contract per plugin API.',
      '- `api/adminshell.ts` is the generated backend client.',
      '- `plugins/*.ts` are generated plugin clients for the main AdminShell frontend.',
      '- Plugins with both `Backend` and `FrontEnd` also get their own generated client under `plugins/<PluginName>/FrontEnd/src/generated/api.ts`.',
      '- `index.ts` re-exports the generated clients for application code.',
      '',
      'Regenerate with:',
      '',
      '```bash',
      'npm run generate:api-client',
      '```',
      '',
      'Do not edit generated files manually.',
    ].join('\n'),
  )
}

async function generatePluginFrontendClients(pluginSpecs, frontendRoot, repoRoot, openApiRoot) {
  const orvalBin = process.platform === 'win32'
    ? join(frontendRoot, 'node_modules/.bin/orval.cmd')
    : join(frontendRoot, 'node_modules/.bin/orval')

  if (!existsSync(orvalBin)) {
    console.warn(`Skipping plugin frontend API generation because Orval was not found at ${orvalBin}.`)
    return
  }

  for (const [pluginId, spec] of Object.entries(pluginSpecs)) {
    const pluginFrontendRoot = join(repoRoot, 'plugins', `${toPascalCase(pluginId)}Plugin`, 'FrontEnd')
    if (!existsSync(pluginFrontendRoot) || !existsSync(join(pluginFrontendRoot, 'package.json'))) {
      continue
    }

    const sharedHttpClientPath = relative(pluginFrontendRoot, join(frontendRoot, 'packages/admin-shell-http-client/src/index.ts'))

    const pluginSpecPath = join(openApiRoot, 'plugins', `${pluginId}.json`)
    const pluginGeneratedRoot = join(pluginFrontendRoot, 'src/generated')
    const pluginGeneratedApi = join(pluginGeneratedRoot, 'api.ts')
    const pluginOrvalConfig = join(pluginFrontendRoot, 'orval.generated.config.mjs')

    mkdirSync(dirname(pluginGeneratedApi), { recursive: true })

    writeFileSync(
      pluginOrvalConfig,
      buildPluginFrontendOrvalConfig({
        pluginId,
        input: pluginSpecPath,
        output: pluginGeneratedApi,
        mutator: sharedHttpClientPath,
      }),
    )

    const result = spawnSync(orvalBin, ['--config', pluginOrvalConfig], {
      cwd: pluginFrontendRoot,
      stdio: 'inherit',
      shell: process.platform === 'win32',
    })

    if (result.status !== 0) {
      throw new Error(`Failed to generate TypeScript API client for plugin ${pluginId}`)
    }

    console.log(`Generated TypeScript API client for ${pluginId} plugin frontend`)
  }
}



function buildPluginFrontendOrvalConfig({ pluginId, input, output, mutator }) {
  const project = {
    [`${toPascalCase(pluginId)}PluginFrontend`]: {
      input,
      output: {
        target: output,
        client: 'axios',
        baseUrl: '',
        clean: true,
        override: {
          mutator: {
            path: mutator,
            name: 'httpClient',
          },
          fetch: {
            includeHttpResponseReturnType: false,
          },
        },
      },
    },
  }

  return `export default ${JSON.stringify(project, null, 2)}\n`
}

function toPascalCase(value) {
  return value
    .split(/[^a-zA-Z0-9]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('')
}
