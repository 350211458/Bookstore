// Authoritative DTO contracts (Spec 07 Req 6) — camelCase JSON as returned by the
// Identity / Catalog / Order services through the YARP gateway (Spec 03 / Spec 04).

export interface Book {
  id: number
  title: string
  author: string
  isbn: string
  price: number
  stockQuantity: number
  category: string | null
  createdAt: string
  updatedAt: string
  isDeleted: boolean
}

/** Create / update request bodies for catalog books. */
export interface BookInput {
  title: string
  author: string
  isbn: string
  price: number
  stockQuantity: number
  category: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CartItem {
  id: number
  sessionId: string
  bookId: number
  title: string
  unitPrice: number
  quantity: number
}

export interface CartResponse {
  items: CartItem[]
  totalAmount: number
}

// Order status is serialized by Order.Api as its integer enum value (Placed=0 ..
// Cancelled=5) — the API does not register JsonStringEnumConverter. Verified against
// the running stack on 2026-08-18.
export const OrderStatus = {
  Placed: 0,
  Paid: 1,
  Processing: 2,
  Shipped: 3,
  Completed: 4,
  Cancelled: 5,
} as const

export type OrderStatusValue = (typeof OrderStatus)[keyof typeof OrderStatus]

export const ORDER_STATUS_TEXT: Record<OrderStatusValue, string> = {
  [OrderStatus.Placed]: '已下单',
  [OrderStatus.Paid]: '已支付',
  [OrderStatus.Processing]: '处理中',
  [OrderStatus.Shipped]: '已发货',
  [OrderStatus.Completed]: '已完成',
  [OrderStatus.Cancelled]: '已取消',
}

export const ORDER_STATUS_TAG_TYPE: Record<
  OrderStatusValue,
  'info' | 'success' | 'warning' | 'danger' | 'primary'
> = {
  [OrderStatus.Placed]: 'info',
  [OrderStatus.Paid]: 'primary',
  [OrderStatus.Processing]: 'warning',
  [OrderStatus.Shipped]: 'primary',
  [OrderStatus.Completed]: 'success',
  [OrderStatus.Cancelled]: 'danger',
}

export interface OrderItem {
  id: number
  orderId: number
  bookId: number
  title: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

/** Bare order returned by GET /api/orders (no nested items). */
export interface Order {
  id: number
  customerName: string
  totalAmount: number
  status: OrderStatusValue
  createdAt: string
  updatedAt: string
}

/** Order with its items, returned by checkout / GET /api/orders/{id}. */
export interface OrderResponse extends Order {
  items: OrderItem[]
}

export interface TokenResponse {
  access_token: string
  token_type: string
  expires_in: number
  scope: string
  [key: string]: unknown
}

export interface UserInfo {
  sub: string
  name: string
  email: string
  role: string
}
