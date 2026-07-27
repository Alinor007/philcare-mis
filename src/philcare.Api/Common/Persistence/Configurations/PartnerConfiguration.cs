using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Partners.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PartnerType).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ContactPerson).HasMaxLength(200);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Province).HasMaxLength(100);
        builder.Property(p => p.Region).HasMaxLength(50);
        builder.Property(p => p.MouReference).HasMaxLength(100);
        builder.Property(p => p.AccreditationNotes).HasMaxLength(1000);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}
