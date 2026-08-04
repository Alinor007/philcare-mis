using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class OrgBodyConfiguration : IEntityTypeConfiguration<OrgBody>
{
    public void Configure(EntityTypeBuilder<OrgBody> builder)
    {
        builder.ToTable("OrgBodies");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BodyType).IsRequired().HasMaxLength(50);
        builder.Property(b => b.QuorumRule).HasMaxLength(200);
        builder.Property(b => b.DecisionThreshold).HasMaxLength(200);
        builder.Property(b => b.MeetingFrequency).HasMaxLength(100);
        builder.Property(b => b.PolicyBasis).HasMaxLength(200);
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.Property(b => b.IsActive).HasDefaultValue(true);

        builder.HasIndex(b => b.Name).IsUnique();

        builder.HasOne(b => b.ParentBody)
            .WithMany(b => b.ChildBodies)
            .HasForeignKey(b => b.ParentBodyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
