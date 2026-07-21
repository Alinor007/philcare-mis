using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("Funds");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Code).IsRequired().HasMaxLength(20);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.PolicyNotes).HasMaxLength(1000);
        builder.Property(f => f.UseCase).HasMaxLength(500);

        builder.HasIndex(f => f.Code).IsUnique();

        builder.HasMany(f => f.Buckets)
            .WithOne(b => b.Fund)
            .HasForeignKey(b => b.FundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
