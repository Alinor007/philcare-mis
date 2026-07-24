using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Partners.UpdatePartner;

public sealed class UpdatePartnerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/partners/{id:int}", async (int id, UpdatePartnerRequest request, UpdatePartnerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdatePartner")
        .WithTags("Partners")
        .AddEndpointFilter<ValidationFilter<UpdatePartnerRequest>>()
        .RequireAuthorization("Program");
    }
}
