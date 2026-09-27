package main

import (
	"net/http"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/auth"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/book"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/user"
)

func newRouter(pool server.DBQuerier, jwtSecret string, allowedOrigin string) http.Handler {
	mux := http.NewServeMux()

	requireAuth := auth.RequireAuth(jwtSecret)

	bookRepo := book.NewRepository(pool)
	mux.Handle("GET /api/books", server.Chain(book.ListHandler(bookRepo), requireAuth, auth.RequirePermission(auth.PermissionBooksRead)))
	mux.Handle("GET /api/books/{id}", requireAuth(book.GetByIDHandler(bookRepo)))
	mux.Handle("POST /api/books", requireAuth(book.AddHandler(bookRepo)))
	mux.Handle("PUT /api/books/{id}", requireAuth(book.UpdateHandler(bookRepo)))
	mux.Handle("DELETE /api/books/{id}", requireAuth(book.DeleteHandler(bookRepo)))

	userRepo := user.NewRepository(pool)
	userSvc := user.NewService(userRepo)
	authSvc := auth.NewService(userRepo, jwtSecret)
	mux.HandleFunc("POST /api/auth/register", auth.RegisterHandler(authSvc))
	mux.HandleFunc("POST /api/auth/login", auth.LoginHandler(authSvc))
	// mux.HandleFunc("GET /users/{id}", user.GetByIDHandler(userSvc))

	mux.Handle("GET /api/users", server.Chain(user.ListHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersRead)))

	return server.CORS(allowedOrigin)(server.Logging(server.Recovery(mux)))
}
