using philcare.Api.Common.Api;

namespace philcare.Api.Features.Attachments.UploadPhoto;

public sealed class UploadPhotoEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/photos/{category}", async (
            string category,
            IFormFile? file,
            UploadPhotoHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(category, file, ct);

            return result.IsSuccess
                ? Results.Created(result.Value.Url, result.Value)
                : result.ToProblem();
        })
        .WithName("UploadPhoto")
        .WithTags("Photos")
        .DisableAntiforgery()
        // "Program" (Program + Admin) — the roles that maintain the records these photos hang off.
        // Reads are gated separately and more tightly, per category, in GetPhotoEndpoint.
        .RequireAuthorization("Program");
    }
}
