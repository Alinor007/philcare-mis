using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class AidProgramConfiguration : IEntityTypeConfiguration<AidProgram>
{
    public void Configure(EntityTypeBuilder<AidProgram> builder)
    {
        builder.ToTable("Programs");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(50);
        builder.Property(p => p.OwnerDepartment).HasMaxLength(200);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasMany(p => p.Projects)
            .WithOne(pr => pr.Program)
            .HasForeignKey(pr => pr.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
