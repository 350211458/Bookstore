<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { fetchUserInfo } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()

onMounted(async () => {
  cart.load().catch(() => undefined)
  if (auth.token && !auth.user) {
    try {
      auth.user = await fetchUserInfo()
    } catch {
      /* interceptor handles 401 */
    }
  }
})
</script>

<template>
  <el-container class="layout">
    <el-header class="header">
      <div class="brand" @click="router.push('/')">📚 Bookstore</div>
      <nav>
        <el-menu
          mode="horizontal"
          :ellipsis="false"
          :default-active="route.path"
          router
        >
          <el-menu-item index="/">图书</el-menu-item>
          <el-menu-item index="/cart">
            购物车
            <el-badge
              :value="cart.itemCount"
              :hidden="cart.itemCount === 0"
              class="cart-badge"
            />
          </el-menu-item>
          <el-menu-item index="/orders">订单</el-menu-item>
          <el-menu-item v-if="auth.isAdmin" index="/admin/books">管理</el-menu-item>
        </el-menu>
      </nav>
      <div class="user">
        <template v-if="auth.token">
          <el-link type="primary" @click="router.push('/profile')">
            {{ auth.user?.name || auth.username }}
          </el-link>
          <el-button
            text
            type="danger"
            @click="auth.clear(); router.push('/login')"
          >
            退出
          </el-button>
        </template>
        <el-button v-else type="primary" @click="router.push('/login')">
          登录
        </el-button>
      </div>
    </el-header>
    <el-main>
      <router-view />
    </el-main>
  </el-container>
</template>

<style scoped>
.layout {
  min-height: 100vh;
}

.header {
  display: flex;
  align-items: center;
  gap: 24px;
  border-bottom: 1px solid var(--el-border-color-light);
  background: #fff;
}

.brand {
  font-size: 20px;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
}

nav {
  flex: 1;
}

.user {
  display: flex;
  align-items: center;
  gap: 8px;
}

.cart-badge {
  margin-left: 6px;
}
</style>
