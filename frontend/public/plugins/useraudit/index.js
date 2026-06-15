// User Audit Plugin — Frontend
// Adds audit log page

export const permissions = {
  auditRead: ['audit:read'],
}

export default class UserAuditPlugin {
  constructor() {
    this.id = 'useraudit';
    this.name = 'User Audit Plugin';
    this.version = '1.0.0';
    this._unsubscribers = [];
  }

  async initialize(container, services) {
    this._services = services;
    const eventBus = services.eventBus;

    this._unsubscribers.push(
      eventBus.subscribe('route:changed', (route) => {
        if (route === '/audit') {
          this._renderAuditPage(container);
        }
      })
    );

    console.log('User Audit Plugin initialized');
  }

  async _renderAuditPage(container) {
    try {
      const api = this._services?.api;
      const res = await api.get('/audit');
      const entries = res.ok ? res.data : [];

      container.innerHTML = `
        <div style="padding: 24px">
          <h3 style="margin: 0 0 16px; font-size: 18px; font-weight: 600">Audit Log</h3>
          ${entries.length === 0
            ? '<p style="color:var(--el-text-color-secondary)">No audit entries.</p>'
            : `<table style="width:100%;border-collapse:collapse">
                <thead>
                  <tr style="text-align:left;border-bottom:1px solid var(--el-border-color)">
                    <th style="padding:8px 12px">Action</th>
                    <th style="padding:8px 12px">User</th>
                    <th style="padding:8px 12px">Timestamp</th>
                  </tr>
                </thead>
                <tbody>
                  ${entries.map(e => `
                    <tr style="border-bottom:1px solid var(--el-border-color-light)">
                      <td style="padding:8px 12px">${e.action || e.description || '-'}</td>
                      <td style="padding:8px 12px">${e.user || '-'}</td>
                      <td style="padding:8px 12px">${e.timestamp ? new Date(e.timestamp).toLocaleString() : '-'}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>`
          }
        </div>
      `;
    } catch (e) {
      container.innerHTML = `<p style="color:var(--el-color-danger)">Failed to load audit log.</p>`;
    }
  }

  dispose() {
    this._unsubscribers.forEach(fn => fn());
    this._unsubscribers = [];
  }
}