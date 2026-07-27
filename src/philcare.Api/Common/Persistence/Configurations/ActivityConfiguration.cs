using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.ActivityCategory).HasMaxLength(100);
        builder.Property(a => a.ActivityType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.TargetGroup).HasMaxLength(200);

        builder.Property(a => a.Barangay).HasMaxLength(100);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.Province).HasMaxLength(100);
        builder.Property(a => a.Region).HasMaxLength(50);

        builder.Property(a => a.Budget).HasPrecision(14, 2);

        builder.Property(a => a.ImplementingPartner).HasMaxLength(200);
        builder.Property(a => a.ResponsibleDepartment).HasMaxLength(200);
        builder.Property(a => a.SdgAlignment).HasMaxLength(100);
        builder.Property(a => a.ImplementationStatus).IsRequired().HasMaxLength(50);
        builder.Property(a => a.SafeguardingRisk).HasMaxLength(100);
        builder.Property(a => a.EvidenceLink).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.Property(a => a.IsActive).HasDefaultValue(true);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ImplementingPartnerRef)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ImplementingPartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
