package user

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type UserRepository struct {
	pool server.DBQuerier
}

func NewRepository(pool server.DBQuerier) *UserRepository {
	return &UserRepository{pool: pool}
}

func (r *UserRepository) Create(ctx context.Context, email, passwordHash string) (User, error) {
	var u User
	err := r.pool.QueryRow(ctx,
		"INSERT INTO users (email, password_hash) VALUES ($1, $2) RETURNING id, email, password_hash, created_at",
		email, passwordHash,
	).Scan(&u.ID, &u.Email, &u.PasswordHash, &u.CreatedAt)
	if err != nil {
		return User{}, fmt.Errorf("creating user: %w", err)
	}
	return u, nil
}

func (r *UserRepository) GetByEmail(ctx context.Context, email string) (User, bool, error) {
	var u User
	err := r.pool.QueryRow(ctx,
		"SELECT id, email, password_hash, created_at FROM users WHERE email = $1", email,
	).Scan(&u.ID, &u.Email, &u.PasswordHash, &u.CreatedAt)

	if errors.Is(err, pgx.ErrNoRows) {
		return User{}, false, nil
	}
	if err != nil {
		return User{}, false, fmt.Errorf("getting user by email %q: %w", email, err)
	}
	return u, true, nil
}

func (r *UserRepository) GetPermissions(ctx context.Context, userID uuid.UUID) ([]string, error) {
	rows, err := r.pool.Query(ctx, `
        SELECT p.name FROM permissions p
        JOIN role_permissions rp ON rp.permission_id = p.id
        JOIN user_roles ur ON ur.role_id = rp.role_id
        WHERE ur.user_id = $1
        UNION
        SELECT p.name FROM permissions p
        JOIN user_permissions up ON up.permission_id = p.id
        WHERE up.user_id = $1
    `, userID)
	if err != nil {
		return nil, fmt.Errorf("getting permissions for user %s: %w", userID, err)
	}
	defer rows.Close()

	var permissions []string
	for rows.Next() {
		var name string
		if err := rows.Scan(&name); err != nil {
			return nil, fmt.Errorf("scanning permission: %w", err)
		}
		permissions = append(permissions, name)
	}
	return permissions, rows.Err()
}
