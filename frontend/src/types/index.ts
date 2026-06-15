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
  frontendManifestResourceName?: string | null;
  hasEmbeddedFrontend?: boolean;
  frontendBaseUrl?: string | null;
}

export interface PluginInstallResult {
  pluginId: string
  pluginName: string
  version: string
  pluginDirectory: string
  activated: boolean
  messages: string[]
}

export interface PluginDependencyInfo {
  pluginId: string
  versionConstraint: string | null
  version?: string | null
  isOptional: boolean
  isResolved: boolean
  errorMessage: string | null
}

export interface PluginDependency {
  id: string
  version: string
}

export type PluginPermissions = Record<string, string[]>

export interface PluginManifest {
  schemaVersion: 1
  id: string
  name: string
  version: string
  description: string
  main?: string
  source?: 'embedded' | 'external'
  frontendBaseUrl?: string | null
  styles?: string[]
  dependencies?: PluginDependency[]
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

export interface ApplicationConfig {
  name?: string
  subtitle?: string
  icon?: string
  favicon?: string
  theme?: 'light' | 'dark'
}

export interface ApplicationService {
  configureApplication(config: ApplicationConfig): void
  resetApplication(): void
}

export interface PluginApiService {
  pluginId: string;
  getUrl(path: string): string;
  get(path: string, config?: any): Promise<any>;
  post(path: string, data?: unknown, config?: any): Promise<any>;
  put(path: string, data?: unknown, config?: any): Promise<any>;
  patch(path: string, data?: unknown, config?: any): Promise<any>;
  delete(path: string, config?: any): Promise<any>;
}

export interface PluginTableColumn {
  pluginId: string;
  id: string;
  label: string;
  width?: number;
  filterType?: 'select' | 'text' | 'none';
  filterOptions?: { label: string; value: string }[];
  render: (row: Record<string, unknown>) => string;
}

export interface PluginComponentRegistryService {
  register(name: string, component: unknown): void;
  resolve(name: string): unknown;
}

export interface PluginTableService {
  registerColumn(tableId: string, column: PluginTableColumn): void;
}

export interface PluginServices {
  http: any;
  api: PluginApiService;
  eventBus: EventBus;
  navigation: NavigationService;
  storage: StorageService;
  ui: UIService & {
    registerTableColumn(tableId: string, column: PluginTableColumn): void;
  };
  components: PluginComponentRegistryService;
  auth: AuthService;
  theme: ThemeService;
  app: ApplicationService;
}

export interface FrontendPlugin {
  id: string
  name: string
  version: string
  permissions?: PluginPermissions
  initialize(container: HTMLElement, services: PluginServices): Promise<void>
  dispose(): void
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