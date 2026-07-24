using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Volunteers.ActivityVolunteers.AddActivityVolunteer;

public sealed class AddActivityVolunteerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/activities/{activityId:int}/volunteers", async (
            int activityId, AddActivityVolunteerRequest request, AddActivityVolunteerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(activityId, request, ct);
            return result.IsSuccess ? Results.Created($"/api/activities/{activityId}/volunteers", result.Value) : result.ToProblem();
        })
        .WithName("AddActivityVolunteer")
        .WithTags("ActivityVolunteers")
        .AddEndpointFilter<ValidationFilter<AddActivityVolunteerRequest>>()
        .RequireAuthorization("Program");
    }
}
