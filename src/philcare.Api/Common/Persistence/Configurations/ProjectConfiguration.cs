using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.FundCode).HasMaxLength(20);
        builder.Property(p => p.TotalBudget).HasPrecision(14, 2);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.ProjectManager).HasMaxLength(200);
        builder.Property(p => p.ImplementationStatus).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ApprovalLevel).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.DonorId);
        builder.HasIndex(p => p.FundCode);

        builder.HasOne(p => p.Program)
            .WithMany(pg => pg.Projects)
            .HasForeignKey(p => p.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
