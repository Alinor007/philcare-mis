namespace philcare.Api.Common.Domain;

/// <summary>
/// Shared by Person, Beneficiary and Volunteer. Lived inside Features/Programs/Domain (and before
/// that, the old Beneficiary.cs) until Person unification made it a cross-module concept — no
/// single feature owns "gender" any more than it owns "full name".
/// </summary>
public enum Gender
{
    Male,
    Female,
    Unspecified
}
