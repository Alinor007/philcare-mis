using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PositionTitle).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => new { a.PersonId, a.OrgBodyId, a.Status });

        // At most one primary Current assignment per person — NULLs don't collide in a unique
        // index, so only one IsPrimaryCurrent = true row can exist per PersonId. Same MariaDB
        // 10.4 filtered-unique-index substitute as ZakatEligibility.IsLiveApproval.
        builder.HasIndex(a => new { a.PersonId, a.IsPrimaryCurrent }).IsUnique();

        builder.HasOne(a => a.Person)
            .WithMany(p => p.Assignments)
            .HasForeignKey(a => a.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.OrgBody)
            .WithMany(b => b.Assignments)
            .HasForeignKey(a => a.OrgBodyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.GovernanceRole)
            .WithMany(r => r.Assignments)
            .HasForeignKey(a => a.GovernanceRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
