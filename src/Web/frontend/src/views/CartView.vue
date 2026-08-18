<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { checkout } from '@/api/order'
import { useCartStore } from '@/stores/cart'
import type { CartItem } from '@/types'

const router = useRouter()
const cart = useCartStore()
const checkingOut = ref(false)

async function changeQty(item: CartItem, quantity: number) {
  await cart.updateQuantity(item.bookId, quantity)
}

async function remove(item: CartItem) {
  await cart.remove(item.bookId)
}

async function clearCart() {
  await cart.clear()
  ElMessage.success('购物车已清空')
}

async function doCheckout() {
  if (cart.items.length === 0) return
  const { value } = await ElMessageBox.prompt('请输入收货人姓名', '确认结账', {
    confirmButtonText: '确认下单',
    cancelButtonText: '取消',
    inputPattern: /\S+/,
    inputErrorMessage: '收货人姓名不能为空',
  })
  checkingOut.value = true
  try {
    const order = await checkout(cart.sessionId, value)
    ElMessage.success(`下单成功，订单号 #${order.id}`)
    router.push(`/orders/${order.id}`)
  } finally {
    checkingOut.value = false
  }
}

onMounted(() => cart.load())
</script>

<template>
  <el-card>
    <template #header>
      <span style="font-size: 16px; font-weight: 600">购物车</span>
    </template>

    <el-empty v-if="cart.items.length === 0" description="购物车是空的" />

    <template v-else>
      <el-table :data="cart.items" stripe>
        <el-table-column prop="title" label="书名" min-width="200" />
        <el-table-column label="单价" width="100">
          <template #default="{ row }">¥{{ Number(row.unitPrice).toFixed(2) }}</template>
        </el-table-column>
        <el-table-column label="数量" width="150">
          <template #default="{ row }">
            <el-input-number
              :model-value="row.quantity"
              :min="1"
              @change="(v: number | undefined) => changeQty(row, v ?? 1)"
            />
          </template>
        </el-table-column>
        <el-table-column label="小计" width="120">
          <template #default="{ row }">
            ¥{{ Number(row.unitPrice * row.quantity).toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="90">
          <template #default="{ row }">
            <el-button size="small" type="danger" text @click="remove(row)">
              移除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="footer">
        <div>
          <el-button text type="danger" @click="clearCart">清空购物车</el-button>
        </div>
        <div class="total">
          合计：
          <span style="color: #e6a23c; font-size: 22px; font-weight: 700">
            ¥{{ Number(cart.totalAmount).toFixed(2) }}
          </span>
          <el-button
            type="primary"
            size="large"
            :loading="checkingOut"
            style="margin-left: 16px"
            @click="doCheckout"
          >
            去结账
          </el-button>
        </div>
      </div>
    </template>
  </el-card>
</template>

<style scoped>
.footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 16px;
}
</style>
