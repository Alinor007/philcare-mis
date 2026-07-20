using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Amount).HasPrecision(14, 2);
        builder.Property(d => d.AdminRate).HasPrecision(5, 4);
        builder.Property(d => d.AmilRate).HasPrecision(5, 4);

        builder.Property(d => d.Currency).IsRequired().HasMaxLength(10);
        builder.Property(d => d.FundType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Reference).HasMaxLength(100);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.Property(d => d.IsVoided).HasDefaultValue(false);

        builder.HasOne(d => d.Donor)
            .WithMany(don => don.Donations)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
