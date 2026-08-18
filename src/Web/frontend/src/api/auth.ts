import { http } from './client'
import type { TokenResponse, UserInfo } from '@/types'

/**
 * Password grant token request (Spec 07 Req 4). `bookstore-app` is a public client
 * (no secret); the identity server returns an OpenIddict JSON token response.
 */
export async function login(
  username: string,
  password: string,
): Promise<TokenResponse> {
  const form = new URLSearchParams()
  form.set('grant_type', 'password')
  form.set('client_id', 'bookstore-app')
  form.set('username', username)
  form.set('password', password)
  const { data } = await http.post<TokenResponse>(
    '/identity/connect/token',
    form,
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
  )
  return data
}

export async function fetchUserInfo(): Promise<UserInfo> {
  const { data } = await http.get<UserInfo>('/identity/connect/userinfo')
  return data
}
