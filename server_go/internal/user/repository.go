package user

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/role"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type UserRepository struct {
	pool server.DBQuerier
}

func NewRepository(pool server.DBQuerier) *UserRepository {
	return &UserRepository{pool: pool}
}

func (r *UserRepository) ListUsers(ctx context.Context) ([]UserDetail, error) {
	rows, err := r.pool.Query(ctx, "SELECT id, email, full_name, user_type, created_at, concurrency_stamp FROM users")
	if err != nil {
		return nil, fmt.Errorf("listing users: %w", err)
	}
	defer rows.Close()

	users := make([]UserDetail, 0)
	for rows.Next() {
		var ud UserDetail
		if err := rows.Scan(&ud.ID, &ud.Email, &ud.FullName, &ud.UserType, &ud.CreatedAt, &ud.ConcurrencyStamp); err != nil {
			return nil, fmt.Errorf("scanning user: %w", err)
		}
		ud.Roles = []Role{}
		ud.DirectPermissions = []Permission{}
		users = append(users, ud)
	}
	return users, rows.Err()
}

func (r *UserRepository) GetRoles(ctx context.Context, userIDs []uuid.UUID) (map[uuid.UUID][]Role, error) {
	rows, err := r.pool.Query(ctx, `
        SELECT ur.user_id, r.id, r.name
        FROM user_roles ur
        JOIN roles r ON r.id = ur.role_id
        WHERE ur.user_id = ANY($1)
    `, userIDs)
	if err != nil {
		return nil, fmt.Errorf("getting roles for users: %w", err)
	}
	defer rows.Close()

	result := make(map[uuid.UUID][]Role)
	for rows.Next() {
		var userID uuid.UUID
		var role Role
		if err := rows.Scan(&userID, &role.ID, &role.Name); err != nil {
			return nil, fmt.Errorf("scanning user role: %w", err)
		}
		result[userID] = append(result[userID], role)
	}
	return result, rows.Err()
}

// Direct grants only, unlike GetPermissions this excludes anything granted via a role.
func (r *UserRepository) GetDirectPermissions(ctx context.Context, userIDs []uuid.UUID) (map[uuid.UUID][]Permission, error) {
	rows, err := r.pool.Query(ctx, `
        SELECT up.user_id, p.id, p.name, p.description
        FROM user_permissions up
        JOIN permissions p ON p.id = up.permission_id
        WHERE up.user_id = ANY($1)
    `, userIDs)
	if err != nil {
		return nil, fmt.Errorf("getting direct permissions for users: %w", err)
	}
	defer rows.Close()

	result := make(map[uuid.UUID][]Permission)
	for rows.Next() {
		var userID uuid.UUID
		var perm Permission
		if err := rows.Scan(&userID, &perm.ID, &perm.Name, &perm.Description); err != nil {
			return nil, fmt.Errorf("scanning direct permission: %w", err)
		}
		result[userID] = append(result[userID], perm)
	}
	return result, rows.Err()
}

func (r *UserRepository) Create(ctx context.Context, email, passwordHash, fullName string, userType int16) (User, error) {
	var u User
	err := r.pool.QueryRow(ctx,
		"INSERT INTO users (email, password_hash, full_name, user_type, concurrency_stamp) VALUES ($1, $2, $3, $4, $5) RETURNING id, email, password_hash, full_name, user_type, created_at",
		email, passwordHash, fullName, userType, uuid.NewString(),
	).Scan(&u.ID, &u.Email, &u.PasswordHash, &u.FullName, &u.UserType, &u.CreatedAt)
	if err != nil {
		return User{}, fmt.Errorf("creating user: %w", err)
	}
	return u, nil
}

func (r *UserRepository) GetByEmail(ctx context.Context, email string) (User, bool, error) {
	var u User
	err := r.pool.QueryRow(ctx,
		"SELECT id, email, password_hash, full_name, user_type, created_at FROM users WHERE email = $1", email,
	).Scan(&u.ID, &u.Email, &u.PasswordHash, &u.FullName, &u.UserType, &u.CreatedAt)

	if errors.Is(err, pgx.ErrNoRows) {
		return User{}, false, nil
	}
	if err != nil {
		return User{}, false, fmt.Errorf("getting user by email %q: %w", email, err)
	}
	return u, true, nil
}

func (r *UserRepository) GetByID(ctx context.Context, userID string) (User, bool, error) {
	var u User
	err := r.pool.QueryRow(ctx,
		"SELECT id, email, password_hash, full_name, user_type, created_at FROM users WHERE id = $1", userID,
	).Scan(&u.ID, &u.Email, &u.PasswordHash, &u.FullName, &u.UserType, &u.CreatedAt)

	if errors.Is(err, pgx.ErrNoRows) {
		return User{}, false, nil
	}
	if err != nil {
		return User{}, false, fmt.Errorf("getting user by id %q: %w", userID, err)
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

func (r *UserRepository) GetAllPermissions(ctx context.Context) ([]role.Permission, error) {
	rows, err := r.pool.Query(ctx, "SELECT id, name, description FROM permissions")
	if err != nil {
		return []role.Permission{}, fmt.Errorf("listing existing permissions: %w", err)
	}

	permissions := make([]role.Permission, 0)
	for rows.Next() {
		var p role.Permission
		if err := rows.Scan(&p.ID, &p.Name, &p.Description); err != nil {
			rows.Close()
			return []role.Permission{}, fmt.Errorf("scanning permission: %w", err)
		}
		permissions = append(permissions, p)
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return []role.Permission{}, fmt.Errorf("iterating permissions: %w", err)
	}

	return permissions, nil
}
