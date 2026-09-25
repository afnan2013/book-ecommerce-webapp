package auth

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type registerRequest struct {
	Email    string `json:"email"`
	Password string `json:"password"`
}

func RegisterHandler(svc *AuthService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req registerRequest

		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			log.Printf("user register handler: decoding body: %v", err)
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}

		fields := map[string]string{}
		if req.Email == "" {
			fields["email"] = "is required"
		}
		if len(req.Password) < 8 {
			fields["password"] = "is required"
		}
		if len(fields) > 0 {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{Error: "input validation failed", Fields: fields})
			return
		}

		created, err := svc.Register(r.Context(), req.Email, req.Password)
		if err != nil {
			if errors.Is(err, ErrEmailTaken) {
				server.WriteError(w, http.StatusConflict, ErrEmailTaken.Error())
				return
			}
			log.Printf("user creating : %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		w.Header().Set("Location", fmt.Sprintf("/users/%d", created.ID))
		server.WriteJSON(w, http.StatusCreated, created)
	}
}

func LoginHandler(svc *AuthService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req registerRequest

		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}

		fields := map[string]string{}
		if req.Email == "" {
			fields["email"] = "is required"
		}
		if len(req.Password) < 8 {
			fields["password"] = "must be at least 8 characters"
		}
		if len(fields) > 0 {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{Error: "input validation failed", Fields: fields})
			return
		}

		token, err := svc.Login(r.Context(), req.Email, req.Password)
		if err != nil {
			if errors.Is(err, ErrInvalidCredentials) {
				server.WriteError(w, http.StatusUnauthorized, ErrEmailTaken.Error())
				return
			}
			log.Printf("user login : %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		server.WriteJSON(w, http.StatusOK, map[string]string{"token": token})
	}
}
