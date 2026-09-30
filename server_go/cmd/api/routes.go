package main

import (
	"net/http"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/auth"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/book"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/role"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/user"
)

func newRouter(pool server.DBQuerier, jwtSecret string, allowedOrigin string) http.Handler {
	mux := http.NewServeMux()

	// Kept shallow on purpose: a DB check here would fail every task at once during a DB blip and turn it into a restart storm.
	mux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) {
		server.WriteJSON(w, http.StatusOK, map[string]string{"status": "ok"})
	})

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
	mux.Handle("GET /api/permissions", server.Chain(auth.ListPermissionHandler(authSvc), requireAuth, auth.RequirePermission(auth.PermissionRead)))
	// mux.HandleFunc("GET /users/{id}", user.GetByIDHandler(userSvc))

	mux.Handle("GET /api/users", server.Chain(user.ListHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersRead)))
	mux.Handle("GET /api/users/me", server.Chain(user.MeHandler(userSvc), requireAuth))
	mux.Handle("PUT /api/users/me", server.Chain(user.UpdateMeHandler(userSvc), requireAuth))
	mux.Handle("POST /api/users", server.Chain(user.CreateHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersCreate)))
	mux.Handle("GET /api/users/{id}", server.Chain(user.GetByIDHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersRead)))
	mux.Handle("PUT /api/users/{id}", server.Chain(user.GetByIDHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersUpdate)))
	mux.Handle("DELETE /api/users/{id}", server.Chain(user.DeleteHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersDelete)))
	mux.Handle("PUT /api/users/{id}/roles", server.Chain(user.SetRolesHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersUpdate)))
	mux.Handle("PUT /api/users/{id}/permissions", server.Chain(user.SetPermissionsHandler(userSvc), requireAuth, auth.RequirePermission(auth.PermissionUsersUpdate)))

	roleRepo := role.NewRepository(pool)
	roleSvc := role.NewService(roleRepo)
	mux.Handle("GET /api/roles", server.Chain(role.ListHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesRead)))
	mux.Handle("GET /api/roles/{id}", server.Chain(role.GetByIDHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesRead)))
	mux.Handle("POST /api/roles", server.Chain(role.CreateHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesCreate)))
	mux.Handle("PUT /api/roles/{id}", server.Chain(role.UpdateHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesUpdate)))
	mux.Handle("DELETE /api/roles/{id}", server.Chain(role.DeleteHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesDelete)))
	mux.Handle("PUT /api/roles/{id}/permissions", server.Chain(role.SetPermissionsHandler(roleSvc), requireAuth, auth.RequirePermission(auth.PermissionRolesUpdate)))

	return server.CORS(allowedOrigin)(server.Logging(server.Recovery(mux)))
}
