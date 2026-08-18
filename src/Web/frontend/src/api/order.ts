import { http } from './client'
import type {
  CartItem,
  CartResponse,
  Order,
  OrderResponse,
  OrderStatusValue,
  PagedResult,
} from '@/types'

export async function getCart(sessionId: string): Promise<CartResponse> {
  const { data } = await http.get<CartResponse>('/order/api/cart', {
    params: { sessionId },
  })
  return data
}

export async function addCartItem(
  sessionId: string,
  bookId: number,
  title: string,
  unitPrice: number,
  quantity: number,
): Promise<CartItem> {
  const { data } = await http.post<CartItem>('/order/api/cart/items', {
    sessionId,
    bookId,
    title,
    unitPrice,
    quantity,
  })
  return data
}

export async function updateCartItem(
  sessionId: string,
  bookId: number,
  quantity: number,
): Promise<CartItem> {
  const { data } = await http.patch<CartItem>(
    `/order/api/cart/items/${bookId}`,
    { quantity },
    { params: { sessionId } },
  )
  return data
}

export async function removeCartItem(
  sessionId: string,
  bookId: number,
): Promise<void> {
  await http.delete(`/order/api/cart/items/${bookId}`, {
    params: { sessionId },
  })
}

export async function clearCart(sessionId: string): Promise<void> {
  await http.delete('/order/api/cart', { params: { sessionId } })
}

export async function checkout(
  sessionId: string,
  customerName: string,
): Promise<OrderResponse> {
  const { data } = await http.post<OrderResponse>(
    '/order/api/orders/checkout',
    { sessionId, customerName },
  )
  return data
}

export async function listOrders(
  page = 1,
  pageSize = 20,
): Promise<PagedResult<Order>> {
  const { data } = await http.get<PagedResult<Order>>('/order/api/orders', {
    params: { page, pageSize },
  })
  return data
}

export async function getOrder(id: number): Promise<OrderResponse> {
  const { data } = await http.get<OrderResponse>(`/order/api/orders/${id}`)
  return data
}

export async function updateOrderStatus(
  id: number,
  status: OrderStatusValue,
): Promise<Order> {
  const { data } = await http.post<Order>(`/order/api/orders/${id}/status`, {
    status,
  })
  return data
}

export async function cancelOrder(id: number): Promise<Order> {
  const { data } = await http.post<Order>(`/order/api/orders/${id}/cancel`)
  return data
}
