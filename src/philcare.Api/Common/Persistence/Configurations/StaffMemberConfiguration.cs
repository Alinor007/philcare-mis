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

        builder.Property(s => s.Position).IsRequired().HasMaxLength(200);

        // 200 to match AidProgram.OwnerDepartment / Activity.ResponsibleDepartment, which hold
        // the same vocabulary — not the 50 used for short type-codes.
        builder.Property(s => s.Department).HasMaxLength(200);

        builder.Property(s => s.EmploymentType).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => s.Department);

        // At most one staff profile per person.
        builder.HasIndex(s => s.PersonId).IsUnique();

        builder.HasOne(s => s.Person)
            .WithOne(p => p.StaffProfile)
            .HasForeignKey<StaffMember>(s => s.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Not unique, not one-to-one: many staff can report to the same supervisor.
        builder.HasOne(s => s.SupervisorPerson)
            .WithMany()
            .HasForeignKey(s => s.SupervisorPersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
