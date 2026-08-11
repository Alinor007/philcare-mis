using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ActivityParticipantConfiguration : IEntityTypeConfiguration<ActivityParticipant>
{
    public void Configure(EntityTypeBuilder<ActivityParticipant> builder)
    {
        builder.ToTable("ActivityParticipants");

        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.RoleInActivity).HasMaxLength(100);
        builder.Property(ap => ap.AttendanceStatus).HasMaxLength(50);
        builder.Property(ap => ap.EvidenceLink).HasMaxLength(500);
        builder.Property(ap => ap.Remarks).HasMaxLength(1000);
        builder.Property(ap => ap.IsActive).HasDefaultValue(true);

        // One roster row per staff member per activity; soft-delete means a re-assignment
        // reactivates the existing row rather than inserting a duplicate.
        builder.HasIndex(ap => new { ap.ActivityId, ap.StaffMemberId }).IsUnique();

        builder.HasOne(ap => ap.Activity)
            .WithMany(a => a.ActivityParticipants)
            .HasForeignKey(ap => ap.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ap => ap.StaffMember)
            .WithMany(s => s.ActivityParticipants)
            .HasForeignKey(ap => ap.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
