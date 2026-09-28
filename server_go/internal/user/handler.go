package user

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/authctx"
	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
	"github.com/google/uuid"
)

type registerRequest struct {
	Email    string `json:"email"`
	Password string `json:"password"`
	FullName string `json:"fullName"`
	UserType int16  `json:"userType"`
}

type updateProfileRequest struct {
	FullName         string  `json:"fullName"`
	PhoneNumber      *string `json:"phoneNumber"`
	Address          *string `json:"address"`
	ConcurrencyStamp string  `json:"concurrencyStamp"`
}

func ListHandler(svc *UserService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		users, err := svc.List(r.Context())
		if err != nil {
			log.Printf("list users: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}
		server.WriteJSON(w, http.StatusOK, users)
	}
}

func GetByIDHandler(svc *UserService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid user id")
			return
		}

		b, ok, err := svc.GetByID(r.Context(), id)
		if err != nil {
			log.Printf("getting user by id: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		if !ok {
			server.WriteError(w, http.StatusNotFound, "user not found")
			return
		}

		server.WriteJSON(w, http.StatusOK, b)
	}
}

func MeHandler(svc *UserService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := authctx.UserID(r.Context())
		if !ok {
			server.WriteError(w, http.StatusUnauthorized, "invalid or missing authorization header")
			return
		}

		detail, ok, err := svc.GetByID(r.Context(), userID)
		if err != nil {
			log.Printf("getting current user: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}
		if !ok {
			server.WriteError(w, http.StatusNotFound, "user not found")
			return
		}

		server.WriteJSON(w, http.StatusOK, detail)
	}
}

func UpdateMeHandler(svc *UserService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := authctx.UserID(r.Context())
		if !ok {
			server.WriteError(w, http.StatusUnauthorized, "invalid or missing authorization header")
			return
		}

		var req updateProfileRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}
		if req.FullName == "" {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{
				Error:  "input validation failed",
				Fields: map[string]string{"fullName": "is required"},
			})
			return
		}

		updated, err := svc.UpdateProfile(r.Context(), userID, req.FullName, req.PhoneNumber, req.Address, req.ConcurrencyStamp)
		if err != nil {
			switch {
			case errors.Is(err, ErrNotFound):
				server.WriteError(w, http.StatusNotFound, ErrNotFound.Error())
			case errors.Is(err, ErrConflict):
				server.WriteError(w, http.StatusConflict, ErrConflict.Error())
			default:
				log.Printf("update current user: %v", err)
				server.WriteError(w, http.StatusInternalServerError, "internal server error")
			}
			return
		}

		server.WriteJSON(w, http.StatusOK, updated)
	}
}

func CreateHandler(svc *UserService) http.HandlerFunc {
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
			fields["password"] = "must be at least 8 characters"
		}
		if req.FullName == "" {
			fields["full_name"] = "is required"
		}
		if req.UserType < UserTypeEmployee || req.UserType > UserTypeBuyer {
			fields["user_type"] = "must be a valid user type"
		}
		if len(fields) > 0 {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{Error: "input validation failed", Fields: fields})
			return
		}

		created, err := svc.CreateUser(r.Context(), req.Email, req.Password, req.FullName, req.UserType)
		if err != nil {
			if errors.Is(err, ErrEmailTaken) {
				server.WriteError(w, http.StatusConflict, "email already registered")
				return
			}
			log.Printf("user creating : %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}

		w.Header().Set("Location", fmt.Sprintf("/users/%s", created.ID))
		server.WriteJSON(w, http.StatusCreated, created)
	}
}

func DeleteHandler(svc *UserService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userId, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid user id")
			return
		}

		_, err = svc.DeleteHandler(r.Context(), userId.String())
		if err != nil {
			switch {
			case errors.Is(err, ErrNotFound):
				server.WriteError(w, http.StatusNotFound, ErrNotFound.Error())
			default:
				log.Printf("delete user by ID: %v", err)
				server.WriteError(w, http.StatusInternalServerError, "internal server error")
			}
			return
		}
		w.WriteHeader(http.StatusNoContent)

	}
}
