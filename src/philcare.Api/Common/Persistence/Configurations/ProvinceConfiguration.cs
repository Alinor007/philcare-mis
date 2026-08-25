using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.ReferenceData.Geography.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(9);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.RegionCode).IsRequired().HasMaxLength(9);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.RegionCode);

        // FK targets Region.Code, not Region.Id — provinces are looked up and referenced by their
        // real PSGC code everywhere else (frontend, other entities), never by the internal PK.
        builder.HasOne(p => p.Region)
            .WithMany(r => r.Provinces)
            .HasForeignKey(p => p.RegionCode)
            .HasPrincipalKey(r => r.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
