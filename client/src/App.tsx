import { useEffect, useState } from 'react';
import type { Book, BookInput } from './types/book';
import { createBook, deleteBook, listBooks, updateBook } from './api/books';
import BookForm from './components/BookForm';
import './App.css';

function App() {
  const [books, setBooks] = useState<Book[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<Book | null>(null);
  const [formOpen, setFormOpen] = useState(false);

  // Initial fetch on mount. Inlined as an async IIFE so the effect body has
  // no synchronous setState (the first line yields at `await`), and an `alive`
  // flag avoids state updates after unmount.
  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const data = await listBooks();
        if (alive) {
          setBooks(data);
          setError(null);
        }
      } catch (e) {
        if (alive) setError(e instanceof Error ? e.message : 'Failed to load');
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
  }, []);

  async function refresh() {
    try {
      const data = await listBooks();
      setBooks(data);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    }
  }

  async function handleSubmit(input: BookInput) {
    if (editing) {
      await updateBook(editing.id, input);
    } else {
      await createBook(input);
    }
    setFormOpen(false);
    setEditing(null);
    await refresh();
  }

  async function handleDelete(book: Book) {
    if (!window.confirm(`Delete "${book.title}"?`)) return;
    try {
      await deleteBook(book.id);
      await refresh();
    } catch (e) {
      window.alert(e instanceof Error ? e.message : 'Delete failed');
    }
  }

  function startEdit(book: Book) {
    setEditing(book);
    setFormOpen(true);
  }

  function startCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  function cancel() {
    setEditing(null);
    setFormOpen(false);
  }

  return (
    <main className="container">
      <header className="page-header">
        <h1>Books</h1>
        {!formOpen && (
          <button className="primary" onClick={startCreate}>
            + Add book
          </button>
        )}
      </header>

      {formOpen && (
        <BookForm
          initial={editing ?? undefined}
          onSubmit={handleSubmit}
          onCancel={cancel}
        />
      )}

      {loading && <p>Loading…</p>}
      {error && <p className="error">Error: {error}</p>}

      {!loading && !error && (
        <table className="books">
          <thead>
            <tr>
              <th>Title</th>
              <th>Author</th>
              <th>Price</th>
              <th aria-label="actions" />
            </tr>
          </thead>
          <tbody>
            {books.length === 0 && (
              <tr>
                <td colSpan={4} className="empty">
                  No books yet. Click "Add book" to create one.
                </td>
              </tr>
            )}
            {books.map(b => (
              <tr key={b.id}>
                <td>{b.title}</td>
                <td>{b.author}</td>
                <td>${b.price.toFixed(2)}</td>
                <td className="row-actions">
                  <button onClick={() => startEdit(b)}>Edit</button>
                  <button onClick={() => handleDelete(b)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}

export default App;
