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
