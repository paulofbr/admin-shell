import {
  getAdminShellHostV1,
  type AuditLogDto as GeneratedAuditLogDto,
} from '@/generated/api/adminshell'

export interface AuditEntry {
  id?: string
  action: string
  entityType: string
  entityId: string | null
  performedBy: string
  details: string | null
  ipAddress: string | null
  timestamp: string
}

export interface AuditLogResponse {
  data: AuditEntry[]
  total: number
}

const api = getAdminShellHostV1()

function normalizeEntry(entry: GeneratedAuditLogDto): AuditEntry {
  return {
    id: entry.id,
    action: entry.action,
    entityType: entry.entityType,
    entityId: entry.entityId,
    performedBy: entry.performedBy ?? '',
    details: entry.details,
    ipAddress: entry.ipAddress,
    timestamp: entry.timestamp,
  }
}

export async function getAuditLog(params: { skip: number; take: number; action?: string }): Promise<AuditLogResponse> {
  const response = params.action
    ? await api.getApiAuditLogActionAction(params.action, { skip: params.skip, take: params.take })
    : await api.getApiAuditLog({ skip: params.skip, take: params.take })

  return {
    data: response.data.data.map(normalizeEntry),
    total: Number(response.data.total ?? 0),
  }
}
