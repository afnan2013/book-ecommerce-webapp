package book

import (
	"context"
	"errors"
	"fmt"

	"github.com/jackc/pgx/v5"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type BookRepository struct {
	pool server.DBQuerier
}

func NewRepository(pool server.DBQuerier) *BookRepository {
	return &BookRepository{pool: pool}
}

func (r *BookRepository) Add(ctx context.Context, b Book) (Book, error) {
	err := r.pool.QueryRow(ctx, "INSERT INTO books (title, author, price) VALUES ($1, $2, $3) RETURNING id", b.Title, b.Author, b.Price).Scan(&b.ID)
	if err != nil {
		return Book{}, fmt.Errorf("adding book: %w", err)
	}
	return b, nil
}

func (r *BookRepository) GetByID(ctx context.Context, id int) (Book, bool, error) {
	var b Book
	err := r.pool.QueryRow(ctx, "SELECT id, title, author, price FROM books WHERE id = $1", id).Scan(
		&b.ID, &b.Title, &b.Author, &b.Price,
	)

	if errors.Is(err, pgx.ErrNoRows) {
		return Book{}, false, nil
	}
	if err != nil {
		return Book{}, false, fmt.Errorf("getting book %d: %w", id, err)
	}
	return b, true, nil
}

func (r *BookRepository) List(ctx context.Context) ([]Book, error) {
	rows, err := r.pool.Query(ctx, "SELECT * FROM books")
	if err != nil {
		return nil, fmt.Errorf("listing books: %w", err)
	}
	defer rows.Close()

	var books []Book
	for rows.Next() {
		var b Book
		if err := rows.Scan(&b.ID, &b.Title, &b.Author, &b.Price); err != nil {
			return nil, fmt.Errorf("scanning book: %w", err)
		}
		books = append(books, b)
	}

	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterating books: %w", err)
	}

	return books, nil
}

func (r *BookRepository) Update(ctx context.Context, id int, b Book) (bool, error) {
	tag, err := r.pool.Exec(ctx, "UPDATE books SET title = $1, author = $2, price = $3 WHERE id = $4", b.Title, b.Author, b.Price, id)
	if err != nil {
		return false, fmt.Errorf("updating book %d: %w", id, err)
	}
	return tag.RowsAffected() > 0, nil
}

func (r *BookRepository) Delete(ctx context.Context, id int) (bool, error) {
	tag, err := r.pool.Exec(ctx, "DELETE FROM books WHERE id = $1", id)
	if err != nil {
		return false, fmt.Errorf("deleting book %d: %w", id, err)
	}
	return tag.RowsAffected() > 0, nil
}
