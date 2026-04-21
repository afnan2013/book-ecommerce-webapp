import type { Book, BookInput } from '../types/book';

const API_BASE = 'http://localhost:5027/api';

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export function listBooks(): Promise<Book[]> {
  return fetch(`${API_BASE}/books`).then(handle<Book[]>);
}

export function getBook(id: number): Promise<Book> {
  return fetch(`${API_BASE}/books/${id}`).then(handle<Book>);
}

export function createBook(input: BookInput): Promise<Book> {
  return fetch(`${API_BASE}/books`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  }).then(handle<Book>);
}

export function updateBook(id: number, input: BookInput): Promise<void> {
  return fetch(`${API_BASE}/books/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ id, ...input }),
  }).then(handle<void>);
}

export function deleteBook(id: number): Promise<void> {
  return fetch(`${API_BASE}/books/${id}`, { method: 'DELETE' }).then(handle<void>);
}
