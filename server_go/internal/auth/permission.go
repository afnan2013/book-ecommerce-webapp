package auth

const (
	PermissionRead = "permission.read"

	PermissionBooksRead   = "books.read"
	PermissionBooksCreate = "books.create"
	PermissionBooksUpdate = "books.update"
	PermissionBooksDelete = "books.delete"

	PermissionUsersRead   = "users.read"
	PermissionUsersCreate = "users.create"
	PermissionUsersUpdate = "users.update"
	PermissionUsersDelete = "users.delete"

	PermissionRolesRead   = "roles.read"
	PermissionRolesCreate = "roles.create"
	PermissionRolesUpdate = "roles.update"
	PermissionRolesDelete = "roles.delete"
)

var PermissionList = map[string]string{
	PermissionRead:        "View the permissions",
	PermissionBooksRead:   "View the book catalog",
	PermissionBooksCreate: "Add a new book",
	PermissionBooksUpdate: "Edit an existing book",
	PermissionBooksDelete: "Remove a book",

	PermissionUsersRead:   "List and view other users",
	PermissionUsersCreate: "Create new user accounts",
	PermissionUsersUpdate: "Edit any user's profile, including role and permission assignments",
	PermissionUsersDelete: "Delete a user",

	PermissionRolesRead:   "View roles and their permissions",
	PermissionRolesCreate: "Create new roles",
	PermissionRolesUpdate: "Edit a role's name or permissions",
	PermissionRolesDelete: "Delete a role",
}
