<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { cancelOrder, getOrder, updateOrderStatus } from '@/api/order'
import { useAuthStore } from '@/stores/auth'
import {
  ORDER_STATUS_TAG_TYPE,
  ORDER_STATUS_TEXT,
  OrderStatus,
  type OrderResponse,
  type OrderStatusValue,
} from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const order = ref<OrderResponse | null>(null)
const loading = ref(false)
const targetStatus = ref<OrderStatusValue>(OrderStatus.Placed)

const allowedTransitions: Record<OrderStatusValue, OrderStatusValue[]> = {
  [OrderStatus.Placed]: [OrderStatus.Paid, OrderStatus.Cancelled],
  [OrderStatus.Paid]: [OrderStatus.Processing, OrderStatus.Cancelled],
  [OrderStatus.Processing]: [OrderStatus.Shipped],
  [OrderStatus.Shipped]: [OrderStatus.Completed],
  [OrderStatus.Completed]: [],
  [OrderStatus.Cancelled]: [],
}

const cancellableStatuses: OrderStatusValue[] = [
  OrderStatus.Placed,
  OrderStatus.Paid,
]

const canCancel = computed(
  () => !!order.value && cancellableStatuses.includes(order.value.status),
)
const nextTargets = computed<OrderStatusValue[]>(() =>
  order.value ? allowedTransitions[order.value.status] : [],
)

async function load() {
  loading.value = true
  try {
    const res = await getOrder(Number(route.params.id))
    order.value = res
    targetStatus.value = allowedTransitions[res.status][0] ?? res.status
  } finally {
    loading.value = false
  }
}

async function doCancel() {
  if (!order.value) return
  await cancelOrder(order.value.id)
  ElMessage.success('订单已取消')
  load()
}

async function doTransition() {
  if (!order.value) return
  await updateOrderStatus(order.value.id, targetStatus.value)
  ElMessage.success(`订单状态已更新为「${ORDER_STATUS_TEXT[targetStatus.value]}」`)
  load()
}

onMounted(load)
</script>

<template>
  <el-card v-loading="loading">
    <template #header>
      <div style="display: flex; align-items: center; gap: 12px">
        <el-button text @click="router.push('/orders')">← 返回订单列表</el-button>
        <span v-if="order" style="font-weight: 600">订单 #{{ order.id }}</span>
      </div>
    </template>

    <template v-if="order">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="收货人">{{ order.customerName }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="ORDER_STATUS_TAG_TYPE[order.status]">
            {{ ORDER_STATUS_TEXT[order.status] }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="下单时间">
          {{ new Date(order.createdAt).toLocaleString() }}
        </el-descriptions-item>
        <el-descriptions-item label="金额">
          <span style="color: #e6a23c; font-weight: 700">
            ¥{{ Number(order.totalAmount).toFixed(2) }}
          </span>
        </el-descriptions-item>
      </el-descriptions>

      <h3 style="margin-bottom: 8px">商品明细</h3>
      <el-table :data="order.items" stripe>
        <el-table-column prop="title" label="书名" min-width="200" />
        <el-table-column label="单价" width="100">
          <template #default="{ row }">¥{{ Number(row.unitPrice).toFixed(2) }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="90" />
        <el-table-column label="小计" width="120">
          <template #default="{ row }">¥{{ Number(row.lineTotal).toFixed(2) }}</template>
        </el-table-column>
      </el-table>

      <div style="margin-top: 20px; display: flex; align-items: center; gap: 12px">
        <el-button v-if="canCancel" type="danger" @click="doCancel">
          取消订单
        </el-button>
        <template v-if="auth.isAdmin && nextTargets.length > 0">
          <el-select v-model="targetStatus" style="width: 140px">
            <el-option
              v-for="s in nextTargets"
              :key="s"
              :label="ORDER_STATUS_TEXT[s]"
              :value="s"
            />
          </el-select>
          <el-button type="primary" @click="doTransition">更新状态</el-button>
        </template>
      </div>
    </template>
  </el-card>
</template>
