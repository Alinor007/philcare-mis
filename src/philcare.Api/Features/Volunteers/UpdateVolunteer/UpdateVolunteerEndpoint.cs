using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Volunteers.UpdateVolunteer;

public sealed class UpdateVolunteerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/volunteers/{id:int}", async (int id, UpdateVolunteerRequest request, UpdateVolunteerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateVolunteer")
        .WithTags("Volunteers")
        .AddEndpointFilter<ValidationFilter<UpdateVolunteerRequest>>()
        .RequireAuthorization("Program");
    }
}
