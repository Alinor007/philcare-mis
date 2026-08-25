using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

/// <summary>
/// Beneficiary replaces the retired Beneficiary entity.
///
/// It is deliberately mapped onto the existing <c>Beneficiaries</c> table rather than a new
/// <c>Beneficiaries</c> one: the table already holds live rows, and a new table would mean
/// create-plus-copy-plus-drop, which is neither additive nor safe to get wrong. Renaming the
/// table is not an option either — that is the MariaDB 10.4 operation that broke Sprint 2.
/// The C# name changes; the storage does not.
/// </summary>
public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("Beneficiaries");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FullName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BeneficiaryType).IsRequired().HasMaxLength(50).HasDefaultValue("INDIVIDUAL");

        builder.Property(b => b.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(b => b.Phone).HasMaxLength(50);

        builder.Property(b => b.Barangay).HasMaxLength(100);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.Province).HasMaxLength(100);
        builder.Property(b => b.Region).HasMaxLength(50);
        builder.Property(b => b.Country).HasMaxLength(100);

        builder.Property(b => b.VulnerabilityCategory).HasMaxLength(50);
        builder.Property(b => b.SafeguardingCategory).HasMaxLength(50);
        builder.Property(b => b.Status).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Remarks).HasMaxLength(1000);
        builder.Property(b => b.PhotoUrl).HasMaxLength(500);

        builder.Property(b => b.IsActive).HasDefaultValue(true);

        // Filtered on (BeneficiaryType, Status), ordered by FullName. The old
        // (BeneficiaryType, Status) index goes away with the column it covered.
        builder.HasIndex(b => new { b.BeneficiaryType, b.Status });
        builder.HasIndex(b => b.FullName);
    }
}
