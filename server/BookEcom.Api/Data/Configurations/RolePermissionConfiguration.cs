using BookEcom.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookEcom.Api.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Explicit FK to IdentityRole<int>. We deliberately didn't put a Role
        // nav on RolePermission (Domain stays free of Identity dependencies),
        // so EF can't infer this relationship by convention — we configure it
        // here from Infrastructure, which can see Identity types.
        builder.HasOne<IdentityRole<int>>()
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
