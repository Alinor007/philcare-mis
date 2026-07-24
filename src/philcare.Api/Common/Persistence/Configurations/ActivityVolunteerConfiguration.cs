using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Volunteers.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ActivityVolunteerConfiguration : IEntityTypeConfiguration<ActivityVolunteer>
{
    public void Configure(EntityTypeBuilder<ActivityVolunteer> builder)
    {
        builder.ToTable("ActivityVolunteers");

        builder.HasKey(av => av.Id);

        builder.Property(av => av.RoleInActivity).HasMaxLength(100);
        builder.Property(av => av.AttendanceStatus).HasMaxLength(50);
        builder.Property(av => av.HoursServed).HasPrecision(5, 1);
        builder.Property(av => av.Remarks).HasMaxLength(1000);

        builder.HasIndex(av => new { av.ActivityId, av.VolunteerId }).IsUnique();

        builder.HasOne(av => av.Activity)
            .WithMany(a => a.ActivityVolunteers)
            .HasForeignKey(av => av.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(av => av.Volunteer)
            .WithMany(v => v.ActivityVolunteers)
            .HasForeignKey(av => av.VolunteerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
