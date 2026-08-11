using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class MeetingMinutesConfiguration : IEntityTypeConfiguration<MeetingMinutes>
{
    public void Configure(EntityTypeBuilder<MeetingMinutes> builder)
    {
        builder.ToTable("MeetingMinutes");

        builder.HasKey(mm => mm.Id);

        builder.Property(mm => mm.Summary).HasMaxLength(2000);
        builder.Property(mm => mm.DocumentLink).HasMaxLength(500);

        builder.Property(mm => mm.PublicationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Exactly one minutes record per meeting.
        builder.HasIndex(mm => mm.MeetingId).IsUnique();

        builder.HasOne(mm => mm.Meeting)
            .WithOne(m => m.Minutes)
            .HasForeignKey<MeetingMinutes>(mm => mm.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mm => mm.PreparedByPerson)
            .WithMany()
            .HasForeignKey(mm => mm.PreparedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mm => mm.ApprovedByPerson)
            .WithMany()
            .HasForeignKey(mm => mm.ApprovedByPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
