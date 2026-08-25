using FluentValidation;

namespace philcare.Api.Features.Programs.ProjectDonors.AddProjectDonor;

public sealed class AddProjectDonorValidator : AbstractValidator<AddProjectDonorRequest>
{
    public AddProjectDonorValidator()
    {
        RuleFor(x => x.DonorId).GreaterThan(0);
    }
}
