using BookEcom.Application.Auth;
using BookEcom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookEcom.Infrastructure.Data.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.HasKey(up => new { up.UserId, up.PermissionId });

        // Explicit FK to AppUser. We deliberately didn't put a User nav on
        // UserPermission (Domain stays free of Identity dependencies), so
        // EF can't infer this relationship by convention — we configure it
        // here from Infrastructure, which can see Identity types.
        // Pin the constraint name — see comment in RolePermissionConfiguration.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_permissions_users_user_id");
    }
}
