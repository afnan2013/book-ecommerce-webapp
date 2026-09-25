package auth

import (
	"context"
	"net/http"
	"strconv"
	"strings"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/golang-jwt/jwt/v5"
)

type contextKey string

var userIDContextKey contextKey = "userID"

func RequireAuth(jwtSecret string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			token, ok := strings.CutPrefix(r.Header.Get("Authorization"), "Bearer ")
			if !ok {
				server.WriteError(w, http.StatusUnauthorized, "invalid or missing authorization header")
				return
			}

			claims := &jwt.RegisteredClaims{}
			parsed, err := jwt.ParseWithClaims(token, claims, func(t *jwt.Token) (any, error) {
				return []byte(jwtSecret), nil
			})
			if err != nil || !parsed.Valid {
				server.WriteError(w, http.StatusUnauthorized, "invalid or expired token")
				return
			}

			userID, err := strconv.Atoi(claims.Subject)
			if err != nil {
				server.WriteError(w, http.StatusUnauthorized, "invalid token subject")
				return
			}

			ctx := context.WithValue(r.Context(), userIDContextKey, userID)
			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

func UserIDFromContext(ctx context.Context) (int, bool) {
	id, ok := ctx.Value(userIDContextKey).(int)
	return id, ok
}
