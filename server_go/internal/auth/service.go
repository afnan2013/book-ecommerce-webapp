package auth

import (
	"context"
	"errors"
	"fmt"
	"time"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/role"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/user"
	"github.com/jackc/pgx/v5/pgconn"
	"golang.org/x/crypto/bcrypt"
)

var ErrEmailTaken = errors.New("email already registered")
var ErrInvalidCredentials = errors.New("invalid email or password")

type AuthService struct {
	repo      *user.UserRepository
	jwtSecret string
}

func NewService(repo *user.UserRepository, jwtSecret string) *AuthService {
	return &AuthService{repo: repo, jwtSecret: jwtSecret}
}

func (s *AuthService) Register(ctx context.Context, email, password, fullName string, userType int16) (user.User, error) {
	passwordHash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return user.User{}, fmt.Errorf("generating password hashing: %w", err)
	}

	created, err := s.repo.Create(ctx, email, string(passwordHash), fullName, userType)
	if err != nil {
		var pgErr *pgconn.PgError

		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return user.User{}, ErrEmailTaken
		}
		return user.User{}, fmt.Errorf("creating user: %w", err)
	}

	return created, nil
}

func (s *AuthService) Login(ctx context.Context, email, password string) (string, time.Time, user.User, error) {
	u, ok, err := s.repo.GetByEmail(ctx, email)
	if err != nil {
		return "", time.Time{}, user.User{}, fmt.Errorf("getting user by email: %w", err)
	}
	if !ok {
		return "", time.Time{}, user.User{}, ErrInvalidCredentials
	}

	if err := bcrypt.CompareHashAndPassword([]byte(u.PasswordHash), []byte(password)); err != nil {
		return "", time.Time{}, user.User{}, ErrInvalidCredentials
	}

	permissions, err := s.repo.GetPermissions(ctx, u.ID)
	if err != nil {
		return "", time.Time{}, user.User{}, fmt.Errorf("getting permissions: %w", err)
	}

	expiresAt := time.Now().Add(24 * time.Hour)
	token, err := generateToken(u.ID, permissions, expiresAt, s.jwtSecret)
	if err != nil {
		return "", time.Time{}, user.User{}, fmt.Errorf("generating token: %w", err)
	}

	return token, expiresAt, u, nil
}

func (s *AuthService) ListPermissions(ctx context.Context) ([]role.Permission, error) {
	permissions, err := s.repo.GetAllPermissions(ctx)
	if err != nil {
		return []role.Permission{}, fmt.Errorf("listing permissions: %w", err)
	}
	return permissions, nil
}
