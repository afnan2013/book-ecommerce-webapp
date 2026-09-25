package book

import "github.com/shopspring/decimal"

type Book struct {
	ID     int             `json:"id"`
	Title  string          `json:"title"`
	Author string          `json:"author"`
	Price  decimal.Decimal `json:"price"`
}
