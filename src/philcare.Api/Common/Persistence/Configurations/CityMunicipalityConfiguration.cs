using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.ReferenceData.Geography.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class CityMunicipalityConfiguration : IEntityTypeConfiguration<CityMunicipality>
{
    public void Configure(EntityTypeBuilder<CityMunicipality> builder)
    {
        builder.ToTable("CitiesMunicipalities");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(9);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.ProvinceCode).HasMaxLength(9);
        builder.Property(c => c.RegionCode).IsRequired().HasMaxLength(9);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.ProvinceCode);
        builder.HasIndex(c => c.RegionCode);

        builder.HasOne(c => c.Region)
            .WithMany(r => r.CitiesMunicipalities)
            .HasForeignKey(c => c.RegionCode)
            .HasPrincipalKey(r => r.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional — NCR and a few independent cities have no province (see CityMunicipality doc).
        builder.HasOne(c => c.Province)
            .WithMany(p => p.CitiesMunicipalities)
            .HasForeignKey(c => c.ProvinceCode)
            .HasPrincipalKey(p => p.Code)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
