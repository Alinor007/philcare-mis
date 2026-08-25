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

        builder.Property(v => v.Skills).HasMaxLength(500);
        builder.Property(v => v.AvailabilityDays).HasMaxLength(200);
        builder.Property(v => v.Status).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Notes).HasMaxLength(1000);

        builder.Property(v => v.IsActive).HasDefaultValue(true);

        // At most one volunteer profile per person.
        builder.HasIndex(v => v.PersonId).IsUnique();

        builder.HasOne(v => v.Person)
            .WithOne(p => p.VolunteerProfile)
            .HasForeignKey<Volunteer>(v => v.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
