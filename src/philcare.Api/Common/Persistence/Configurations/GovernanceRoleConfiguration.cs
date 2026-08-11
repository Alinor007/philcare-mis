using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class GovernanceRoleConfiguration : IEntityTypeConfiguration<GovernanceRole>
{
    public void Configure(EntityTypeBuilder<GovernanceRole> builder)
    {
        builder.ToTable("GovernanceRoles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.RoleCategory).IsRequired().HasMaxLength(50);
        builder.Property(r => r.DefaultVotingRights).HasMaxLength(100);
        builder.Property(r => r.CountsForQuorum).HasMaxLength(100);
        builder.Property(r => r.Delegable).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.Property(r => r.IsActive).HasDefaultValue(true);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasOne(r => r.DefaultBody)
            .WithMany()
            .HasForeignKey(r => r.DefaultBodyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
