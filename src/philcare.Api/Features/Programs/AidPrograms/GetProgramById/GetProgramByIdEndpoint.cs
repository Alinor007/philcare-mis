using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.AidPrograms.GetProgramById;

public sealed record ProgramDetailResponse(
    int Id, string Name, string Category, string? OwnerDepartment, string Status, string? Notes, bool IsActive, int ProjectCount);

public sealed class GetProgramByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/programs/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var program = await db.Programs
                .Where(p => p.Id == id)
                .Select(p => new ProgramDetailResponse(
                    p.Id, p.Name, p.Category, p.OwnerDepartment, p.Status, p.Notes, p.IsActive, p.Projects.Count))
                .FirstOrDefaultAsync(ct);

            if (program is null)
            {
                return Results.Problem(title: "Programs.NotFound", detail: "Program not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(program);
        })
        .WithName("GetProgramById")
        .WithTags("Programs")
        .RequireAuthorization();
    }
}
