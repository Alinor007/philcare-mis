using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.AidPrograms.UpdateProgram;

public sealed class UpdateProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/programs/{id:int}", async (int id, UpdateProgramRequest request, UpdateProgramHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateProgram")
        .WithTags("Programs")
        .AddEndpointFilter<ValidationFilter<UpdateProgramRequest>>()
        .RequireAuthorization("Program");
    }
}
