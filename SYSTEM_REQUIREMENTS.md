# PhilCare MIS — System Requirements

**Status**: living document, generated from the actual codebase on branch `sprint-5/feature/governance`, not from a prior spec. Re-verify against `git log` before trusting this after a gap in sessions — this system has been built across multiple parallel sessions and moves fast.

This document is the **specification**: what the system is, who uses it, what it enforces, and why. For **step-by-step build instructions** for a frontend against this API, see [`FRONTEND_PROMPTS.md`](FRONTEND_PROMPTS.md) — that file is the executable playbook; this one is the reference you'd hand to a new engineer or auditor.

---

## 1. What PhilCare MIS is

A management information system for **PhilCare**, a Philippines-based Islamic charity/NGO. It replaces spreadsheet-based tracking of donors, Zakat/Sadaqah/general funds, aid programs, and beneficiaries with a single system of record that enforces the organization's actual financial and safeguarding policies at the API level — not just documents them.

**Governance is in scope as of Sprint 5.** Earlier revisions of this document stated governance was permanently excluded; that decision was reversed by the org. §3.5 describes what's built: org-body hierarchy, people, role assignments, meetings, minutes, and decisions. Two workbook entities — External Engagements and Media Outputs — remain deliberately deferred (see §6).

**Language**: English only. The org's real source data included bilingual (English/Arabic) labels; Arabic-language text was stripped throughout. The underlying **Islamic finance domain logic is preserved in full** — Zakat fund segregation, the 12.5% Amil (zakat collector) share cap, the 8 Zakat asnaf categories, Sharia-restricted fund tracking. That logic is a business requirement, not a language choice.

---

## 2. Users & roles

Five roles, each a `UserRole` string on the account (`Admin`, `Finance`, `Program`, `ZakatDonations`, `Viewer`):

| Role | Can do |
|---|---|
| **Admin** | Everything. Only role that can: void money-movement records (donations, other income, expenses, distributions), deactivate entities (programs, partners, volunteers, donors are edited not deleted), manage users, manage lookups, decide (approve/reject) Zakat eligibility cases, and write anything in Governance (people, org bodies, roles, assignments, meetings, minutes, decisions). |
| **Finance** | Read everything except beneficiary/Zakat PII (see §3.3). Write: donors, donations, other income, expenses. |
| **Program** | Read everything, including beneficiary/Zakat PII. Write: programs, projects, activities, participants, activity enrollment, distributions, partners, volunteers, sponsorships, Zakat eligibility case creation/submission (not the approval decision — that's Admin-only). Recording a Distribution also writes a Finance Expense in the same call — a deliberate, accepted grant so field staff can record aid without a Finance-role handoff (see §3.3); voiding either side stays Admin-only. |
| **ZakatDonations** | The Zakat & Donations Collection Department. Write: donors, donor engagements, donations, other income, Zakat eligibility case creation/submission. Read: the participant registry (a case is raised against a beneficiary, so assessing one means looking the person up). **Cannot** record expenses, create or edit beneficiaries, record distributions, void anything, approve Zakat cases, or touch Governance. |
| **Viewer** | Read everything except beneficiary/Zakat PII. No writes. |

`ZakatDonations` exists because no combination of the original four fitted: `Finance` covers the money-in side but is refused on every Zakat endpoint, while `Program` covers Zakat casework but not donors or donations, and granting the department `Admin` would have handed it user management, Governance and every void. Its two exclusions are deliberate controls rather than oversights — **expenses** stay with Finance so a collections department cannot disburse, and the **approval decision** stays with Admin so the department that assesses a Zakat case is never the one that approves it.

Governance writes are **Admin-only** — reusing the existing policy rather than adding a dedicated governance role, since board rosters, meeting minutes, and voting records are sensitive enough to warrant the same gate as void/decide operations.

Authorization is enforced with five ASP.NET policies applied per-endpoint — never inferred client-side only:

| Policy | Roles | Guards |
|---|---|---|
| `"Admin"` | Admin | voids, deactivations, users, lookups, Zakat decisions, all Governance writes |
| `"Finance"` | Finance ∨ Admin | expenses — money **out** |
| `"Income"` | Finance ∨ Admin ∨ ZakatDonations | donors, donor engagements, donations, other income — money **in** |
| `"Program"` | Program ∨ Admin | program delivery writes, incl. creating/editing beneficiaries |
| `"Casework"` | Program ∨ Admin ∨ ZakatDonations | Zakat eligibility cases + **read** access to the participant registry |

Reads otherwise require any authenticated role; there is no field-level or row-level restriction yet (see §8, Known Gaps). Adding `ZakatDonations` needed **no migration** — `User.Role` is persisted via `HasConversion<string>()` into an existing `varchar(20)`.

**Route scoping (UI only).** Because most reads are open to any authenticated role, a narrow role would otherwise be shown the entire application. `ZakatDonations` is therefore scoped client-side to the nine routes it actually works in — dashboard, donors, donations, other income, funds, finance reports, participants, zakat eligibility, sponsorships — via an allowlist in `lib/roles.ts`, enforced in the sidebar and by `RequireRouteScope` for anyone arriving by URL. Its dashboard renders only the fund and income cards, and the three delivery-side report queries are not issued at all. This is a **relevance filter, not a security boundary**: the API still serves those reads, and the real gates remain the policies above.

### Auth mechanics
- JWT access tokens, 15-minute expiry. Refresh tokens, 7-day expiry, **rotate on every use** (old refresh token is invalidated the moment a new pair is issued).
- **Account lockout**: 5 failed login attempts locks the account for 15 minutes (HTTP 423).
- Password change, logout (single session), and "revoke all sessions" (kills every refresh token for the user) are all self-service.
- New users are created via `POST /api/auth/register` (Admin-only) — there is no public sign-up.

---

## 3. Domain modules

### 3.1 Reference Data
A single generic `LookupItem` table (`category`, `code`, `label`, `sortOrder`, `isActive`) backs every coded dropdown in the system, rather than hardcoding enums for business vocabulary that changes over time (categories, statuses, types). **31 categories** are seeded today, spanning Finance/Programs (`fund_type`, `expense_category`, `zakat_asnaf`, `payment_method`, `donor_type`, `activity_type`, `region`, `beneficiary_status`, `distribution_type`, `participant_type`, `vulnerability_category`, `safeguarding_category`, `program_category`, `program_status`, `implementation_status`, `age_group`, `partner_type`, `volunteer_status`, `sponsorship_type`, `income_type`, `engagement_type`, `beneficiary_type`) and Governance (`person_category`, `person_status`, `org_body_type`, `governance_role_category`, `meeting_type`, `meeting_mode`, `attendance_status`, `meeting_role`, `decision_status`).

Seeding is **additive**: re-running the seeder only inserts `(category, code)` pairs that don't already exist, so adding a new category in a later sprint doesn't require a destructive reseed of an environment that already has data.

Codes are immutable once created (records reference them by string); only `label`, `sortOrder`, and `isActive` can be edited. Deactivating a lookup item hides it from new-entry dropdowns without breaking historical records that already reference its code.

### 3.2 Finance
The financial core. Modeled as a **two-tier fund system**, not a flat chart of accounts:

- **Fund** (6 seeded): `ZAKA-FUND` (Zakat, restricted), `SADA-FUND` (Sadaqah), `GENE-FUND` (General), `REST-FUND` (Restricted Grant, restricted), `OPER-FUND` (Operations), `CAPX-FUND` (Capital, restricted).
- **FundingBucket** (10 seeded): each Fund splits into typed buckets — `Program`, `Admin`, `Operations`, or `Capital` — each with its own **maximum admin-rate cap**. Money is only ever spendable from a bucket's live `remaining = allocatedAmount − expensedAmount`, which the API guarantees never goes negative.

**Money movement, three inflow paths + one outflow path:**
- **Donation** — the primary inflow. Multi-currency (`amountOriginal` + `currency` + `fxRateToPhp` → API-computed `amountPhp`). Auto-splits into that fund's Program bucket and, if `adminAllowed`, its Admin bucket — capped at **15% for general funds, 12.5% for Zakat** (the Amil/collector share). The requester's rate is clamped to the bucket's cap, never trusted as-is.
- **Other Income** — non-donation inflows (capital income, grants, waqf, government support — `income_type` lookup). Lands 100% in one bucket, no admin split. **The Zakat fund is hard-blocked** as a destination (`400 OtherIncome.ZakatFundNotAllowed`) — Zakat may only enter the system through a Zakat donation, never as generic "other income."
- **Opening Balances** — one seeded snapshot per fund for the current year (e.g. `REST-FUND` carries a real ₱139,930.15 restricted balance forward from the prior year).
- **Expense** — the single outflow path. Rejected if it would overdraw the bucket. Spending from the Zakat program bucket (`ZAK-PROG`) requires **both** `zakatAsnaf` (one of the 8 categories) **and** `beneficiaryCount` — enforced, not optional.

**Donor compliance**: every donor carries `KydStatus` (Pending/Review/Cleared/Rejected), `RiskRating` (Low/Medium/High), a PEP (politically-exposed-person) flag, and privacy consent. New donors always start `KydStatus = Pending`.

**Voiding**: donations, other income, expenses are soft-voided, never hard-deleted, and voiding reverses the exact bucket-balance effect the create had. A donation cannot be voided once its allocated funds have already been spent (`400`, not silently allowed).

**Reports** (7): fund summary, admin-recovery control (per-bucket cap vs. allocated vs. expensed vs. remaining), donor utilization, income summary, restricted-fund ledger (running balance with opening/closing), Zakat & Amil summary, Zakat-by-asnaf breakdown.

### 3.3 Programs
The program-delivery hierarchy: **Program → Project → Activity**, matching the org's own 9-phase workflow (needs identification → project creation → beneficiary registration → activity planning → mobilization → delivery/distribution → attendance logging → completion/closeout → reporting). A Project can optionally reference a Finance `Donor`/`Fund` (informational link, not a DB foreign key — see §4). An Activity can link to a real `Partner` (§3.4) as its implementing organization, with the legacy free-text field now **server-derived** from the partner's name (no drift possible once linked — a partner rename cascades automatically).

**Lifecycle** — `Project.implementationStatus` and `Activity.implementationStatus` (`implementation_status` lookup: `PLANNED, ONGOING, COMPLETED, DELAYED, ON_HOLD, CANCELLED`) both move through the **same enforced transition table**, via `POST /api/projects/{id}/status` and `POST /api/activities/{id}/status` — not free-text `PUT`, which no longer accepts a status value at all. `PLANNED → ONGOING/CANCELLED`; `ONGOING → COMPLETED/DELAYED/ON_HOLD/CANCELLED`; `DELAYED`/`ON_HOLD → ONGOING/CANCELLED`; `COMPLETED`/`CANCELLED` are terminal (`400 {Projects|Activities}.InvalidStatusTransition` otherwise). Completing an Activity captures `actualBeneficiaries`/`actualEndDate`. **Closing a Project is blocked** (`400 Projects.HasOpenActivities`) unless every child Activity is `COMPLETED` or `CANCELLED` — the project's own `closedAt` timestamp is set only on that transition.

**Participant Registry** — deliberately modeled as one generic registry (`participantType`: beneficiary/trainee/attendee/volunteer/partner-rep/community-leader/donor-rep/observer), not a narrow "Beneficiary" table, per the org's own proposed data model. Carries `vulnerabilityCategory`, `safeguardingCategory`, and `consentOnFile` — this is the population the org's safeguarding policy exists to protect. **Consent is a hard registration gate**: `consentOnFile = false` is rejected outright (`400 Participants.ConsentRequired`, no exceptions, on both create and update) rather than stored and ignored. An elevated `safeguardingCategory` (anything other than `NONE`) does not block registration — the record saves with a `safeguardingWarning` flag and message on the response, which the officer must act on. Participant PII (phone, vulnerability/safeguarding category, consent status) and the beneficiary master-list report are gated to **Program/Admin only**, not the any-authenticated-role default most reads use — same boundary as Zakat eligibility case data.

**Activity roster** — participants enroll into activities (unique per activity+participant pair, `409 ActivityParticipants.AlreadyEnrolled` on duplicate). Attendance status is validated against the `attendance_status` lookup (`PRESENT, ONLINE_PRESENT, LATE, EXCUSED, ABSENT`) rather than accepted as free text. Enrollment enforces the participant's on-file consent when the roster entry marks `consentRequired = true` (`400 ActivityParticipants.ConsentRequired`) — the gate `Participant.consentOnFile`/`ActivityParticipant.consentRequired` used to declare without enforcing. Removal is a **soft delete** (`isActive`), not a hard delete, so attendance/audit history survives; re-enrolling after removal reactivates the same row rather than inserting a second one. `PUT /api/activities/{activityId}/participants/{participantId}` updates attendance/role in place, so correcting a record no longer requires delete-then-re-post.

**Distribution** — one generic entity for all aid handed to a beneficiary (cash, food pack, hygiene kit, school supplies...), rather than separate tables per aid type. Recording a distribution **now posts a linked Finance `Expense` in the same save** — `Expense` remains the single source of truth for money movement (same balance check, same Zakat-asnaf gate as a hand-entered expense), and `Distribution` never mutates a `FundingBucket` itself. `activityId` and `fundingBucketCode` are **required** (closing a prior gap where an omitted bucket code silently skipped the Zakat-eligibility check entirely). `totalValuePhp` is server-computed as `quantity × unitValuePhp`, never client-supplied; `beneficiaryCount` is carried for Zakat-asnaf reporting only and deliberately excluded from that formula, since `participantId` is a single non-nullable FK — a bulk/household distribution event is future scope, not this one. A zero-value in-kind handout (`unitValuePhp = 0`) is allowed and posts no Expense at all. Voiding a distribution reverses its linked expense's bucket effect; voiding that Expense directly is blocked (`409 Expenses.VoidViaDistribution`) so the two records can't drift out of sync. Historical distributions recorded before this change have no linked expense and are never backfilled, since backfilling would retroactively consume live bucket balances.

**Reports** (4): program summary (project/activity counts + budgets + total distributed value — sound for every distribution recorded after `activityId` became required; older orphaned rows are a known, documented gap), distribution summary (by type, with distinct-participant counts), activity report (roster size, present count, distribution count/value per activity), beneficiary master list (vulnerability breakdown, PII-bearing, Program/Admin only).

### 3.4 Partners, Volunteers, Sponsorship, Zakat Eligibility
- **Partner** — implementing/donor organizations. Unique name. Carries MOU tracking (reference, start/end dates) and accreditation notes.
- **Volunteer** — a registry with explicit **safeguarding-compliance tracking**: orientation completed (+ date), code-of-conduct signed (+ date), police clearance on file. Enrolling a volunteer into an activity whose `safeguardingRisk` is set to anything other than `NONE` **requires `orientationCompleted = true`**, enforced server-side (`400 ActivityVolunteers.OrientationRequired`) — this is the safeguarding gate with actual teeth, not a checkbox nobody checks.
- **Sponsorship** — recurring pledges linking a Finance Donor to a Programs Participant (`monthlyAmountPhp`, `caseWorker`, lifecycle `Active → Paused/Ended`, `Ended` is terminal). **Deliberately decoupled from actual money**: the pledge amount is a commitment record; real payments are ordinary Finance Donations. This keeps `Expense`/`Donation` as the only sources of truth for cash, at the cost of no automatic "did this month's pledge actually arrive" reconciliation (see §8).
- **Zakat Eligibility** — the formal case-assessment workflow that *gates* who may receive Zakat. Lifecycle: `Draft → Submitted → Approved/Rejected` (Admin decides). A **Distribution against the Zakat program bucket is blocked unless the participant has an Approved, unexpired eligibility case** — the API auto-fills the distribution's `zakatAsnaf` from the approved case, or rejects it if the caller supplies a mismatching one. Only one live (Approved, unexpired) case per participant is allowed, enforced at the database level with a unique index (not just an application-level check — this was tightened after a code-review pass specifically because it gates charitable-fund disbursement).

### 3.5 Governance
Sourced entirely from the org's real "Enhanced System v7.2" workbook — 11 governance sheets there, of which this sprint builds 9 (the two omitted, **External Engagements** and **Media Outputs**, had essentially no real data — 1 populated row apiece behind hundreds of empty stub IDs — and are deferred rather than built against placeholder shape).

- **Person** — the identity hub every other governance record hangs off. `personCategory` (Board/Executive/Member), `personStatus`, optional loose link to a `Volunteer` (informational, handler-validated, no DB FK — same pattern as Programs↔Finance links, see §4).
- **OrgBody** — the governance-body hierarchy (General Assembly → Board of Trustees → Executive Management → committees/units), self-referencing via `parentBodyId`. `quorumRule`/`decisionThreshold`/`meetingFrequency`/`policyBasis` are kept as **free-text policy strings verbatim from the org's governance manual** (e.g. `"50% + 1"`, `"75% for strategic decisions"`) rather than parsed into numbers — see the quorum note below. Updating a body's parent is rejected if it would create a cycle (`400 Governance.CircularBodyHierarchy`, walks the ancestor chain server-side); deactivating a body with active child bodies or current assignments is rejected (`409 Governance.BodyInUse`).
- **GovernanceRole** — role master (Chairperson, Vice Chair, Secretary, Treasurer, CEO, Member...). Its voting/quorum/delegable fields are also free-text conditional rules ("Depends on body", "Yes if eligible"), not booleans — actual per-instance enforcement lives on `Assignment`.
- **Assignment** — the entity "board trustees" and "executive team" are *derived from*, not stored as. There is no separate `BoardTrustee`/`ExecutiveTeamMember` table; a person's board or executive membership is just an `Assignment` (person × org body × role, with start/end dates and a `Current`/`Former` status), and `GET /api/governance/bodies/{id}/members` resolves the live roster by filtering to `Current` assignments (or `asOf`/`includeFormer` for historical views). **At most one primary Current assignment per person** is enforced at the database level with a unique index (`409 Governance.DuplicatePrimaryAssignment` on violation, including the race case) — the same pattern used for `ZakatEligibility.IsLiveApproval`.
- **Meeting** — belongs to an OrgBody; `quorumRequired`/`decisionThreshold` are **snapshot-copied from the body at creation time**, not live-read, so a later policy change doesn't silently rewrite the historical record of what applied to a past meeting. `publicationDeadline` defaults to meeting date + 10 days (the org's own convention) when not supplied.
- **MeetingParticipant** — attendance/voting record per person per meeting, optionally tied to the specific `Assignment` that granted the vote (validated to actually belong to that person: `400 Governance.AssignmentPersonMismatch`).
- **Quorum reporting** (`GET /api/governance/meetings/{id}/quorum`) — reports eligible/present/quorum-eligible-present counts and a percentage, **alongside** the meeting's snapshotted quorum-rule text. It deliberately does **not** attempt to evaluate free-text rules like `"50% + 1 ordinary; 2/3 strategic decisions"` — that interpretation is a human judgment call the system surfaces numbers for, not something it fakes.
- **MeetingMinutes / MeetingDecision** — minutes are 1:1 with a Held meeting (`400 Governance.MeetingNotHeld` if the meeting hasn't happened yet; `409 Governance.MinutesAlreadyExist` on a second attempt); decisions are a **separate, properly-keyed child entity**, not inline text on the minutes row — the source workbook's "Minutes ID" was reused across multiple decision rows for one meeting, which this schema fixes. Both minutes and their decisions are locked from further edits once `PublicationStatus = Published` (`409 Governance.MinutesPublished`).
- **Reports** (1): governance summary — per body, current member count, meetings held (date-range filterable), minutes published vs. pending, open decisions.

---

## 4. Cross-cutting technical requirements

- **API shape**: .NET 10 vertical-slice Minimal API. Every write validated (FluentValidation) before the handler runs. Every error is RFC-9457 `ProblemDetails` — machine-readable `title` code (e.g. `"Donations.FundsAlreadySpent"`) + human `detail` string, always safe to surface directly in UI.
- **Money**: `DECIMAL(14,2)` everywhere, no floats. All reporting settles in PHP; multi-currency inputs carry the FX rate used, computed server-side (never trust a client-supplied converted amount).
- **Soft delete only**: every entity that can be "removed" uses `isActive`/`isVoided`, never a hard delete. List endpoints default to hiding inactive/voided rows with an explicit opt-in to show them.
- **Audit trail**: every entity is timestamp- and actor-stamped (`createdAt`/`updatedAt`/`createdBy`/`updatedBy`) automatically via a `SaveChanges` interceptor — handlers never set these manually.
- **Cross-module coupling is deliberately loose**: links from Programs into Finance (`Project.FundCode`, `Distribution.FundingBucketCode`) are validated at the handler level (existence/active checks) rather than enforced as database foreign keys, so the two modules can evolve independently. The one exception is `Distribution.ExpenseId` — a real DB foreign key to `Expenses`, because that link is the actual money-movement record a distribution posts, not an informational cross-reference.
- **Migrations must be additive-only** on this MariaDB/XAMPP target — a `RENAME COLUMN` in an EF-generated migration hit a real MariaDB 10.4 syntax incompatibility in Sprint 2. Every migration since has been designed to add tables/columns only, never rename in place.
- **CORS**: locked to `http://localhost:3000` (the intended frontend dev origin) — not open/wildcard.

---

## 5. Non-functional / operational requirements

- **Testing**: xUnit + `WebApplicationFactory`, each test class gets its own isolated EF Core InMemory database (a real bug — a shared hardcoded database name caused parallel test classes to silently duplicate seed data — was found and fixed; every test class now gets a uniquely-named store).
- **No live-database verification has been completed in any session to date.** Every check so far has been against the InMemory test provider; nobody has confirmed a migration applies cleanly to a real running MariaDB/XAMPP instance. Treat this as the standing top-priority verification gap, not a formality.
- **Branching**: `{sprint-name}/feature/{feature-name}`, one PR per sprint, sequential base branches while prior sprints are unmerged.

---

## 6. Sprint status (what exists vs. what's proposed)

| Sprint | Scope | Status |
|---|---|---|
| 1 | Auth, Reference Data | Merged |
| 2 | Finance (Fund/FundingBucket model, donations, expenses, KYD/AML, 7 reports) | Merged |
| 3 | Programs (Program/Project/Activity, Participants, roster, Distributions, 2 reports) | Merged |
| 4 | Partners, Volunteers, Sponsorship, Zakat Eligibility, Other Income | Built, PR open, includes a review-feedback follow-up (DB-enforced single-live-approval, server-derived partner name, lookup-validated safeguarding risk) |
| 5 | Governance (Person, OrgBody, GovernanceRole, Assignment, Meeting, MeetingParticipant, MeetingMinutes, MeetingDecision, governance-summary report), plus Donor Engagements and `Participant.BeneficiaryType` | Built, branch `sprint-5/feature/governance` |
| 6 | Program & Beneficiary workflow refactor: consent/safeguarding gates on registration, enforced Project/Activity lifecycle transitions + closeout guard, Distribution now posts a linked Finance Expense (server-computed total, required bucket, Zakat gate closed against the omitted-bucket bypass), attendance vocabulary validation + roster soft-delete/reactivate, activity report + beneficiary master list | Built |
| 7 (proposed, not yet planned) | External Engagements + Media Outputs (the 2 remaining workbook governance sheets — deferred from Sprint 5 for having ~1 real data row each), Feedback/Complaints, Documents/Evidence registry, M&E Reports, Follow-ups, **sponsorship-fulfillment reconciliation report**, **row-level scoping for Sponsorship**, drop the now-safe-to-remove legacy `Activity.ImplementingPartner` string column, wire `FundingBucket.ConcurrencyStamp` into an actual optimistic-concurrency check | Not started |
| Unslotted | Staff directory, Membership, Finance hardening (bank reconciliation, KYD-review/donor-engagement workflow endpoints, year-end close checklist, controls audit, opening-balance import beyond the one seeded snapshot) | Not started |

---

## 7. Known gaps (surfaced deliberately, not hidden)

1. **Live MySQL verification never done** (see §5) — highest-priority open item.
2. **Sponsorship has no cash-reconciliation** against actual Finance donations — you can see what's *pledged*, not what's *arrived*.
3. **Sponsorship has no row-level authorization** — any Program-role user can read/write any sponsorship (financial commitment + case-worker assignment over a vulnerable population), not just their own assigned cases.
4. **Activities have no direct "funding source" field** — funding is tracked at the Project level and per-Distribution, not on the Activity row itself.
5. **`SdgAlignment` is free text**, not a multi-select tag list — usable for display, not filterable/reportable per-SDG.
6. **List-endpoint filtering is limited** — e.g. Activities/Projects can't be filtered by category or SDG tag yet, even though the data is stored.
7. **Governance quorum/decision-threshold rules are not machine-evaluated** — the API reports attendance counts and the org's own free-text policy string side by side; it does not parse "50% + 1 ordinary; 2/3 strategic decisions" and declare a meeting quorate. That judgment stays human.
8. **External Engagements and Media Outputs are not built** — 2 of the 11 workbook governance entities, deferred for having almost no real data behind them (see §6).
9. **Governance writes are Admin-only** with no dedicated role — fine for a first cut, but means a board secretary can't manage minutes without full Admin access.
10. **Funding-bucket balance checks have no concurrency protection** — two simultaneous writes against the same bucket (an Expense and a Distribution, or two of either) can both pass the balance check before either commits. Pre-existing in Finance since Sprint 2; Sprint 6 made it reachable from one more endpoint rather than introducing it. A `FundingBucket.ConcurrencyStamp` column exists for a future optimistic-concurrency pass but is not yet wired into `ExpensePosting` or incremented anywhere.
11. **Distributions recorded before Sprint 6 have no linked Expense** and were deliberately not backfilled (backfilling would retroactively consume live bucket balances). Program-summary totals are sound going forward — `activityId` is now required — but older rows with no `activityId` are still excluded, exactly as before.

---

## 8. Where the real requirements come from

The org provided real production Excel workbooks (outside this repo) as the actual source of truth, ranked when they conflict:
1. **"Proposed Final Database"** workbook — clean target schema, wins on entity shape.
2. **"PhilCare Financial Donor Database" (2026)** workbook — authoritative for the Finance module specifically (fund/bucket model, admin-rate caps, KYD/AML fields).
3. **"Enhanced System v7.2"** workbook — messier historical data; useful for extra field ideas, not overall structure; also the **sole source of the Governance module** (§3.5) — the "Proposed Final Database" workbook contains no governance sheets at all.

See `FRONTEND_PROMPTS.md` for the complete field-by-field API contract (all 129 endpoints, every request/response shape, every enum, every business rule a frontend must surface) — that file is kept in lockstep with the actual API and is the more precise reference for anyone implementing against it.
