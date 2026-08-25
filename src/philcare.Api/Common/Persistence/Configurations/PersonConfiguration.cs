using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.People.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        // Table name kept as the pre-promotion "GovernancePeople" — renaming it is the MariaDB
        // 10.4 operation this project cannot perform additively (see BeneficiaryConfiguration for
        // the same reasoning). Only the C# name/namespace changed.
        builder.ToTable("GovernancePeople");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PersonCategory).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.ContactNumber).HasMaxLength(50);

        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CivilStatus).HasMaxLength(50);

        builder.Property(p => p.Barangay).HasMaxLength(100);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Province).HasMaxLength(100);
        builder.Property(p => p.Region).HasMaxLength(50);

        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactNumber).HasMaxLength(50);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);

        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.FullName);
    }
}
