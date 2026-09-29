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
var ErrNotFound = errors.New("user not found")
var ErrConflict = errors.New("user was modified by someone else")

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
	detail, ok, err := s.repo.GetByID(ctx, userID.String())
	if err != nil {
		return UserDetail{}, false, fmt.Errorf("getting user: %w", err)
	}

	if !ok {
		return UserDetail{}, false, nil
	}

	if err := s.attachRolesAndPermissions(ctx, &detail); err != nil {
		return UserDetail{}, false, err
	}
	return detail, true, nil
}

func (s *UserService) attachRolesAndPermissions(ctx context.Context, detail *UserDetail) error {
	roles, err := s.repo.GetRoles(ctx, []uuid.UUID{detail.ID})
	if err != nil {
		return fmt.Errorf("getting roles: %w", err)
	}

	permissions, err := s.repo.GetDirectPermissions(ctx, []uuid.UUID{detail.ID})
	if err != nil {
		return fmt.Errorf("getting direct permissions: %w", err)
	}

	if r, ok := roles[detail.ID]; ok {
		detail.Roles = r
	}
	if p, ok := permissions[detail.ID]; ok {
		detail.DirectPermissions = p
	}
	return nil
}

func (s *UserService) UpdateProfile(ctx context.Context, userID uuid.UUID, fullName string, phoneNumber, address *string, stamp string) (UserDetail, error) {
	_, ok, err := s.repo.GetByID(ctx, userID.String())
	if err != nil {
		return UserDetail{}, fmt.Errorf("getting user: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrNotFound
	}

	updated, ok, err := s.repo.UpdateProfile(ctx, userID, fullName, phoneNumber, address, stamp)
	if err != nil {
		return UserDetail{}, fmt.Errorf("updating profile: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrConflict
	}

	if err := s.attachRolesAndPermissions(ctx, &updated); err != nil {
		return UserDetail{}, err
	}
	return updated, nil
}

func (s *UserService) SetRoles(ctx context.Context, userID uuid.UUID, roleIDs []uuid.UUID, stamp string) (UserDetail, error) {
	_, ok, err := s.repo.GetByID(ctx, userID.String())
	if err != nil {
		return UserDetail{}, fmt.Errorf("getting user: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrNotFound
	}

	ok, err = s.repo.SetRoles(ctx, userID, roleIDs, stamp)
	if err != nil {
		return UserDetail{}, fmt.Errorf("setting user roles: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrConflict
	}

	updated, _, err := s.GetByID(ctx, userID)
	if err != nil {
		return UserDetail{}, err
	}
	return updated, nil
}

func (s *UserService) SetDirectPermissions(ctx context.Context, userID uuid.UUID, permissionIDs []uuid.UUID, stamp string) (UserDetail, error) {
	_, ok, err := s.repo.GetByID(ctx, userID.String())
	if err != nil {
		return UserDetail{}, fmt.Errorf("getting user: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrNotFound
	}

	ok, err = s.repo.SetDirectPermissions(ctx, userID, permissionIDs, stamp)
	if err != nil {
		return UserDetail{}, fmt.Errorf("setting user permissions: %w", err)
	}
	if !ok {
		return UserDetail{}, ErrConflict
	}

	updated, _, err := s.GetByID(ctx, userID)
	if err != nil {
		return UserDetail{}, err
	}
	return updated, nil
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

func (s *UserService) DeleteUser(ctx context.Context, id string) (bool, error) {
	ok, err := s.repo.DeleteUserById(ctx, id)
	if err != nil {
		return false, fmt.Errorf("deleting the user : %w", err)
	}
	if !ok {
		return false, ErrNotFound
	}

	return true, nil
}
