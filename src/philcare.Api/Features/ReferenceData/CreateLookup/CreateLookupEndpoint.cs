using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.ReferenceData.CreateLookup;

public sealed class CreateLookupEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/lookups", async (CreateLookupRequest request, CreateLookupHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/lookups/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateLookup")
        .WithTags("ReferenceData")
        .AddEndpointFilter<ValidationFilter<CreateLookupRequest>>()
        .RequireAuthorization("Admin");
    }
}
