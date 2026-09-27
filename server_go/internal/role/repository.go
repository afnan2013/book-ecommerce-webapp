package role

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type RoleRepository struct {
	pool server.DBQuerier
}

func NewRepository(pool server.DBQuerier) *RoleRepository {
	return &RoleRepository{pool: pool}
}

func (r *RoleRepository) ListRoles(ctx context.Context) ([]Role, error) {
	rows, err := r.pool.Query(ctx, "SELECT id, name, concurrency_stamp, is_system FROM roles")
	if err != nil {
		return nil, fmt.Errorf("listing roles: %w", err)
	}
	defer rows.Close()

	roles := make([]Role, 0)
	for rows.Next() {
		var role Role
		if err := rows.Scan(&role.ID, &role.Name, &role.ConcurrencyStamp, &role.IsSystem); err != nil {
			return nil, fmt.Errorf("scanning role: %w", err)
		}
		role.Permissions = []Permission{}
		roles = append(roles, role)
	}
	return roles, rows.Err()
}

func (r *RoleRepository) GetRoleByID(ctx context.Context, id uuid.UUID) (Role, bool, error) {
	var role Role
	err := r.pool.QueryRow(ctx,
		"SELECT id, name, concurrency_stamp, is_system FROM roles WHERE id = $1", id,
	).Scan(&role.ID, &role.Name, &role.ConcurrencyStamp, &role.IsSystem)
	if errors.Is(err, pgx.ErrNoRows) {
		return Role{}, false, nil
	}
	if err != nil {
		return Role{}, false, fmt.Errorf("getting role %s: %w", id, err)
	}
	role.Permissions = []Permission{}
	return role, true, nil
}

func (r *RoleRepository) GetPermissionsForRoles(ctx context.Context, roleIDs []uuid.UUID) (map[uuid.UUID][]Permission, error) {
	rows, err := r.pool.Query(ctx, `
        SELECT rp.role_id, p.id, p.name, p.description
        FROM role_permissions rp
        JOIN permissions p ON p.id = rp.permission_id
        WHERE rp.role_id = ANY($1)
    `, roleIDs)
	if err != nil {
		return nil, fmt.Errorf("getting permissions for roles: %w", err)
	}
	defer rows.Close()

	result := make(map[uuid.UUID][]Permission)
	for rows.Next() {
		var roleID uuid.UUID
		var p Permission
		if err := rows.Scan(&roleID, &p.ID, &p.Name, &p.Description); err != nil {
			return nil, fmt.Errorf("scanning role permission: %w", err)
		}
		result[roleID] = append(result[roleID], p)
	}
	return result, rows.Err()
}

func (r *RoleRepository) Create(ctx context.Context, name string) (Role, error) {
	var role Role
	err := r.pool.QueryRow(ctx, `
        INSERT INTO roles (name, concurrency_stamp, is_system)
        VALUES ($1, $2, false)
        RETURNING id, name, concurrency_stamp, is_system
    `, name, uuid.NewString()).Scan(&role.ID, &role.Name, &role.ConcurrencyStamp, &role.IsSystem)
	if err != nil {
		return Role{}, fmt.Errorf("creating role: %w", err)
	}
	role.Permissions = []Permission{}
	return role, nil
}

// Update returns ok=false when either the role does not exist or oldStamp no longer matches.
func (r *RoleRepository) Update(ctx context.Context, id uuid.UUID, name, oldStamp string) (Role, bool, error) {
	var role Role
	err := r.pool.QueryRow(ctx, `
        UPDATE roles SET name = $1, concurrency_stamp = $2
        WHERE id = $3 AND concurrency_stamp = $4
        RETURNING id, name, concurrency_stamp, is_system
    `, name, uuid.NewString(), id, oldStamp).Scan(&role.ID, &role.Name, &role.ConcurrencyStamp, &role.IsSystem)
	if errors.Is(err, pgx.ErrNoRows) {
		return Role{}, false, nil
	}
	if err != nil {
		return Role{}, false, fmt.Errorf("updating role %s: %w", id, err)
	}
	role.Permissions = []Permission{}
	return role, true, nil
}

func (r *RoleRepository) Delete(ctx context.Context, id uuid.UUID) (bool, error) {
	tag, err := r.pool.Exec(ctx, "DELETE FROM roles WHERE id = $1", id)
	if err != nil {
		return false, fmt.Errorf("deleting role %s: %w", id, err)
	}
	return tag.RowsAffected() > 0, nil
}

// Runs in a transaction so a concurrent stamp mismatch can't leave permissions half-applied.
func (r *RoleRepository) SetPermissions(ctx context.Context, id uuid.UUID, permissionIDs []uuid.UUID, oldStamp string) (Role, bool, error) {
	tx, err := r.pool.Begin(ctx)
	if err != nil {
		return Role{}, false, fmt.Errorf("beginning transaction: %w", err)
	}
	defer tx.Rollback(ctx)

	var role Role
	err = tx.QueryRow(ctx, `
        UPDATE roles SET concurrency_stamp = $1
        WHERE id = $2 AND concurrency_stamp = $3
        RETURNING id, name, concurrency_stamp, is_system
    `, uuid.NewString(), id, oldStamp).Scan(&role.ID, &role.Name, &role.ConcurrencyStamp, &role.IsSystem)
	if errors.Is(err, pgx.ErrNoRows) {
		return Role{}, false, nil
	}
	if err != nil {
		return Role{}, false, fmt.Errorf("updating role stamp: %w", err)
	}

	if _, err := tx.Exec(ctx, "DELETE FROM role_permissions WHERE role_id = $1", id); err != nil {
		return Role{}, false, fmt.Errorf("clearing role permissions: %w", err)
	}

	for _, pid := range permissionIDs {
		if _, err := tx.Exec(ctx,
			"INSERT INTO role_permissions (role_id, permission_id) VALUES ($1, $2)",
			id, pid,
		); err != nil {
			return Role{}, false, fmt.Errorf("granting permission %s to role %s: %w", pid, id, err)
		}
	}

	if err := tx.Commit(ctx); err != nil {
		return Role{}, false, fmt.Errorf("committing role permission update: %w", err)
	}

	perms, err := r.GetPermissionsForRoles(ctx, []uuid.UUID{id})
	if err != nil {
		return Role{}, false, err
	}
	role.Permissions = perms[id]
	if role.Permissions == nil {
		role.Permissions = []Permission{}
	}
	return role, true, nil
}
