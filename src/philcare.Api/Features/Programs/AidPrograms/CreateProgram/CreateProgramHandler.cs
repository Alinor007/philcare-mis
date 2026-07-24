using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.AidPrograms.CreateProgram;

public sealed class CreateProgramHandler(AppDbContext db)
{
    public async Task<Result<CreateProgramResponse>> HandleAsync(CreateProgramRequest request, CancellationToken cancellationToken)
    {
        var program = new AidProgram
        {
            Name = request.Name,
            Category = request.Category,
            OwnerDepartment = request.OwnerDepartment,
            Status = "Active",
            Notes = request.Notes,
            IsActive = true
        };

        db.Programs.Add(program);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateProgramResponse(
            program.Id, program.Name, program.Category, program.OwnerDepartment, program.Status, program.Notes, program.IsActive));
    }
}
