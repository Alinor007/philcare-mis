using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.ReviewDonorKyd;

public sealed record ReviewDonorKydRequest(KydStatus Status);

public sealed record ReviewDonorKydResponse(int Id, string KydStatus);
