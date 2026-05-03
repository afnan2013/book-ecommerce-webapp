using BookEcom.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookEcom.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Explicit FK to IdentityRole<int>. We deliberately didn't put a Role
        // nav on RolePermission (Domain stays free of Identity dependencies),
        // so EF can't infer this relationship by convention — we configure it
        // here from Infrastructure, which can see Identity types.
        // Pin the constraint name. Without it, EF's convention derives the
        // name from the principal table ("asp_net_roles"), which differs from
        // what was generated when a Role nav property still existed. Pinning
        // keeps the production schema stable across this refactor.
        builder.HasOne<IdentityRole<int>>()
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permissions_roles_role_id");
    }
}
