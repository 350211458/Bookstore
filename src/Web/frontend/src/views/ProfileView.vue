<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { fetchUserInfo } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  if (!auth.user) {
    try {
      auth.user = await fetchUserInfo()
    } catch {
      // interceptor handles 401
    }
  }
})
</script>

<template>
  <el-card>
    <template #header>
      <span style="font-size: 16px; font-weight: 600">个人中心</span>
    </template>
    <el-descriptions :column="1" border>
      <el-descriptions-item label="用户名">
        {{ auth.user?.name || auth.username || '-' }}
      </el-descriptions-item>
      <el-descriptions-item label="邮箱">
        {{ auth.user?.email || '-' }}
      </el-descriptions-item>
      <el-descriptions-item label="角色">
        <el-tag :type="auth.isAdmin ? 'danger' : 'success'">
          {{ auth.isAdmin ? '管理员' : '顾客' }}
        </el-tag>
      </el-descriptions-item>
    </el-descriptions>
    <el-button
      type="danger"
      style="margin-top: 16px"
      @click="auth.clear(); router.push('/login')"
    >
      退出登录
    </el-button>
  </el-card>
</template>
