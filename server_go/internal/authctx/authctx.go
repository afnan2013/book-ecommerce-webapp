package authctx

import (
	"context"

	"github.com/google/uuid"
)

type contextKey string

var userIDKey contextKey = "userID"
var permissionsKey contextKey = "permissions"

func WithUserID(ctx context.Context, userID uuid.UUID) context.Context {
	return context.WithValue(ctx, userIDKey, userID)
}

func UserID(ctx context.Context) (uuid.UUID, bool) {
	id, ok := ctx.Value(userIDKey).(uuid.UUID)
	return id, ok
}

func WithPermissions(ctx context.Context, permissions map[string]bool) context.Context {
	return context.WithValue(ctx, permissionsKey, permissions)
}

func Permissions(ctx context.Context) (map[string]bool, bool) {
	perms, ok := ctx.Value(permissionsKey).(map[string]bool)
	return perms, ok
}
