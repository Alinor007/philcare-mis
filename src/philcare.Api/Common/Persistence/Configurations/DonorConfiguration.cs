using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.ToTable("Donors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Email).HasMaxLength(256);
        builder.Property(d => d.Phone).HasMaxLength(50);
        builder.Property(d => d.Address).HasMaxLength(500);
        builder.Property(d => d.Country).HasMaxLength(100);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.KydStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.RiskRating)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(d => d.Donations)
            .WithOne(don => don.Donor)
            .HasForeignKey(don => don.DonorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
