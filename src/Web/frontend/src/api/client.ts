import axios from 'axios'
import type { AxiosError, AxiosInstance } from 'axios'
import { ElMessage, ElNotification } from 'element-plus'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'

export const baseURL: string =
  import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:8080'

export const http: AxiosInstance = axios.create({
  baseURL,
  timeout: 15000,
})

http.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.token) {
    config.headers.set('Authorization', `Bearer ${auth.token}`)
  }
  return config
})

function detailOf(error: AxiosError): string {
  const data = error.response?.data as
    | { error_description?: string; message?: string; title?: string }
    | undefined
  return data?.error_description || data?.message || data?.title || ''
}

http.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status
    if (status === 401) {
      const auth = useAuthStore()
      if (auth.token) {
        auth.clear()
        router.push('/login')
      }
      ElMessage.error('登录已过期，请重新登录')
    } else if (status === 409) {
      ElNotification.warning({
        title: '库存不足',
        message: detailOf(error) || 'Insufficient stock for one or more items.',
      })
    } else if (status === 400) {
      ElMessage.error(detailOf(error) || '请求参数错误')
    } else if (status === 404) {
      ElMessage.error(detailOf(error) || '资源不存在')
    } else {
      ElMessage.error(detailOf(error) || '网络错误，请稍后重试')
    }
    return Promise.reject(error)
  },
)
