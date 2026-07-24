using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Partners.CreatePartner;

public sealed class CreatePartnerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/partners", async (CreatePartnerRequest request, CreatePartnerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/partners/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreatePartner")
        .WithTags("Partners")
        .AddEndpointFilter<ValidationFilter<CreatePartnerRequest>>()
        .RequireAuthorization("Program");
    }
}
