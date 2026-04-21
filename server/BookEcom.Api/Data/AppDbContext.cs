using BookEcom.Api.Auth;
using BookEcom.Api.Auth.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Permission.Name must be unique — "books.read" can only exist once.
        builder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        // Composite primary keys for the join tables.
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });
    }
}
