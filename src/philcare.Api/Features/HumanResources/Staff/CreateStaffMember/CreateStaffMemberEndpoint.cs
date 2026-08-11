using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

public sealed class CreateStaffMemberEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/staff", async (CreateStaffMemberRequest request, CreateStaffMemberHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/staff/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateStaffMember")
        .WithTags("Staff")
        // Admin rather than the "Program" policy used by Partners/Volunteers: employment records
        // are more sensitive than an operational roster. Reads stay open to any authenticated user.
        .AddEndpointFilter<ValidationFilter<CreateStaffMemberRequest>>()
        .RequireAuthorization("Admin");
    }
}
