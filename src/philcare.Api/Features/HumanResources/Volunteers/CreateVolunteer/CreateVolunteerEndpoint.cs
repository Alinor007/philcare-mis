using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;

public sealed class CreateVolunteerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/volunteers", async (CreateVolunteerRequest request, CreateVolunteerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/volunteers/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateVolunteer")
        .WithTags("Volunteers")
        .AddEndpointFilter<ValidationFilter<CreateVolunteerRequest>>()
        .RequireAuthorization("Program");
    }
}
