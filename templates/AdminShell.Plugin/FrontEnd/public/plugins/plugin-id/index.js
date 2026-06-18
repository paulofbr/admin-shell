// Plugin Title — Frontend

export const permissions = {
  itemsRead: ['plugin-id:items:read'],
};

export default class PluginNameFrontendPlugin {
  constructor() {
    this.id = 'plugin-id';
    this.name = 'Plugin Title';
    this.version = '1.0.0';
  }

  async initialize(container, services) {
    this._services = services;

    const api = services.api;
    const ui = services.ui;

    if (ui?.registerPage) {
      ui.registerPage({
        path: '/plugin-id/items',
        component: {
          template: '<div>Plugin Title items page</div>',
        },
        menu: {
          label: 'Plugin Title',
          icon: 'mdi-package-variant',
        },
      });
    }

    if (api) {
      try {
        const response = await api.get('/plugin-id/items');
        console.log('Plugin Title items', response.data);
      } catch (error) {
        console.warn('Plugin Title API is not ready yet', error);
      }
    }
  }

  dispose() {}
}
