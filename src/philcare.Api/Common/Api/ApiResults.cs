using Microsoft.AspNetCore.Http.HttpResults;
using philcare.Api.Common.Domain;

namespace philcare.Api.Common.Api;

public static class ApiResults
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");

        var error = result.Error;
        var statusCode = MapStatusCode(error.Type);

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Locked => StatusCodes.Status423Locked,
        _ => StatusCodes.Status500InternalServerError
    };
}
