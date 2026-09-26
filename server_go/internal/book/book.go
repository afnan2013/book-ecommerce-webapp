package book

import (
	"github.com/google/uuid"
	"github.com/shopspring/decimal"
)

type Book struct {
	ID     uuid.UUID       `json:"id"`
	Title  string          `json:"title"`
	Author string          `json:"author"`
	Price  decimal.Decimal `json:"price"`
}
