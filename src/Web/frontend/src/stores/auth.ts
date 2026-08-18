import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchUserInfo, login } from '@/api/auth'
import { decodeJwt } from '@/utils/jwt'
import type { UserInfo } from '@/types'

const TOKEN_KEY = 'bookstore_token'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const user = ref<UserInfo | null>(null)
  const username = ref<string>('')

  const isAdmin = computed(() => {
    if (user.value?.role === 'Admin') return true
    if (!token.value) return false
    return decodeJwt(token.value)?.role === 'Admin'
  })

  async function loginWithPassword(name: string, password: string) {
    const res = await login(name, password)
    token.value = res.access_token
    username.value = name
    localStorage.setItem(TOKEN_KEY, res.access_token)
    try {
      user.value = await fetchUserInfo()
    } catch {
      // interceptor already surfaces the error; userinfo is best-effort here
      user.value = null
    }
  }

  function clear() {
    token.value = null
    user.value = null
    username.value = ''
    localStorage.removeItem(TOKEN_KEY)
  }

  return { token, user, username, isAdmin, loginWithPassword, clear }
})
