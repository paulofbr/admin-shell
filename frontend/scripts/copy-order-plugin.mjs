import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const repoRoot = resolve(frontendRoot, '..')
const sourceDir = resolve(repoRoot, 'plugins/OrderCreationPlugin/FrontEnd/dist')
const targetDir = resolve(frontendRoot, 'public/plugins/order-creation')

mkdirSync(targetDir, { recursive: true })
copyFileSync(resolve(sourceDir, 'index.js'), resolve(targetDir, 'index.js'))
copyFileSync(resolve(sourceDir, 'styles.css'), resolve(targetDir, 'styles.css'))

const manifest = JSON.parse(
  readFileSync(resolve(repoRoot, 'plugins/OrderCreationPlugin/manifest.json'), 'utf8'),
)
manifest.styles = ['styles.css']
writeFileSync(resolve(targetDir, 'plugin.json'), `${JSON.stringify(manifest, null, 2)}\n`)
