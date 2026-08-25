using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.People.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MembershipNumber).IsRequired().HasMaxLength(50);
        builder.Property(m => m.MembershipType).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Status).IsRequired().HasMaxLength(50);
        builder.Property(m => m.ReferredBy).HasMaxLength(200);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.Property(m => m.IsActive).HasDefaultValue(true);

        // The membership number is the org's own identifier — unique across the roll, unlike
        // PersonId which is deliberately NOT unique here (see Membership: renewals are new rows).
        builder.HasIndex(m => m.MembershipNumber).IsUnique();
        builder.HasIndex(m => m.PersonId);

        builder.HasOne(m => m.Person)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
