import { useState, type FormEvent } from 'react';
import type { Book, BookInput } from '../types/book';

interface Props {
  initial?: Book;
  onSubmit: (input: BookInput) => Promise<void>;
  onCancel: () => void;
}

function BookForm({ initial, onSubmit, onCancel }: Props) {
  const [title, setTitle] = useState(initial?.title ?? '');
  const [author, setAuthor] = useState(initial?.author ?? '');
  const [price, setPrice] = useState(initial?.price?.toString() ?? '');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const priceNum = Number(price);
    if (!title.trim() || !author.trim() || Number.isNaN(priceNum)) {
      setError('All fields are required; price must be a number.');
      return;
    }
    try {
      setSubmitting(true);
      setError(null);
      await onSubmit({ title: title.trim(), author: author.trim(), price: priceNum });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed');
      setSubmitting(false);
    }
  }

  return (
    <form className="book-form" onSubmit={handleSubmit}>
      <h2>{initial ? 'Edit book' : 'New book'}</h2>

      <label>
        Title
        <input value={title} onChange={e => setTitle(e.target.value)} disabled={submitting} />
      </label>

      <label>
        Author
        <input value={author} onChange={e => setAuthor(e.target.value)} disabled={submitting} />
      </label>

      <label>
        Price
        <input
          value={price}
          onChange={e => setPrice(e.target.value)}
          disabled={submitting}
          inputMode="decimal"
          placeholder="0.00"
        />
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" className="primary" disabled={submitting}>
          {submitting ? 'Saving…' : 'Save'}
        </button>
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      </div>
    </form>
  );
}

export default BookForm;
