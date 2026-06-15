import { createApp, type App } from 'vue'
import ElementPlus from 'element-plus'
import OrderQuickCreateWidget from './components/OrderQuickCreateWidget.vue'
import OrderCreatePage from './views/OrderCreatePage.vue'
import OrderSummaryPage from './views/OrderSummaryPage.vue'
import { pluginServicesKey, type PluginServices } from './types'

const ROUTES = new Set(['/orders', '/orders/summary', '/orders/create'])

export default class OrderCreationPlugin {
  private container?: HTMLElement
  private services?: PluginServices
  private routeApp?: App
  private readonly unsubscribers: Array<() => void> = []
  private pendingRoute?: string
  private pendingRouteTimer?: number
  private routeRenderRetryCount = 0

  get id(): string {
    return 'order-creation'
  }

  get name(): string {
    return 'Order Creation Plugin'
  }

  get version(): string {
    return '1.0.0'
  }

  async initialize(container: HTMLElement, services: PluginServices): Promise<void> {
    this.container = container
    this.services = services

    services.components.register('OrderQuickCreateWidget', OrderQuickCreateWidget)
    services.components.register('OrderCreatePageComponent', OrderCreatePage)
    services.components.register('OrderSummaryPageComponent', OrderSummaryPage)

    this.unsubscribers.push(
      services.eventBus.subscribe<string>('route:changed', (path) => {
        this.renderRoute(path)
      }),
    )

    this.renderRoute(services.navigation.getCurrentPath())
    console.log(`${this.name} initialized with Vue SFC components`)
  }

  private renderRoute(path: string): void {
    if (!this.services) return

    const normalizedPath = path === '/' ? '/' : path
    if (!ROUTES.has(normalizedPath)) {
      this.disposeRouteApp()
      return
    }

    const componentName = normalizedPath === '/orders/create'
      ? 'OrderCreatePageComponent'
      : 'OrderSummaryPageComponent'

    const component = this.services.components.resolve(componentName)
    if (!component) {
      this.disposeRouteApp()
      return
    }

    const target = document.querySelector<HTMLElement>(
      `[data-plugin-route-container][data-plugin-route-component="${componentName}"]`,
    )

    if (!target) {
      this.disposeRouteApp()
      this.scheduleRouteRender(normalizedPath)
      return
    }

    this.pendingRoute = undefined
    this.routeRenderRetryCount = 0
    this.disposeRouteApp()
    target.innerHTML = ''

    this.routeApp = createApp(component)
    this.routeApp.provide(pluginServicesKey, this.services)
    this.routeApp.use(ElementPlus)
    this.routeApp.mount(target)
  }

  private scheduleRouteRender(path: string): void {
    if (this.pendingRoute === path || this.pendingRouteTimer) return

    const retryCount = this.routeRenderRetryCount
    this.pendingRoute = path
    this.pendingRouteTimer = window.setTimeout(() => {
      this.pendingRoute = undefined
      this.pendingRouteTimer = undefined
      this.routeRenderRetryCount += 1
      this.renderRoute(path)
    }, retryCount < 3 ? 0 : 50)
  }

  dispose(): void {
    this.unsubscribers.forEach((unsubscribe) => unsubscribe())
    this.unsubscribers.length = 0
    this.disposeRouteApp()
    if (this.pendingRouteTimer) {
      window.clearTimeout(this.pendingRouteTimer)
      this.pendingRouteTimer = undefined
    }
    this.pendingRoute = undefined
    this.routeRenderRetryCount = 0
    this.container = undefined
    this.services = undefined
  }

  private disposeRouteApp(): void {
    if (this.routeApp) {
      this.routeApp.unmount()
      this.routeApp = undefined
    }
  }
}
