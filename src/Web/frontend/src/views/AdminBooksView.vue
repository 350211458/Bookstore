<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  adjustStock,
  createBook,
  deleteBook,
  listBooks,
  updateBook,
} from '@/api/catalog'
import type { Book, BookInput } from '@/types'

const books = ref<Book[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const saving = ref(false)
const form = reactive<BookInput>({
  title: '',
  author: '',
  isbn: '',
  price: 0,
  stockQuantity: 0,
  category: null,
})

const stockDialogVisible = ref(false)
const stockForm = reactive({ id: 0, title: '', delta: 0 })

async function load() {
  loading.value = true
  try {
    const res = await listBooks({ page: page.value, pageSize: pageSize.value })
    books.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    title: '',
    author: '',
    isbn: '',
    price: 0,
    stockQuantity: 0,
    category: null,
  })
  dialogVisible.value = true
}

function openEdit(book: Book) {
  editingId.value = book.id
  Object.assign(form, {
    title: book.title,
    author: book.author,
    isbn: book.isbn,
    price: book.price,
    stockQuantity: book.stockQuantity,
    category: book.category,
  })
  dialogVisible.value = true
}

async function save() {
  saving.value = true
  try {
    const input: BookInput = {
      ...form,
      category: form.category?.trim() ? form.category.trim() : null,
    }
    if (editingId.value === null) {
      await createBook(input)
      ElMessage.success('图书已创建')
    } else {
      await updateBook(editingId.value, input)
      ElMessage.success('图书已更新')
    }
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function remove(book: Book) {
  await ElMessageBox.confirm(`确定删除《${book.title}》？此操作不可恢复。`, '删除确认', {
    type: 'warning',
    confirmButtonText: '删除',
    cancelButtonText: '取消',
  })
  await deleteBook(book.id)
  ElMessage.success('已删除')
  load()
}

function openStock(book: Book) {
  stockForm.id = book.id
  stockForm.title = book.title
  stockForm.delta = 0
  stockDialogVisible.value = true
}

async function applyStock() {
  await adjustStock(stockForm.id, stockForm.delta)
  ElMessage.success('库存已调整')
  stockDialogVisible.value = false
  load()
}

onMounted(load)
</script>

<template>
  <el-card>
    <template #header>
      <div style="display: flex; justify-content: space-between; align-items: center">
        <span style="font-size: 16px; font-weight: 600">图书管理</span>
        <el-button type="primary" @click="openCreate">新建图书</el-button>
      </div>
    </template>

    <el-table v-loading="loading" :data="books" stripe>
      <el-table-column prop="id" label="ID" width="60" />
      <el-table-column prop="title" label="书名" min-width="170" />
      <el-table-column prop="author" label="作者" width="140" />
      <el-table-column prop="isbn" label="ISBN" width="140" />
      <el-table-column label="价格" width="90">
        <template #default="{ row }">¥{{ Number(row.price).toFixed(2) }}</template>
      </el-table-column>
      <el-table-column prop="stockQuantity" label="库存" width="80" />
      <el-table-column prop="category" label="分类" width="150" />
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="openEdit(row)">编辑</el-button>
          <el-button size="small" type="warning" @click="openStock(row)">
            调整库存
          </el-button>
          <el-button size="small" type="danger" @click="remove(row)">
            删除
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
      @size-change="() => { page = 1; load() }"
    />

    <el-dialog
      v-model="dialogVisible"
      :title="editingId === null ? '新建图书' : '编辑图书'"
      width="520px"
    >
      <el-form :model="form" label-width="100px">
        <el-form-item label="书名" required>
          <el-input v-model="form.title" />
        </el-form-item>
        <el-form-item label="作者">
          <el-input v-model="form.author" />
        </el-form-item>
        <el-form-item label="ISBN" required>
          <el-input v-model="form.isbn" />
        </el-form-item>
        <el-form-item label="价格">
          <el-input-number v-model="form.price" :min="0" :precision="2" style="width: 180px" />
        </el-form-item>
        <el-form-item label="库存">
          <el-input-number v-model="form.stockQuantity" :min="0" style="width: 180px" />
        </el-form-item>
        <el-form-item label="分类">
          <el-input v-model="form.category" placeholder="如 Software Engineering" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="stockDialogVisible" title="调整库存" width="400px">
      <p style="margin-top: 0">
        《{{ stockForm.title }}》 — 正数增加库存，负数扣减库存（扣减后不可低于 0）。
      </p>
      <el-input-number v-model="stockForm.delta" :min="-9999" :max="9999" style="width: 180px" />
      <template #footer>
        <el-button @click="stockDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="applyStock">确认</el-button>
      </template>
    </el-dialog>
  </el-card>
</template>
