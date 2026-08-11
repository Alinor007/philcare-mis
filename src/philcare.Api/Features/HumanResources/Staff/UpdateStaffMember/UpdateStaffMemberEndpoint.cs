using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

public sealed class UpdateStaffMemberEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/staff/{id:int}", async (
            int id, UpdateStaffMemberRequest request, UpdateStaffMemberHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateStaffMember")
        .WithTags("Staff")
        .AddEndpointFilter<ValidationFilter<UpdateStaffMemberRequest>>()
        .RequireAuthorization("Admin");
    }
}
