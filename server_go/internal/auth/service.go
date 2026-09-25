package auth

import (
	"context"
	"errors"
	"fmt"

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

func (s *AuthService) Register(ctx context.Context, email, password string) (user.User, error) {
	passwordHash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return user.User{}, fmt.Errorf("generating password hashing: %w", err)
	}

	created, err := s.repo.Create(ctx, email, string(passwordHash))
	if err != nil {
		var pgErr *pgconn.PgError

		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return user.User{}, ErrEmailTaken
		}
		return user.User{}, fmt.Errorf("creating user: %w", err)
	}

	return created, nil
}

func (s *AuthService) Login(ctx context.Context, email, password string) (string, error) {
	user, ok, err := s.repo.GetByEmail(ctx, email)
	if err != nil {
		return "", fmt.Errorf("getting user by email: %w", err)
	}
	if !ok {
		return "", ErrInvalidCredentials
	}

	if err := bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte(password)); err != nil {
		return "", ErrInvalidCredentials
	}

	token, err := generateToken(user.ID, s.jwtSecret)
	if err != nil {
		return "", fmt.Errorf("generating token: %w", err)
	}

	return token, nil
}
