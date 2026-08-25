using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.ReferenceData.Geography.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(9);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DesignationName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.IslandGroup).IsRequired().HasMaxLength(20);

        builder.HasIndex(r => r.Code).IsUnique();
    }
}
