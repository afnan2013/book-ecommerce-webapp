package main

import (
	"net/http"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/auth"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/book"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/user"
)

func newRouter(pool server.DBQuerier, jwtSecret string) http.Handler {
	mux := http.NewServeMux()

	requireAuth := auth.RequireAuth(jwtSecret)

	bookRepo := book.NewRepository(pool)
	mux.Handle("GET /books", requireAuth(book.ListHandler(bookRepo)))
	mux.Handle("GET /books/{id}", requireAuth(book.GetByIDHandler(bookRepo)))
	mux.Handle("POST /books", requireAuth(book.AddHandler(bookRepo)))
	mux.Handle("PUT /books/{id}", requireAuth(book.UpdateHandler(bookRepo)))
	mux.Handle("DELETE /books/{id}", requireAuth(book.DeleteHandler(bookRepo)))

	userRepo := user.NewRepository(pool)
	authSvc := auth.NewService(userRepo, jwtSecret)
	mux.HandleFunc("POST /register", auth.RegisterHandler(authSvc))
	mux.HandleFunc("POST /login", auth.LoginHandler(authSvc))
	// mux.HandleFunc("GET /users/{id}", user.GetByIDHandler(userSvc))

	return server.Logging(server.Recovery(mux))
}
