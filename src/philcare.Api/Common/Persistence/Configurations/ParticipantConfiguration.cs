using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ParticipantType).IsRequired().HasMaxLength(50);

        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.AgeGroup).HasMaxLength(50);
        builder.Property(p => p.Phone).HasMaxLength(50);

        builder.Property(p => p.Barangay).HasMaxLength(100);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Province).HasMaxLength(100);
        builder.Property(p => p.Region).HasMaxLength(50);
        builder.Property(p => p.Country).HasMaxLength(100);

        builder.Property(p => p.VulnerabilityCategory).HasMaxLength(50);
        builder.Property(p => p.SafeguardingCategory).HasMaxLength(50);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Remarks).HasMaxLength(1000);

        builder.Property(p => p.IsActive).HasDefaultValue(true);
    }
}
