<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const form = reactive({ username: 'alice', password: 'P@ssw0rd!' })
const loading = ref(false)

async function submit() {
  loading.value = true
  try {
    await auth.loginWithPassword(form.username, form.password)
    const redirect =
      typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.push(redirect)
  } catch {
    // interceptor already displayed the error
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login">
    <el-card class="login-card">
      <template #header>
        <span style="font-size: 18px; font-weight: 600">登录 Bookstore</span>
      </template>
      <el-form :model="form" label-width="60px" @submit.prevent="submit">
        <el-form-item label="用户名">
          <el-input v-model="form.username" placeholder="用户名" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            placeholder="密码"
            @keyup.enter="submit"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="submit">
            登录
          </el-button>
          <el-button @click="router.push('/')">返回</el-button>
        </el-form-item>
      </el-form>
      <el-alert
        type="info"
        :closable="false"
        title="演示账号：alice / P@ssw0rd!（顾客）；admin / Admin@123（管理员）"
      />
    </el-card>
  </div>
</template>

<style scoped>
.login {
  display: flex;
  justify-content: center;
  padding-top: 80px;
}

.login-card {
  width: 420px;
}
</style>
