<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { listBooks } from '@/api/catalog'
import { useCartStore } from '@/stores/cart'
import type { Book } from '@/types'

const router = useRouter()
const cart = useCartStore()

const filters = reactive({
  keyword: '',
  category: '',
  minPrice: undefined as number | undefined,
  maxPrice: undefined as number | undefined,
})
const page = ref(1)
const pageSize = ref(10)
const books = ref<Book[]>([])
const total = ref(0)
const loading = ref(false)
const addQty = ref<Record<number, number>>({})

async function load() {
  loading.value = true
  try {
    const res = await listBooks({
      keyword: filters.keyword || undefined,
      category: filters.category || undefined,
      minPrice: filters.minPrice,
      maxPrice: filters.maxPrice,
      page: page.value,
      pageSize: pageSize.value,
    })
    books.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

function search() {
  page.value = 1
  load()
}

async function addToCart(book: Book) {
  await cart.add(book.id, book.title, book.price, addQty.value[book.id] ?? 1)
  ElMessage.success(`已将《${book.title}》加入购物车`)
}

onMounted(load)
</script>

<template>
  <el-card>
    <el-form inline>
      <el-form-item label="关键字">
        <el-input
          v-model="filters.keyword"
          placeholder="书名 / 作者 / ISBN"
          clearable
          style="width: 200px"
          @keyup.enter="search"
        />
      </el-form-item>
      <el-form-item label="分类">
        <el-input
          v-model="filters.category"
          placeholder="如 Software Engineering"
          clearable
          style="width: 190px"
          @keyup.enter="search"
        />
      </el-form-item>
      <el-form-item label="价格区间">
        <el-input-number
          v-model="filters.minPrice"
          :min="0"
          :controls="false"
          placeholder="最低"
          style="width: 100px"
        />
        <span style="margin: 0 8px">-</span>
        <el-input-number
          v-model="filters.maxPrice"
          :min="0"
          :controls="false"
          placeholder="最高"
          style="width: 100px"
        />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="search">搜索</el-button>
      </el-form-item>
    </el-form>

    <el-table v-loading="loading" :data="books" stripe>
      <el-table-column prop="id" label="ID" width="60" />
      <el-table-column prop="title" label="书名" min-width="170" />
      <el-table-column prop="author" label="作者" width="140" />
      <el-table-column prop="isbn" label="ISBN" width="150" />
      <el-table-column label="价格" width="90">
        <template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template>
      </el-table-column>
      <el-table-column prop="stockQuantity" label="库存" width="80" />
      <el-table-column prop="category" label="分类" width="170" />
      <el-table-column label="操作" width="230" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="router.push(`/books/${row.id}`)">
            详情
          </el-button>
          <el-input-number
            v-model="addQty[row.id]"
            :min="1"
            :max="Math.max(row.stockQuantity, 1)"
            size="small"
            style="width: 90px"
          />
          <el-button
            size="small"
            type="primary"
            :disabled="row.stockQuantity === 0"
            @click="addToCart(row)"
          >
            加购
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="page"
      v-model:page-size="pageSize"
      :total="total"
      :page-sizes="[10, 20, 50]"
      layout="total, sizes, prev, pager, next"
      style="margin-top: 16px; justify-content: flex-end"
      @current-change="load"
      @size-change="search"
    />
  </el-card>
</template>
