using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class OpeningBalanceConfiguration : IEntityTypeConfiguration<OpeningBalance>
{
    public void Configure(EntityTypeBuilder<OpeningBalance> builder)
    {
        builder.ToTable("OpeningBalances");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.FundCode).IsRequired().HasMaxLength(20);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(10);
        builder.Property(o => o.OpeningBalancePhp).HasPrecision(14, 2);
        builder.Property(o => o.Source).HasMaxLength(200);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.Property(o => o.Status).IsRequired().HasMaxLength(50);

        builder.HasIndex(o => new { o.Year, o.FundCode }).IsUnique();

        builder.HasOne(o => o.Fund)
            .WithMany()
            .HasForeignKey(o => o.FundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
