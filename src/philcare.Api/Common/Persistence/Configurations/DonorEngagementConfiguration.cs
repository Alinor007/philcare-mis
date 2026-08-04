using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DonorEngagementConfiguration : IEntityTypeConfiguration<DonorEngagement>
{
    public void Configure(EntityTypeBuilder<DonorEngagement> builder)
    {
        builder.ToTable("DonorEngagements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EngagementType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.Property(e => e.FollowUpRequired).HasDefaultValue(false);

        builder.HasIndex(e => e.DonorId);

        builder.HasOne(e => e.Donor)
            .WithMany()
            .HasForeignKey(e => e.DonorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
