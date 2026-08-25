using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ProjectDonorConfiguration : IEntityTypeConfiguration<ProjectDonor>
{
    public void Configure(EntityTypeBuilder<ProjectDonor> builder)
    {
        builder.ToTable("ProjectDonors");

        builder.HasKey(pd => pd.Id);

        builder.HasIndex(pd => new { pd.ProjectId, pd.DonorId }).IsUnique();

        builder.HasOne(pd => pd.Project)
            .WithMany(p => p.Donors)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pd => pd.Donor)
            .WithMany()
            .HasForeignKey(pd => pd.DonorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
