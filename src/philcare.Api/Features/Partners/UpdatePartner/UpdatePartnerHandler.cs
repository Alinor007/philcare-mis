using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Partners.UpdatePartner;

public sealed class UpdatePartnerHandler(AppDbContext db)
{
    public async Task<Result<UpdatePartnerResponse>> HandleAsync(int id, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (partner is null)
        {
            return Result.Failure<UpdatePartnerResponse>(Error.NotFound("Partners.NotFound", "Partner not found."));
        }

        var duplicateName = await db.Partners.AnyAsync(p => p.Id != id && p.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<UpdatePartnerResponse>(
                Error.Conflict("Partners.DuplicateName", "A partner with this name already exists."));
        }

        partner.Name = request.Name;
        partner.PartnerType = request.PartnerType;
        partner.ContactPerson = request.ContactPerson;
        partner.Email = request.Email;
        partner.Phone = request.Phone;
        partner.Address = request.Address;
        partner.City = request.City;
        partner.Province = request.Province;
        partner.Region = request.Region;
        partner.MouReference = request.MouReference;
        partner.MouStartDate = request.MouStartDate;
        partner.MouEndDate = request.MouEndDate;
        partner.AccreditationNotes = request.AccreditationNotes;
        partner.Notes = request.Notes;
        partner.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdatePartnerResponse(
            partner.Id, partner.Name, partner.PartnerType, partner.ContactPerson, partner.Email, partner.Phone, partner.IsActive));
    }
}
