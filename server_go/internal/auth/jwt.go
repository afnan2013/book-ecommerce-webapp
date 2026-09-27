package auth

import (
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
)

type tokenClaims struct {
	jwt.RegisteredClaims
	Permissions []string `json:"permissions"`
}

func generateToken(userID uuid.UUID, permissions []string, expiresAt time.Time, secret string) (string, error) {
	claims := tokenClaims{
		RegisteredClaims: jwt.RegisteredClaims{
			Subject:   userID.String(),
			ExpiresAt: jwt.NewNumericDate(expiresAt),
			IssuedAt:  jwt.NewNumericDate(time.Now()),
		},
		Permissions: permissions,
	}

	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	return token.SignedString([]byte(secret))
}
