using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("StaffMembers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Position).IsRequired().HasMaxLength(200);

        // 200 to match AidProgram.OwnerDepartment / Activity.ResponsibleDepartment, which hold
        // the same vocabulary — not the 50 used for short type-codes.
        builder.Property(s => s.Department).HasMaxLength(200);

        builder.Property(s => s.EmploymentType).IsRequired().HasMaxLength(50);

        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        // Deliberately NOT unique, unlike Partner.Name: partners are organizations, but two staff
        // can legitimately share a name. A unique index here would reject a real hire.
        builder.HasIndex(s => s.FullName);
        builder.HasIndex(s => s.Department);
    }
}
