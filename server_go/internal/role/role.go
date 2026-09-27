package role

import "github.com/google/uuid"

type Permission struct {
	ID          uuid.UUID `json:"id"`
	Name        string    `json:"name"`
	Description string    `json:"description"`
}

type Role struct {
	ID               uuid.UUID    `json:"id"`
	Name             string       `json:"name"`
	ConcurrencyStamp string       `json:"concurrencyStamp"`
	IsSystem         bool         `json:"isSystem"`
	Permissions      []Permission `json:"permissions"`
}
