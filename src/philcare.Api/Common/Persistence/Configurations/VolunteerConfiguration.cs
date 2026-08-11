using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> builder)
    {
        builder.ToTable("Volunteers");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.FullName).IsRequired().HasMaxLength(200);

        builder.Property(v => v.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.Phone).HasMaxLength(50);
        builder.Property(v => v.Email).HasMaxLength(256);

        builder.Property(v => v.Barangay).HasMaxLength(100);
        builder.Property(v => v.City).HasMaxLength(100);
        builder.Property(v => v.Province).HasMaxLength(100);
        builder.Property(v => v.Region).HasMaxLength(50);

        builder.Property(v => v.Skills).HasMaxLength(500);
        builder.Property(v => v.AvailabilityDays).HasMaxLength(200);
        builder.Property(v => v.Status).IsRequired().HasMaxLength(50);

        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.IsActive).HasDefaultValue(true);
    }
}
