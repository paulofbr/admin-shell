// Reporting Plugin — Frontend
// Adds a Reports page and event bus integration

export const permissions = {
  reportsRead: ['reports:read'],
  reportsCreate: ['reports:create'],
}

export default class ReportingPlugin {
  constructor() {
    this.id = 'reporting';
    this.name = 'Reporting Plugin';
    this.version = '1.0.0';
    this._unsubscribers = [];
  }

  async initialize(container, services) {
    const eventBus = services.eventBus;

    // Listen for route changes to inject reports content
    this._unsubscribers.push(
      eventBus.subscribe('route:changed', (route) => {
        if (route === '/reports') {
          this._renderReportsPage(container);
        }
      })
    );

    console.log('Reporting Plugin initialized');
  }

  async _renderReportsPage(container) {
    try {
      const res = await fetch('/api/plugins/reporting/reports');
      const reports = res.ok ? await res.json() : [];

      container.innerHTML = `
        <div style="padding: 24px">
          <h3 style="margin: 0 0 16px; font-size: 18px; font-weight: 600">Reports</h3>
          ${reports.length === 0
            ? '<p style="color:var(--el-text-color-secondary)">No reports available.</p>'
            : `<ul style="list-style:none;padding:0;margin:0">
                ${reports.map(r => `<li style="padding:8px 12px;border:1px solid var(--el-border-color);border-radius:8px;margin-bottom:8px">${r}</li>`).join('')}
              </ul>`
          }
        </div>
      `;
    } catch (e) {
      container.innerHTML = `<p style="color:var(--el-color-danger)">Failed to load reports.</p>`;
    }
  }

  dispose() {
    this._unsubscribers.forEach(fn => fn());
    this._unsubscribers = [];
  }
}