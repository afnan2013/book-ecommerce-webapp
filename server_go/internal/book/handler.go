package book

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strconv"

	"github.com/shopspring/decimal"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

func ListHandler(repo *BookRepository) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		books, err := repo.List(r.Context())
		if err != nil {
			log.Printf("list books: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}
		if len(books) == 0 {
			server.WriteJSON(w, http.StatusOK, []Book{})
			return
		}
		server.WriteJSON(w, http.StatusOK, books)
	}
}

func GetByIDHandler(repo *BookRepository) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := strconv.Atoi(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid book id")
			return
		}

		b, ok, err := repo.GetByID(r.Context(), id)
		if err != nil {
			log.Printf("getting book by id: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		if !ok {
			server.WriteError(w, http.StatusNotFound, "book not found")
			return
		}

		server.WriteJSON(w, http.StatusOK, b)
	}
}

func AddHandler(repo *BookRepository) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var b Book

		if err := json.NewDecoder(r.Body).Decode(&b); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}

		fields := map[string]string{}
		if b.Title == "" {
			fields["title"] = "is required"
		}
		if b.Price.LessThanOrEqual(decimal.Zero) {
			fields["price"] = "must be greater than 0"
		}
		if !b.Price.Equal(b.Price.Round(2)) {
			fields["price"] = "must have at most 2 decimal places"
		}
		if b.Author == "" {
			fields["author"] = "is required"
		}

		if len(fields) > 0 {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{Error: "input validation failed", Fields: fields})
			return
		}

		created, err := repo.Add(r.Context(), b)
		if err != nil {
			log.Printf("creating book: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}
		w.Header().Set("Location", fmt.Sprintf("/books/%d", created.ID))
		server.WriteJSON(w, http.StatusCreated, created)
	}
}

func UpdateHandler(repo *BookRepository) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := strconv.Atoi(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid book id")
			return
		}

		var b Book
		if err := json.NewDecoder(r.Body).Decode(&b); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}

		fields := map[string]string{}
		if b.Title == "" {
			fields["title"] = "is required"
		}
		if b.Price.LessThanOrEqual(decimal.Zero) {
			fields["price"] = "must be greater than 0"
		}
		if !b.Price.Equal(b.Price.Round(2)) {
			fields["price"] = "must have at most 2 decimal places"
		}
		if b.Author == "" {
			fields["author"] = "is required"
		}

		if len(fields) > 0 {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{Error: "input validation failed", Fields: fields})
			return
		}

		found, err := repo.Update(r.Context(), id, b)
		if err != nil {
			log.Printf("updating book: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		if !found {
			server.WriteError(w, http.StatusNotFound, "book not found")
			return
		}

		b.ID = id
		server.WriteJSON(w, http.StatusOK, b)
	}
}

func DeleteHandler(repo *BookRepository) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := strconv.Atoi(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid book id")
			return
		}

		found, err := repo.Delete(r.Context(), id)
		if err != nil {
			log.Printf("deleting book: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		if !found {
			server.WriteError(w, http.StatusNotFound, "book not found")
			return
		}

		w.WriteHeader(http.StatusNoContent)
	}
}
