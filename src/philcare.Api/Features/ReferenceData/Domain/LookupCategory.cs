namespace philcare.Api.Features.ReferenceData.Domain;

public static class LookupCategory
{
    public const string FundType = "fund_type";
    public const string ExpenseCategory = "expense_category";
    public const string ZakatAsnaf = "zakat_asnaf";
    public const string PaymentMethod = "payment_method";
    public const string DonorType = "donor_type";
    public const string ActivityType = "activity_type";
    public const string Region = "region";
    public const string BeneficiaryStatus = "beneficiary_status";
    public const string DistributionType = "distribution_type";
    public const string PartnerType = "partner_type";
    public const string VolunteerStatus = "volunteer_status";
    public const string SponsorshipType = "sponsorship_type";
    public const string SafeguardingCategory = "safeguarding_category";
    public const string IncomeType = "income_type";
    public const string EngagementType = "engagement_type";
    public const string BeneficiaryType = "beneficiary_type";

    // Pre-existing categories that were seeded without a matching constant.
    public const string AgeGroup = "age_group";
    public const string ImplementationStatus = "implementation_status";
    public const string ProgramCategory = "program_category";
    // Distinct from FundType above: that one belongs to actual Finance Fund entities (Zakat,
    // Sadaqah, Waqf...) and has no consumer yet. This one categorizes a Project's funding source
    // (partner contribution, internal fund, zakat, sponsorship, government/LGU support) and is
    // purely informational — reusing FundType's category name would conflate the two.
    public const string ProjectFundType = "project_fund_type";
    public const string OwnerDepartment = "owner_department";
    public const string VulnerabilityCategory = "vulnerability_category";


    // Program & Beneficiary workflow refactor — Sprint 6. AidProgram.Status had no vocabulary
    // at all before this (finding #29 in the pre-refactor audit).
    public const string ProgramStatus = "program_status";

    // Person unification — shared identity for Staff/Volunteers/Members
    public const string CivilStatus = "civil_status";
    public const string MembershipType = "membership_type";
    public const string MembershipStatus = "membership_status";

    // Governance — Sprint 5
    public const string PersonCategory = "person_category";
    public const string PersonStatus = "person_status";
    public const string OrgBodyType = "org_body_type";
    public const string GovernanceRoleCategory = "governance_role_category";
    public const string MeetingType = "meeting_type";
    public const string MeetingMode = "meeting_mode";
    public const string AttendanceStatus = "attendance_status";
    public const string MeetingRole = "meeting_role";
    public const string DecisionStatus = "decision_status";

    // Governance — added with the Excel vocabulary realignment. Both are sourced from the
    // workbook's Lists sheet ("Called By", "Participation Mode Options"); CalledBy also carries
    // "General Manager", which appears in real Meetings_Register rows but not in the dropdown.
    public const string CalledBy = "called_by";
    public const string ParticipationMode = "participation_mode";

    // Human Resources — Sprint 7. Staff reuses OwnerDepartment for its org unit rather than a new
    // "department" category: it is the same vocabulary, already seeded and already mirroring OrgBody.
    public const string EmploymentType = "employment_type";
}
