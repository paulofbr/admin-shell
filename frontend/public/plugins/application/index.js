export default class ApplicationPlugin {
  get id() {
    return 'application'
  }

  get name() {
    return 'Application Configuration Plugin'
  }

  get version() {
    return '1.0.0'
  }

  async initialize(_container, services) {
    services.app.configureApplication({
      name: 'Admin Shell',
      subtitle: 'Plugin-enabled management panel',
      icon: 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"%3E%3Crect width="32" height="32" rx="8" fill="%234f46e5"/%3E%3Cpath d="M8 10h16v12H8z" fill="none" stroke="%23fff" stroke-width="2"/%3E%3C/svg%3E',
      favicon: 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"%3E%3Crect width="32" height="32" rx="8" fill="%234f46e5"/%3E%3Cpath d="M8 10h16v12H8z" fill="none" stroke="%23fff" stroke-width="2"/%3E%3C/svg%3E',
    })
  }

  dispose() {}
}
