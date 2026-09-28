package auth

import (
	"net/http"
	"strings"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/authctx"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
)

func RequireAuth(jwtSecret string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			token, ok := strings.CutPrefix(r.Header.Get("Authorization"), "Bearer ")
			if !ok {
				server.WriteError(w, http.StatusUnauthorized, "invalid or missing authorization header")
				return
			}

			claims := &tokenClaims{}
			parsed, err := jwt.ParseWithClaims(token, claims, func(t *jwt.Token) (any, error) {
				return []byte(jwtSecret), nil
			})
			if err != nil || !parsed.Valid {
				server.WriteError(w, http.StatusUnauthorized, "invalid or expired token")
				return
			}

			userID, err := uuid.Parse(claims.Subject)
			if err != nil {
				server.WriteError(w, http.StatusUnauthorized, "invalid token subject")
				return
			}

			permissions := make(map[string]bool, len(claims.Permissions))
			for _, p := range claims.Permissions {
				permissions[p] = true
			}

			ctx := authctx.WithUserID(r.Context(), userID)
			ctx = authctx.WithPermissions(ctx, permissions)
			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

func RequirePermission(permissions ...string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			perms, ok := authctx.Permissions(r.Context())
			if !ok {
				server.WriteError(w, http.StatusForbidden, "forbidden")
				return
			}
			for _, p := range permissions {
				if perms[p] {
					next.ServeHTTP(w, r)
					return
				}
			}
			server.WriteError(w, http.StatusForbidden, "forbidden")
		})
	}
}
