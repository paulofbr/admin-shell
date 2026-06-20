import {
  getAdminShellHostV1,
  type CreateUserRequest as GeneratedCreateUserRequest,
  type PagedResultOfUserDto,
  type UpdateUserRequest as GeneratedUpdateUserRequest,
  type UserDto,
} from '@/generated/api/adminshell'
import type { ExtensionField, PaginatedResponse, User } from '@admin-shell/ui/types'

const api = getAdminShellHostV1()

interface CreateUserRequest {
  email: string;
  username: string;
  password: string;
  displayName?: string | null;
  extensionFields?: ExtensionField[];
}

interface UpdateUserRequest {
  email?: string | null;
  username?: string | null;
  displayName?: string | null;
  isActive?: boolean;
  extensionFields?: ExtensionField[];
}

export async function getUsers(
  skip = 0,
  take = 10,
  filters?: { email?: string; username?: string; displayName?: string }
): Promise<PaginatedResponse<User>> {
  const response = await api.getApiV1Users({ skip, take, ...filters })
  return response.data as PagedResultOfUserDto as PaginatedResponse<User>
}

export async function getUserById(id: string): Promise<User> {
  const response = await api.getApiV1UsersId(id)
  return response.data as UserDto as User
}

export async function createUser(data: CreateUserRequest): Promise<User> {
  const response = await api.postApiV1Users({
    email: data.email,
    username: data.username,
    password: data.password,
    displayName: data.displayName ?? null,
    extensionFields: data.extensionFields,
  } as unknown as GeneratedCreateUserRequest)
  return response.data as UserDto as User
}

export async function updateUser(data: User): Promise<User> {
  const response = await api.putApiV1UsersId(data.id, {
    email: data.email ?? null,
    username: data.username ?? null,
    displayName: data.displayName ?? null,
    isActive: data.isActive ?? null,
    extensionFields: data.extensionFields,
  } as unknown as GeneratedUpdateUserRequest)
  return response.data as UserDto as User
}

export async function deleteUser(id: string): Promise<void> {
  await api.deleteApiV1UsersId(id)
}
