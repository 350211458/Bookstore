import { http } from './client'
import type { Book, BookInput, PagedResult } from '@/types'

export interface BookQuery {
  keyword?: string
  category?: string
  minPrice?: number
  maxPrice?: number
  page?: number
  pageSize?: number
}

export async function listBooks(query: BookQuery): Promise<PagedResult<Book>> {
  const { data } = await http.get<PagedResult<Book>>('/catalog/api/books', {
    params: query,
  })
  return data
}

export async function getBook(id: number): Promise<Book> {
  const { data } = await http.get<Book>(`/catalog/api/books/${id}`)
  return data
}

export async function createBook(input: BookInput): Promise<Book> {
  const { data } = await http.post<Book>('/catalog/api/books', input)
  return data
}

export async function updateBook(id: number, input: BookInput): Promise<Book> {
  const { data } = await http.put<Book>(`/catalog/api/books/${id}`, input)
  return data
}

export async function deleteBook(id: number): Promise<void> {
  await http.delete(`/catalog/api/books/${id}`)
}

export async function adjustStock(id: number, delta: number): Promise<Book> {
  const { data } = await http.patch<Book>(`/catalog/api/books/${id}/stock`, {
    delta,
  })
  return data
}
