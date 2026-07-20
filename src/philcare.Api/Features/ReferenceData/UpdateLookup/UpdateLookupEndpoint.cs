using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.ReferenceData.UpdateLookup;

public sealed class UpdateLookupEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/lookups/{id:int}", async (int id, UpdateLookupRequest request, UpdateLookupHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateLookup")
        .WithTags("ReferenceData")
        .AddEndpointFilter<ValidationFilter<UpdateLookupRequest>>()
        .RequireAuthorization("Admin");
    }
}
