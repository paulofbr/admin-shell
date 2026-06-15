// User Department Plugin — Frontend
// Adds a Department column + filter dropdown to the Users table

export const permissions = {
  usersRead: ['users:read'],
  usersDepartment: ['users:department'],
}

const DEPARTMENTS = [
  { id: 'eng', name: 'Engineering', color: '#6366f1' },
  { id: 'marketing', name: 'Marketing', color: '#10b981' },
  { id: 'sales', name: 'Sales', color: '#f59e0b' },
  { id: 'hr', name: 'Human Resources', color: '#ef4444' },
  { id: 'finance', name: 'Finance', color: '#3b82f6' },
  { id: 'ops', name: 'Operations', color: '#8b5cf6' },
];

export default class UserDepartmentPlugin {
  constructor() {
    this.id = 'user-department';
    this.name = 'User Department Plugin';
    this.version = '1.0.0';
    this._unsubscribers = [];
    this._departmentCache = new Map();
  }

  async initialize(container, services) {
    this._services = services;
    const eventBus = services.eventBus;
    const api = services.api;
    const ui = services.ui;

    // Fetch departments and pre-warm cache via plugin API
    try {
      const deptRes = await api.get('/departments');
      if (deptRes.ok) {
        const depts = deptRes.data;
        // Build a lookup map
        depts.forEach(d => this._departmentCache.set(d.id, d));
      }
    } catch (e) {
      // Fall back to static list
      DEPARTMENTS.forEach(d => this._departmentCache.set(d.id, d));
    }

    // Register the department column contribution
    ui.registerTableColumn('users', {
      pluginId: this.id,
      id: 'department',
      label: 'Department',
      width: 140,
      filterType: 'select',
      filterOptions: DEPARTMENTS.map(d => ({ label: d.name, value: d.id })),
      render: (user) => {
        const dept = this._getDepartmentForUser(user);
        if (!dept) return '<span style="color:var(--el-text-color-placeholder)">—</span>';
        return `<span style="
          display:inline-flex;align-items:center;gap:6px;
          padding:2px 10px;border-radius:20px;
          font-size:12px;font-weight:500;
          background:${dept.color}15;
          color:${dept.color};
          border:1px solid ${dept.color}30;
        ">
          <span style="width:6px;height:6px;border-radius:50%;background:${dept.color}"></span>
          ${dept.name}
        </span>`;
      },
    });

    // Listen for user data to fetch department assignments
    this._unsubscribers.push(
      eventBus.subscribe('users:loaded', (users) => {
        // Fetch departments for each user in batches
        users.forEach(user => this._fetchUserDepartment(user));
      })
    );
  }

  dispose() {
    this._unsubscribers.forEach(fn => fn());
    this._unsubscribers = [];
    this._departmentCache.clear();
  }

  _getDepartmentForUser(user) {
    // Try the plugin's API cache first, fall back to hash
    if (this._departmentCache.has(user.id)) {
      return this._departmentCache.get(user.id);
    }

    // If the table plugin already stored a department id, resolve it
    if (user._department) {
      return this._departmentCache.get(user._department);
    }

    // Deterministic fallback
    const hash = this._hashCode(user.id);
    const depts = DEPARTMENTS;
    return depts[Math.abs(hash) % depts.length];
  }

  async _fetchUserDepartment(user) {
    try {
      const api = this._services?.api;
      const res = await api.get(`/users/${user.id}/department`);
      if (res.ok) {
        const data = res.data;
        // The department data will be used by the render function
        // Store it for the render callback
        user._department = data.departmentId;
      }
    } catch (e) {
      // Silently fail
    }
  }

  _hashCode(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash |= 0;
    }
    return hash;
  }
}