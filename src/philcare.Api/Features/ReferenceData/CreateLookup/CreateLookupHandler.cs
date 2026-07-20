using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.ReferenceData.CreateLookup;

public sealed class CreateLookupHandler(AppDbContext db)
{
    public async Task<Result<CreateLookupResponse>> HandleAsync(CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var exists = await db.LookupItems
            .AnyAsync(l => l.Category == request.Category && l.Code == request.Code, cancellationToken);

        if (exists)
        {
            return Result.Failure<CreateLookupResponse>(
                Error.Conflict("Lookup.AlreadyExists", "A lookup item with this category and code already exists."));
        }

        var item = new LookupItem
        {
            Category = request.Category,
            Code = request.Code,
            Label = request.Label,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        db.LookupItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateLookupResponse(item.Id, item.Category, item.Code, item.Label, item.SortOrder, item.IsActive));
    }
}
