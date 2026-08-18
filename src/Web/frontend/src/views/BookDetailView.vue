<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getBook } from '@/api/catalog'
import { useCartStore } from '@/stores/cart'
import type { Book } from '@/types'

const route = useRoute()
const router = useRouter()
const cart = useCartStore()

const book = ref<Book | null>(null)
const quantity = ref(1)
const loading = ref(false)

async function addToCart() {
  if (!book.value) return
  await cart.add(book.value.id, book.value.title, book.value.price, quantity.value)
  ElMessage.success(`已将《${book.value.title}》加入购物车`)
}

onMounted(async () => {
  loading.value = true
  try {
    book.value = await getBook(Number(route.params.id))
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <el-card v-loading="loading">
    <template #header>
      <el-button text @click="router.push('/')">← 返回图书列表</el-button>
    </template>
    <template v-if="book">
      <h2 style="margin-top: 0">{{ book.title }}</h2>
      <el-descriptions :column="2" border>
        <el-descriptions-item label="作者">{{ book.author || '-' }}</el-descriptions-item>
        <el-descriptions-item label="ISBN">{{ book.isbn }}</el-descriptions-item>
        <el-descriptions-item label="分类">{{ book.category || '-' }}</el-descriptions-item>
        <el-descriptions-item label="库存">
          <el-tag :type="book.stockQuantity === 0 ? 'danger' : 'success'">
            {{ book.stockQuantity }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="价格">
          <span style="color: #e6a23c; font-size: 20px; font-weight: 600">
            ¥{{ Number(book.price).toFixed(2) }}
          </span>
        </el-descriptions-item>
      </el-descriptions>
      <div style="margin-top: 20px; display: flex; align-items: center; gap: 12px">
        <el-input-number v-model="quantity" :min="1" :max="Math.max(book.stockQuantity, 1)" />
        <el-button
          type="primary"
          size="large"
          :disabled="book.stockQuantity === 0"
          @click="addToCart"
        >
          加入购物车
        </el-button>
      </div>
    </template>
  </el-card>
</template>
