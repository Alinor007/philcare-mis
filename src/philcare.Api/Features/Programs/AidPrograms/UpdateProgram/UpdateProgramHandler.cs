using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.AidPrograms.UpdateProgram;

public sealed class UpdateProgramHandler(AppDbContext db)
{
    public async Task<Result<UpdateProgramResponse>> HandleAsync(int id, UpdateProgramRequest request, CancellationToken cancellationToken)
    {
        var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (program is null)
        {
            return Result.Failure<UpdateProgramResponse>(Error.NotFound("Programs.NotFound", "Program not found."));
        }

        program.Name = request.Name;
        program.Category = request.Category;
        program.OwnerDepartment = request.OwnerDepartment;
        program.Status = request.Status;
        program.Notes = request.Notes;
        program.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateProgramResponse(
            program.Id, program.Name, program.Category, program.OwnerDepartment, program.Status, program.Notes, program.IsActive));
    }
}
