using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Partners.Domain;

namespace philcare.Api.Features.Partners.CreatePartner;

public sealed class CreatePartnerHandler(AppDbContext db)
{
    public async Task<Result<CreatePartnerResponse>> HandleAsync(CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        var duplicateName = await db.Partners.AnyAsync(p => p.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<CreatePartnerResponse>(
                Error.Conflict("Partners.DuplicateName", "A partner with this name already exists."));
        }

        var partner = new Partner
        {
            Name = request.Name,
            PartnerType = request.PartnerType,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            MouReference = request.MouReference,
            MouStartDate = request.MouStartDate,
            MouEndDate = request.MouEndDate,
            AccreditationNotes = request.AccreditationNotes,
            Notes = request.Notes,
            IsActive = true
        };

        db.Partners.Add(partner);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatePartnerResponse(
            partner.Id, partner.Name, partner.PartnerType, partner.ContactPerson, partner.Email, partner.Phone, partner.IsActive));
    }
}
