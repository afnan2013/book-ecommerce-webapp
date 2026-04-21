namespace BookEcom.Api.Auth;

/// <summary>
/// Canonical names for every permission in the system. Use these constants
/// anywhere a permission name is referenced — never hardcode the string.
/// </summary>
public static class PermissionNames
{
    // Books
    public const string BooksRead        = "books.read";
    public const string BooksCreate      = "books.create";
    public const string BooksUpdate      = "books.update";
    public const string BooksDelete      = "books.delete";

    // Users — admin operations on OTHER users. Self operations (GET / PATCH
    // /api/users/me) need no permission — just [Authorize]. The /me endpoints
    // take no {id} param, so there's no target to gate beyond authentication.
    public const string UsersRead   = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    // Roles
    public const string RolesRead   = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";

    public static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>
        {
            [BooksRead]   = "View the book catalog",
            [BooksCreate] = "Add a new book",
            [BooksUpdate] = "Edit an existing book",
            [BooksDelete] = "Remove a book",

            [UsersRead]   = "List and view other users",
            [UsersCreate] = "Create new user accounts",
            [UsersUpdate] = "Edit any user's profile, including role and permission assignments",
            [UsersDelete] = "Delete a user (never yourself — enforced in the controller)",

            [RolesRead]   = "View roles and their permissions",
            [RolesCreate] = "Create new roles",
            [RolesUpdate] = "Edit a role's name or permissions (SuperAdmin is immutable)",
            [RolesDelete] = "Delete a role (SuperAdmin cannot be deleted)",
        };

    public static IReadOnlyCollection<string> All => Descriptions.Keys.ToList();
}
