// Core types for the admin shell

export interface User {
  id: string;
  email: string;
  username: string;
  displayName: string | null;
  avatarUrl: string | null;
  isActive: boolean;
  createdAt: string;
  roles: Role[];
}

export interface Role {
  id: string;
  name: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  displayName?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  skip: number;
  take: number;
}

export interface Permission {
  id: string;
  code: string;
  resource: string;
  action: string;
  description?: string;
}

export interface PluginDescriptor {
  id: string;
  name: string;
  version: string;
  description: string;
  assemblyPath: string;
  status: 'discovered' | 'loading' | 'loaded' | 'initializing' | 'active' | 'failed' | 'disabled';
  dependencies: PluginDependencyInfo[];
  loadedAt: string;
  errorMessage: string | null;
}

export interface PluginDependencyInfo {
  pluginId: string;
  versionConstraint: string | null;
  isOptional: boolean;
  isResolved: boolean;
  errorMessage: string | null;
}

export interface PluginManifest {
  id: string;
  name: string;
  version: string;
  description: string;
  main: string;
  styles?: string[];
  dependencies?: Record<string, string>;
  permissions?: string[];
  uiContributions?: {
    menuItems?: MenuItem[];
    widgets?: WidgetDescriptor[];
  };
}

export interface MenuItem {
  id: string;
  label: string;
  icon?: string;
  path?: string;
  order: number;
  parentId?: string;
  permissions?: string[];
  children?: MenuItem[];
}

export interface WidgetDescriptor {
  id: string;
  title: string;
  zone: string;
  order: number;
  width: number;
  height: number;
  componentName?: string;
  settings?: Record<string, unknown>;
}

export interface HealthStatus {
  status: string;
  timestamp: string;
  version: string;
  plugins: PluginDescriptor[];
}

export interface NotificationItem {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
}

export interface EventBus {
  publish<T>(event: string, data: T): void;
  subscribe<T>(event: string, handler: (data: T) => void): () => void;
}

export interface NavigationService {
  navigate(path: string): void;
  getCurrentPath(): string;
}

export interface StorageService {
  get<T>(key: string): T | null;
  set<T>(key: string, value: T): void;
  remove(key: string): void;
}

export interface UIService {
  showNotification(message: string, type: 'success' | 'error' | 'info' | 'warning'): void;
  showModal(component: unknown): void;
  showLoading(show: boolean): void;
}

export interface AuthService {
  getToken(): string | null;
  isAuthenticated(): boolean;
  getUser(): User | null;
}

export interface ThemeService {
  getCurrentTheme(): 'light' | 'dark';
  toggleTheme(): void;
}

export interface PluginServices {
  http: import('axios').AxiosInstance;
  eventBus: EventBus;
  navigation: NavigationService;
  storage: StorageService;
  ui: UIService;
  auth: AuthService;
  theme: ThemeService;
}

export interface FrontendPlugin {
  id: string;
  name: string;
  version: string;
  initialize(container: HTMLElement, services: PluginServices): Promise<void>;
  dispose(): void;
}

// Plugin contributions to data tables
export interface TableColumnContrib {
  pluginId: string;
  id: string;
  label: string;
  width?: number;
  filterType?: 'select' | 'text' | 'none';
  filterOptions?: { label: string; value: string }[];
  render: (row: Record<string, unknown>) => string;
}

export interface TableFilterContrib {
  pluginId: string;
  id: string;
  label: string;
  type: 'select' | 'text';
  options?: { label: string; value: string }[];
  value: string | null;
}