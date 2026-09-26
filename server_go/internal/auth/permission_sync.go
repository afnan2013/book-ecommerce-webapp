package auth

import (
	"context"
	"fmt"
	"log"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

func SyncPermissions(ctx context.Context, pool server.DBQuerier) error {
	rows, err := pool.Query(ctx, "SELECT name, description FROM permissions")
	if err != nil {
		return fmt.Errorf("listing existing permissions: %w", err)
	}

	existing := map[string]string{}
	for rows.Next() {
		var name, description string
		if err := rows.Scan(&name, &description); err != nil {
			rows.Close()
			return fmt.Errorf("scanning permission: %w", err)
		}
		existing[name] = description
	}
	rows.Close()
	if err := rows.Err(); err != nil {
		return fmt.Errorf("iterating permissions: %w", err)
	}

	for name, description := range PermissionList {
		current, ok := existing[name]
		switch {
		case !ok:
			if _, err := pool.Exec(ctx,
				"INSERT INTO permissions (name, description) VAlUES ($1, $2)",
				name, description,
			); err != nil {
				return fmt.Errorf("inserting permission %q: %w", name, err)
			}
			log.Printf("added permission %q", name)
		case current != description:
			if _, err := pool.Exec(ctx,
				"UPDATE permissions SET description = $1 WHERE name = $2",
				description, name,
			); err != nil {
				return fmt.Errorf("updating permission %q: %w", name, err)
			}
			log.Printf("updated description for permission %q", name)
		}
	}

	for name := range existing {
		if _, ok := PermissionList[name]; ok {
			continue
		}
		if _, err := pool.Exec(ctx, "DELETE FROM permissions WHERE name = $1", name); err != nil {
			return fmt.Errorf("deleting orphan permission %q: %w", name, err)
		}
		log.Printf("removed orphan permission %q (no longer in code)", name)
	}

	return nil
}
