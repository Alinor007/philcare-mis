using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class MeetingDecisionConfiguration : IEntityTypeConfiguration<MeetingDecision>
{
    public void Configure(EntityTypeBuilder<MeetingDecision> builder)
    {
        builder.ToTable("MeetingDecisions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DecisionText).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.ActionPoints).HasMaxLength(2000);
        builder.Property(d => d.DecisionStatus).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.HasOne(d => d.MeetingMinutes)
            .WithMany(mm => mm.Decisions)
            .HasForeignKey(d => d.MeetingMinutesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ResponsiblePerson)
            .WithMany()
            .HasForeignKey(d => d.ResponsiblePersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
