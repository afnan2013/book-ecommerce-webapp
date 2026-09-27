package role

import (
	"context"
	"errors"
	"fmt"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgconn"
)

var ErrNotFound = errors.New("role not found")
var ErrNameTaken = errors.New("role name already taken")
var ErrConflict = errors.New("role was modified by someone else")
var ErrSystemRole = errors.New("system roles cannot be modified")

type RoleService struct {
	repo *RoleRepository
}

func NewService(repo *RoleRepository) *RoleService {
	return &RoleService{repo: repo}
}

func (s *RoleService) List(ctx context.Context) ([]Role, error) {
	roles, err := s.repo.ListRoles(ctx)
	if err != nil {
		return nil, fmt.Errorf("listing roles: %w", err)
	}

	ids := make([]uuid.UUID, len(roles))
	for i, r := range roles {
		ids[i] = r.ID
	}

	permissions, err := s.repo.GetPermissionsForRoles(ctx, ids)
	if err != nil {
		return nil, fmt.Errorf("getting role permissions: %w", err)
	}

	for i := range roles {
		if p, ok := permissions[roles[i].ID]; ok {
			roles[i].Permissions = p
		}
	}
	return roles, nil
}

func (s *RoleService) GetByID(ctx context.Context, id uuid.UUID) (Role, error) {
	role, ok, err := s.repo.GetRoleByID(ctx, id)
	if err != nil {
		return Role{}, fmt.Errorf("getting role: %w", err)
	}
	if !ok {
		return Role{}, ErrNotFound
	}

	permissions, err := s.repo.GetPermissionsForRoles(ctx, []uuid.UUID{id})
	if err != nil {
		return Role{}, fmt.Errorf("getting role permissions: %w", err)
	}
	if p, ok := permissions[id]; ok {
		role.Permissions = p
	}
	return role, nil
}

func (s *RoleService) Create(ctx context.Context, name string) (Role, error) {
	created, err := s.repo.Create(ctx, name)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return Role{}, ErrNameTaken
		}
		return Role{}, fmt.Errorf("creating role: %w", err)
	}
	return created, nil
}

func (s *RoleService) Update(ctx context.Context, id uuid.UUID, name, stamp string) (Role, error) {
	existing, ok, err := s.repo.GetRoleByID(ctx, id)
	if err != nil {
		return Role{}, fmt.Errorf("getting role: %w", err)
	}
	if !ok {
		return Role{}, ErrNotFound
	}
	if existing.IsSystem {
		return Role{}, ErrSystemRole
	}

	updated, ok, err := s.repo.Update(ctx, id, name, stamp)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return Role{}, ErrNameTaken
		}
		return Role{}, fmt.Errorf("updating role: %w", err)
	}
	if !ok {
		return Role{}, ErrConflict
	}
	return updated, nil
}

func (s *RoleService) Delete(ctx context.Context, id uuid.UUID) error {
	existing, ok, err := s.repo.GetRoleByID(ctx, id)
	if err != nil {
		return fmt.Errorf("getting role: %w", err)
	}
	if !ok {
		return ErrNotFound
	}
	if existing.IsSystem {
		return ErrSystemRole
	}

	ok, err = s.repo.Delete(ctx, id)
	if err != nil {
		return fmt.Errorf("deleting role: %w", err)
	}
	if !ok {
		return ErrNotFound
	}
	return nil
}

func (s *RoleService) SetPermissions(ctx context.Context, id uuid.UUID, permissionIDs []uuid.UUID, stamp string) (Role, error) {
	existing, ok, err := s.repo.GetRoleByID(ctx, id)
	if err != nil {
		return Role{}, fmt.Errorf("getting role: %w", err)
	}
	if !ok {
		return Role{}, ErrNotFound
	}
	if existing.IsSystem {
		return Role{}, ErrSystemRole
	}

	updated, ok, err := s.repo.SetPermissions(ctx, id, permissionIDs, stamp)
	if err != nil {
		return Role{}, fmt.Errorf("setting role permissions: %w", err)
	}
	if !ok {
		return Role{}, ErrConflict
	}
	return updated, nil
}
