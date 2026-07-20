using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.ReferenceData.UpdateLookup;

public sealed class UpdateLookupHandler(AppDbContext db)
{
    public async Task<Result<UpdateLookupResponse>> HandleAsync(int id, UpdateLookupRequest request, CancellationToken cancellationToken)
    {
        var item = await db.LookupItems.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (item is null)
        {
            return Result.Failure<UpdateLookupResponse>(
                Error.NotFound("Lookup.NotFound", "Lookup item not found."));
        }

        item.Label = request.Label;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateLookupResponse(item.Id, item.Category, item.Code, item.Label, item.SortOrder, item.IsActive));
    }
}
