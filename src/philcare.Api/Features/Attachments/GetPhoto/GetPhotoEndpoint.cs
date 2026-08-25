using philcare.Api.Common.Api;
using philcare.Api.Features.Attachments.Domain;

namespace philcare.Api.Features.Attachments.GetPhoto;

/// <summary>
/// Serves stored photos. Registered as one route per category rather than a single
/// <c>{category}</c> route, because the categories do not share an authorization policy:
///
///   beneficiary → "Casework", matching GET /api/beneficiaries. A beneficiary photo is PII and
///                 must not become the way around the gate on the rest of that record.
///   staff, volunteer → authenticated, matching GET /api/staff and GET /api/volunteers.
///
/// A single route would have to pick one policy and would then either leak beneficiary photos to
/// Finance/Viewer or break staff avatars for them.
/// </summary>
public sealed class GetPhotoEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        MapCategory(app, PhotoRules.BeneficiaryCategory).RequireAuthorization("Casework");
        MapCategory(app, PhotoRules.StaffCategory).RequireAuthorization();
        MapCategory(app, PhotoRules.VolunteerCategory).RequireAuthorization();
    }

    private static RouteHandlerBuilder MapCategory(IEndpointRouteBuilder app, string category) =>
        app.MapGet($"/api/photos/{category}/{{fileName}}", (string fileName, PhotoStore store) =>
        {
            var result = store.Open(category, fileName);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            // No caching header: these are access-controlled images, and a shared cache holding a
            // beneficiary photo would outlive the authorization that permitted the fetch.
            return Results.File(result.Value.Stream, result.Value.ContentType);
        })
        .WithName($"GetPhoto_{category}")
        .WithTags("Photos");
}
