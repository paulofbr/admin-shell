import type { PaginatedResponse, User } from '@/types'
import client from './client'

export interface CreateUserRequest {
  email: string
  username: string
  password: string
  displayName?: string
}

export interface UpdateUserRequest {
  email?: string
  username?: string
  displayName?: string
  isActive?: boolean
}

export async function getUsers(
  skip = 0,
  take = 10,
): Promise<PaginatedResponse<User>> {
  const response = await client.get<PaginatedResponse<User>>('/api/users', {
    params: { skip, take },
  })
  return response.data
}

export async function getUserById(id: string): Promise<User> {
  const response = await client.get<User>(`/api/users/${id}`)
  return response.data
}

export async function createUser(data: CreateUserRequest): Promise<User> {
  const response = await client.post<User>('/api/users', data)
  return response.data
}

export async function updateUser(id: string, data: UpdateUserRequest): Promise<User> {
  const response = await client.put<User>(`/api/users/${id}`, data)
  return response.data
}

export async function deleteUser(id: string): Promise<void> {
  await client.delete(`/api/users/${id}`)
}