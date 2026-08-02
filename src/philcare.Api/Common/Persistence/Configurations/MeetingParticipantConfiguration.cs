using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> builder)
    {
        builder.ToTable("MeetingParticipants");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.RoleInMeeting).HasMaxLength(100);
        builder.Property(mp => mp.AttendanceStatus).IsRequired().HasMaxLength(50);
        builder.Property(mp => mp.ParticipationMode).HasMaxLength(50);
        builder.Property(mp => mp.Remarks).HasMaxLength(1000);

        builder.HasIndex(mp => new { mp.MeetingId, mp.PersonId }).IsUnique();

        builder.HasOne(mp => mp.Meeting)
            .WithMany(m => m.Participants)
            .HasForeignKey(mp => mp.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mp => mp.Person)
            .WithMany()
            .HasForeignKey(mp => mp.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mp => mp.Assignment)
            .WithMany()
            .HasForeignKey(mp => mp.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
