using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.ToTable("LookupItems");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(l => new { l.Category, l.Code }).IsUnique();
    }
}
