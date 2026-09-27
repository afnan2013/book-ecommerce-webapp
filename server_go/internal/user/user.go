package user

import (
	"time"

	"github.com/google/uuid"
)

const (
	UserTypeEmployee int16 = 1
	UserTypeSeller   int16 = 2
	UserTypeBuyer    int16 = 3
)

type User struct {
	ID           uuid.UUID `json:"id"`
	Email        string    `json:"email"`
	PasswordHash string    `json:"-"`
	FullName     string    `json:"fullName"`
	UserType     int16     `json:"userType"`
	CreatedAt    time.Time `json:"createdAt"`
}

type Role struct {
	ID   uuid.UUID `json:"id"`
	Name string    `json:"name"`
}

type Permission struct {
	ID          uuid.UUID `json:"id"`
	Name        string    `json:"name"`
	Description string    `json:"description"`
}

type UserDetail struct {
	User
	ConcurrencyStamp  string       `json:"concurrencyStamp"`
	Roles             []Role       `json:"roles"`
	DirectPermissions []Permission `json:"directPermissions"`
}
