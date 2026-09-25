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

	bookRepo := book.NewRepository(pool)
	mux.HandleFunc("GET /books", book.ListHandler(bookRepo))
	mux.HandleFunc("GET /books/{id}", book.GetByIDHandler(bookRepo))
	mux.HandleFunc("POST /books", book.AddHandler(bookRepo))
	mux.HandleFunc("PUT /books/{id}", book.UpdateHandler(bookRepo))
	mux.HandleFunc("DELETE /books/{id}", book.DeleteHandler(bookRepo))

	userRepo := user.NewRepository(pool)
	authSvc := auth.NewService(userRepo, jwtSecret)
	mux.HandleFunc("POST /register", auth.RegisterHandler(authSvc))
	mux.HandleFunc("POST /login", auth.LoginHandler(authSvc))
	// mux.HandleFunc("GET /users/{id}", user.GetByIDHandler(userSvc))

	return server.Logging(server.Recovery(mux))
}
