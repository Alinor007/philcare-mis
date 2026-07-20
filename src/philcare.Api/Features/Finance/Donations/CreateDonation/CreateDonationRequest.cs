namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed record CreateDonationRequest(
    int DonorId,
    decimal Amount,
    string Currency,
    string FundType,
    DateTime ReceivedDate,
    string PaymentMethod,
    bool AdminAllowed,
    decimal AdminRate,
    decimal AmilRate,
    string? Reference,
    string? Notes);

public sealed record AllocationResponse(decimal ProgramAmount, decimal AdminAmount, decimal AmilAmount);

public sealed record CreateDonationResponse(
    int Id,
    int DonorId,
    decimal Amount,
    string Currency,
    string FundType,
    DateTime ReceivedDate,
    string PaymentMethod,
    bool AdminAllowed,
    decimal AdminRate,
    decimal AmilRate,
    string? Reference,
    string? Notes,
    bool IsVoided,
    AllocationResponse Allocation);
