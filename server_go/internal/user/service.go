package user

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgconn"
	"golang.org/x/crypto/bcrypt"
)

var ErrEmailTaken = errors.New("email already registered")

type UserService struct {
	repo *UserRepository
}

func NewService(repo *UserRepository) *UserService {
	return &UserService{repo: repo}
}

func (s *UserService) List(ctx context.Context) ([]UserDetail, error) {
	users, err := s.repo.ListUsers(ctx)
	if err != nil {
		return nil, fmt.Errorf("listing users: %w", err)
	}

	ids := make([]uuid.UUID, len(users))
	for i, u := range users {
		ids[i] = u.ID
	}

	roles, err := s.repo.GetRoles(ctx, ids)
	if err != nil {
		return nil, fmt.Errorf("getting roles: %w", err)
	}

	permissions, err := s.repo.GetDirectPermissions(ctx, ids)
	if err != nil {
		return nil, fmt.Errorf("getting direct permissions: %w", err)
	}

	for i := range users {
		if r, ok := roles[users[i].ID]; ok {
			users[i].Roles = r
		}
		if p, ok := permissions[users[i].ID]; ok {
			users[i].DirectPermissions = p
		}
	}

	return users, nil
}

func (s *UserService) GetByID(ctx context.Context, userID uuid.UUID) (UserDetail, bool, error) {
	user, ok, err := s.repo.GetByID(ctx, userID.String())
	if err != nil {
		return UserDetail{}, false, fmt.Errorf("listing users: %w", err)
	}

	if !ok {
		return UserDetail{}, false, nil
	}

	roles, err := s.repo.GetRoles(ctx, []uuid.UUID{userID})
	if err != nil {
		return UserDetail{}, false, fmt.Errorf("getting roles: %w", err)
	}

	permissions, err := s.repo.GetDirectPermissions(ctx, []uuid.UUID{userID})
	if err != nil {
		return UserDetail{}, false, fmt.Errorf("getting direct permissions: %w", err)
	}

	return UserDetail{
		User:              user,
		Roles:             roles[userID],
		DirectPermissions: permissions[userID],
	}, true, nil

}

func (s *UserService) CreateUser(ctx context.Context, email, password, fullName string, userType int16) (User, error) {
	passwordHash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return User{}, fmt.Errorf("generating password hashing: %w", err)
	}

	created, err := s.repo.Create(ctx, email, string(passwordHash), fullName, userType)
	if err != nil {
		var pgErr *pgconn.PgError

		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return User{}, ErrEmailTaken
		}
		return User{}, fmt.Errorf("creating user: %w", err)
	}

	return created, nil
}
