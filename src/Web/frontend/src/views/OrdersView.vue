<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { listOrders } from '@/api/order'
import { ORDER_STATUS_TAG_TYPE, ORDER_STATUS_TEXT } from '@/types'
import type { Order, OrderStatusValue } from '@/types'

const router = useRouter()
const orders = ref<Order[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)

async function load() {
  loading.value = true
  try {
    const res = await listOrders(page.value, pageSize.value)
    orders.value = res.items
    total.value = res.totalCount
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <el-card>
    <template #header>
      <span style="font-size: 16px; font-weight: 600">订单列表</span>
    </template>

    <el-table v-loading="loading" :data="orders" stripe>
      <el-table-column prop="id" label="订单号" width="90" />
      <el-table-column prop="customerName" label="收货人" width="140" />
      <el-table-column label="金额" width="120">
        <template #default="{ row }">¥{{ Number(row.totalAmount).toFixed(2) }}</template>
      </el-table-column>
      <el-table-column label="状态" width="110">
        <template #default="{ row }">
          <el-tag :type="ORDER_STATUS_TAG_TYPE[row.status as OrderStatusValue]">
            {{ ORDER_STATUS_TEXT[row.status as OrderStatusValue] }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" label="下单时间" min-width="180">
        <template #default="{ row }">
          {{ new Date(row.createdAt).toLocaleString() }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="90">
        <template #default="{ row }">
          <el-button size="small" @click="router.push(`/orders/${row.id}`)">
            详情
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
  </el-card>
</template>
