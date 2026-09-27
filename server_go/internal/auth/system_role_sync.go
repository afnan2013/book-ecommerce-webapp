package auth

import (
	"context"
	"fmt"
	"maps"
	"slices"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/google/uuid"
)

func syncSystemRole(ctx context.Context, pool server.DBQuerier, name string, permissions []string) error {
	var roleID uuid.UUID
	err := pool.QueryRow(ctx, `
        INSERT INTO roles (name, concurrency_stamp, is_system)
        VALUES ($1, $2, true)
        ON CONFLICT (name) DO UPDATE SET is_system = true
        RETURNING id
    `, name, uuid.NewString()).Scan(&roleID)
	if err != nil {
		return fmt.Errorf("upserting system role %q: %w", name, err)
	}

	rows, err := pool.Query(ctx, `
        SELECT p.name FROM role_permissions rp
        JOIN permissions p ON p.id = rp.permission_id
        WHERE rp.role_id = $1
    `, roleID)
	if err != nil {
		return fmt.Errorf("listing current grants for %q: %w", name, err)
	}
	existing := map[string]bool{}
	for rows.Next() {
		var p string
		if err := rows.Scan(&p); err != nil {
			rows.Close()
			return fmt.Errorf("scanning granted permission for %q: %w", name, err)
		}
		existing[p] = true
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return fmt.Errorf("iterating granted permissions for %q: %w", name, err)
	}

	target := map[string]bool{}
	for _, p := range permissions {
		target[p] = true
	}

	for p := range target {
		if existing[p] {
			continue
		}
		if _, err := pool.Exec(ctx, `
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT $1, id FROM permissions WHERE name = $2
        `, roleID, p); err != nil {
			return fmt.Errorf("granting %q to %q: %w", p, name, err)
		}
	}

	for p := range existing {
		if target[p] {
			continue
		}
		if _, err := pool.Exec(ctx, `
            DELETE FROM role_permissions
            WHERE role_id = $1 AND permission_id = (SELECT id FROM permissions WHERE name = $2)
        `, roleID, p); err != nil {
			return fmt.Errorf("revoking %q from %q: %w", p, name, err)
		}
	}

	return nil
}

func SyncSuperAdminRole(ctx context.Context, pool server.DBQuerier) error {
	return syncSystemRole(ctx, pool, "SuperAdmin", slices.Collect(maps.Keys(PermissionList)))
}
