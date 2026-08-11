using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MeetingType).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Mode).IsRequired().HasMaxLength(50);
        builder.Property(m => m.CalledBy).HasMaxLength(100);
        builder.Property(m => m.QuorumRequired).HasMaxLength(200);
        builder.Property(m => m.DecisionThreshold).HasMaxLength(200);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(m => new { m.OrgBodyId, m.MeetingDate });

        builder.HasOne(m => m.OrgBody)
            .WithMany(b => b.Meetings)
            .HasForeignKey(m => m.OrgBodyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Two independent FKs to Person with no inverse navigation — WithMany() with no
        // lambda avoids ambiguity between Chair and Secretary.
        builder.HasOne(m => m.ChairPerson)
            .WithMany()
            .HasForeignKey(m => m.ChairPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.SecretaryPerson)
            .WithMany()
            .HasForeignKey(m => m.SecretaryPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
