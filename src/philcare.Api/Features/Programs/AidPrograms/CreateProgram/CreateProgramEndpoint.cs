using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.AidPrograms.CreateProgram;

public sealed class CreateProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/programs", async (CreateProgramRequest request, CreateProgramHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/programs/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateProgram")
        .WithTags("Programs")
        .AddEndpointFilter<ValidationFilter<CreateProgramRequest>>()
        .RequireAuthorization("Program");
    }
}
