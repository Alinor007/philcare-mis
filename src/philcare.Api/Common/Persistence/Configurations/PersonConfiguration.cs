using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("GovernancePeople");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PersonCategory).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.ContactNumber).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.VolunteerId);
    }
}
