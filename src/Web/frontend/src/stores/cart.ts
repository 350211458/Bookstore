import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  addCartItem,
  clearCart,
  getCart,
  removeCartItem,
  updateCartItem,
} from '@/api/order'
import type { CartItem } from '@/types'

const SESSION_KEY = 'bookstore_session'

function ensureSessionId(): string {
  let id = localStorage.getItem(SESSION_KEY)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(SESSION_KEY, id)
  }
  return id
}

export const useCartStore = defineStore('cart', () => {
  const sessionId = ensureSessionId()
  const items = ref<CartItem[]>([])
  const totalAmount = ref(0)

  const itemCount = computed(() =>
    items.value.reduce((sum, item) => sum + item.quantity, 0),
  )

  async function load() {
    const res = await getCart(sessionId)
    items.value = res.items
    totalAmount.value = res.totalAmount
  }

  async function add(
    bookId: number,
    title: string,
    unitPrice: number,
    quantity = 1,
  ) {
    await addCartItem(sessionId, bookId, title, unitPrice, quantity)
    await load()
  }

  async function updateQuantity(bookId: number, quantity: number) {
    await updateCartItem(sessionId, bookId, quantity)
    await load()
  }

  async function remove(bookId: number) {
    await removeCartItem(sessionId, bookId)
    await load()
  }

  async function clear() {
    await clearCart(sessionId)
    await load()
  }

  return {
    sessionId,
    items,
    totalAmount,
    itemCount,
    load,
    add,
    updateQuantity,
    remove,
    clear,
  }
})
