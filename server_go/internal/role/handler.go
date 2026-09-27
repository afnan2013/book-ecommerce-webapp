package role

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"

	"github.com/google/uuid"

	"github.com/afnan2013/book-ecommerce-webapp/server_go/internal/server"
)

type createRequest struct {
	Name string `json:"name"`
}

type updateRequest struct {
	Name             string `json:"name"`
	ConcurrencyStamp string `json:"concurrencyStamp"`
}

type setPermissionsRequest struct {
	PermissionIDs    []string `json:"permissionIds"`
	ConcurrencyStamp string   `json:"concurrencyStamp"`
}

func writeServiceError(w http.ResponseWriter, action string, err error) {
	switch {
	case errors.Is(err, ErrNotFound):
		server.WriteError(w, http.StatusNotFound, ErrNotFound.Error())
	case errors.Is(err, ErrSystemRole):
		server.WriteError(w, http.StatusForbidden, ErrSystemRole.Error())
	case errors.Is(err, ErrConflict):
		server.WriteError(w, http.StatusConflict, ErrConflict.Error())
	case errors.Is(err, ErrNameTaken):
		server.WriteError(w, http.StatusConflict, ErrNameTaken.Error())
	default:
		log.Printf("%s: %v", action, err)
		server.WriteError(w, http.StatusInternalServerError, "internal server error")
	}
}

func ListHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		roles, err := svc.List(r.Context())
		if err != nil {
			log.Printf("list roles: %v", err)
			server.WriteError(w, http.StatusInternalServerError, "internal server error")
			return
		}
		server.WriteJSON(w, http.StatusOK, roles)
	}
}

func GetByIDHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid role id")
			return
		}

		role, err := svc.GetByID(r.Context(), id)
		if err != nil {
			writeServiceError(w, "get role", err)
			return
		}
		server.WriteJSON(w, http.StatusOK, role)
	}
}

func CreateHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		var req createRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}
		if req.Name == "" {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{
				Error:  "input validation failed",
				Fields: map[string]string{"name": "is required"},
			})
			return
		}

		created, err := svc.Create(r.Context(), req.Name)
		if err != nil {
			writeServiceError(w, "create role", err)
			return
		}

		w.Header().Set("Location", fmt.Sprintf("/api/roles/%s", created.ID))
		server.WriteJSON(w, http.StatusCreated, created)
	}
}

func UpdateHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid role id")
			return
		}

		var req updateRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}
		if req.Name == "" {
			server.WriteJSON(w, http.StatusBadRequest, server.ErrorResponse{
				Error:  "input validation failed",
				Fields: map[string]string{"name": "is required"},
			})
			return
		}

		if _, err := svc.Update(r.Context(), id, req.Name, req.ConcurrencyStamp); err != nil {
			writeServiceError(w, "update role", err)
			return
		}

		w.WriteHeader(http.StatusNoContent)
	}
}

func DeleteHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid role id")
			return
		}

		if err := svc.Delete(r.Context(), id); err != nil {
			writeServiceError(w, "delete role", err)
			return
		}

		w.WriteHeader(http.StatusNoContent)
	}
}

func SetPermissionsHandler(svc *RoleService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		id, err := uuid.Parse(r.PathValue("id"))
		if err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid role id")
			return
		}

		var req setPermissionsRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			server.WriteError(w, http.StatusBadRequest, "invalid json data")
			return
		}

		permissionIDs := make([]uuid.UUID, len(req.PermissionIDs))
		for i, raw := range req.PermissionIDs {
			pid, err := uuid.Parse(raw)
			if err != nil {
				server.WriteError(w, http.StatusBadRequest, "invalid permission id")
				return
			}
			permissionIDs[i] = pid
		}

		updated, err := svc.SetPermissions(r.Context(), id, permissionIDs, req.ConcurrencyStamp)
		if err != nil {
			writeServiceError(w, "set role permissions", err)
			return
		}

		server.WriteJSON(w, http.StatusOK, updated)
	}
}
