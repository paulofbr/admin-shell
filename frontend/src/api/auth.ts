import type { User } from '@/types'
import client from './client'

export interface LoginPayload {
  email: string
  password: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user?: User
}

export interface RegisterPayload {
  email: string
  username: string
  password: string
  displayName?: string
}

export async function login(
  email: string,
  password: string,
): Promise<LoginResult> {
  const response = await client.post<LoginResult>('/api/auth/login', {
    email,
    password,
  })
  return response.data
}

export async function register(
  email: string,
  username: string,
  password: string,
  displayName?: string,
): Promise<LoginResult> {
  const response = await client.post<LoginResult>('/api/auth/register', {
    email,
    username,
    password,
    displayName,
  })
  return response.data
}

export async function refresh(
  accessToken: string,
  refreshToken: string,
): Promise<LoginResult> {
  const response = await client.post<LoginResult>('/api/auth/refresh', {
    accessToken,
    refreshToken,
  })
  return response.data
}

export async function logout(): Promise<void> {
  await client.post('/api/auth/logout')
}

export async function getMe(): Promise<User> {
  const response = await client.get<User>('/api/auth/me')
  return response.data
}