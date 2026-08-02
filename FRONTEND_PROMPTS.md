# PhilCare MIS — Frontend Prompt Playbook

Complete, ready-to-paste prompts to build the PhilCare MIS frontend with an AI coding tool (Claude Code, Cursor, Windsurf, etc.) against the existing .NET REST API in this repo.

## How to use this file

1. Start the backend first: run `src/philcare.Api` (API listens on **http://localhost:5182**). CORS is already configured for a frontend at **http://localhost:3000**.
2. Create an empty folder for the frontend (outside this repo or in a `frontend/` sibling folder) and open it in your AI coding tool.
3. Paste **Prompt 1** and let the tool finish. Run the app, confirm it compiles, then continue with Prompt 2, then 3, and so on — **one prompt at a time, in order**. Each prompt assumes the previous ones are done.
4. If the tool loses context mid-way (new session, compaction), paste **Prompt 0 — Global Context** first, then continue with the phase prompt you were on.
5. Log in with the seeded admin account (from the API's `appsettings.json` `Seed` section: `admin@philcare.local` / `Admin@12345` unless changed).

Phases: 1 Scaffold → 2 Auth & shell → 3 Dashboard → 4 Donors → 5 Donations → 6 Other Income & Expenses → 7 Funds & Finance reports → 8 Programs hierarchy → 9 Participants & Distributions → 10 Partners & Volunteers → 11 Sponsorships → 12 Zakat Eligibility → 13 Admin & polish.

---

## Prompt 0 — Global Context (paste when starting a fresh session)

```text
CONTEXT — PhilCare MIS frontend. Read this fully before doing anything.

You are building a React SPA admin system for PhilCare, a Philippine Islamic charity NGO. The backend is a finished .NET REST API; you NEVER modify it, you only consume it.

STACK (already scaffolded unless I say otherwise): React 18 + Vite + TypeScript, Tailwind CSS, shadcn/ui components, TanStack Query (react-query) for all server state, React Router v6, Axios. Dev server MUST run on port 3000 (backend CORS only allows http://localhost:3000). API base URL: http://localhost:5182 (from VITE_API_BASE_URL env var).

AUTH MECHANICS:
- POST /api/auth/login {email, password} → 200 {accessToken, refreshToken, refreshTokenExpiresAt}. 401 = bad credentials. 423 Locked = account locked out (max 5 failed attempts, 15-min lockout) — show a specific "account locked, try again later" message.
- Access tokens (JWT) expire in 15 minutes. Refresh tokens live 7 days and ROTATE: POST /api/auth/refresh {refreshToken} → new {accessToken, refreshToken, refreshTokenExpiresAt}. Store both tokens (localStorage is acceptable here); on any API 401, silently refresh once and retry; if refresh fails, clear tokens and redirect to /login.
- Send Authorization: Bearer <accessToken> on every request except login/refresh.
- GET /api/auth/me → {id, email, role} (role is a string). POST /api/auth/change-password {currentPassword, newPassword} → 204. POST /api/auth/logout {refreshToken} → 204. POST /api/auth/revoke-all → 204 (kills all sessions).

ROLES & UI GATING — role comes from /api/auth/me. Four roles: Admin, Finance, Program, Viewer.
- Reads: every authenticated role can view every screen and report.
- Finance writes (create/edit donors, donations, other income, expenses): visible only to Finance and Admin.
- Program writes (programs, projects, activities, participants, enrollment, distributions, partners, volunteers, sponsorships, zakat cases): visible only to Program and Admin.
- Admin-only: void buttons (donations, other income, expenses, distributions), deactivate buttons (programs, partners, volunteers, donors are edited not deleted), user management, lookup management, zakat approve/reject decision.
- Hide (don't just disable) buttons the current role can't use. Also handle 403 responses gracefully with a toast ("You don't have permission for this").

ERROR HANDLING — the API returns three shapes; handle all:
1. Business errors (400/404/409/423): RFC-9457 ProblemDetails {type, title, status, detail, traceId}. "title" is a machine code like "Donations.FundsAlreadySpent"; "detail" is the human message — show "detail" in a toast/inline alert.
2. Validation errors (400): {title: "One or more validation errors occurred.", status: 400, errors: {FieldName: ["message", ...]}} — map to the matching form fields.
3. Unhandled (500): {title: "An unexpected error occurred", detail: "..."} — generic error toast.

ENUMS — serialized as strings everywhere: UserRole(Admin|Finance|Program|Viewer), Gender(Male|Female|Unspecified), DonorType(Individual|Organization|Partner), KydStatus(Pending|Review|Cleared|Rejected), RiskRating(Low|Medium|High), BucketType(Program|Admin|Operations|Capital), AllocationType(Program|Admin|Amil|Opening|Income), SponsorshipStatus(Active|Paused|Ended), ZakatEligibilityStatus(Draft|Submitted|Approved|Rejected).

LOOKUPS — all category-coded dropdowns are data-driven: GET /api/lookups/{category} → [{id, category, code, label, sortOrder, isActive}]. Forms submit the CODE, tables display the LABEL. Cache lookups with TanStack Query (staleTime ~5 min). Categories in use: fund_type, expense_category, zakat_asnaf, payment_method, donor_type, activity_type, region, beneficiary_status, distribution_type, participant_type, vulnerability_category, safeguarding_category, program_category, implementation_status, age_group, partner_type, volunteer_status, sponsorship_type, income_type.

MONEY — everything settles in PHP. Format as ₱ with thousands separators, 2 decimals (Intl.NumberFormat 'en-PH', currency PHP). Multi-currency inputs (donations, other income, expenses) take amountOriginal + currency + fxRateToPhp and the API computes amountPhp = round(amountOriginal × fxRateToPhp, 2) — preview this live in forms.

SEEDED FINANCE CODES (for dropdowns until fetched): Funds: ZAKA-FUND (Zakat, restricted), SADA-FUND (Sadaqah), GENE-FUND (General), REST-FUND (Restricted Grant, restricted), OPER-FUND (Operations), CAPX-FUND (Capital, restricted). Buckets (code → fund, type): ZAK-PROG→ZAKA-FUND Program, ZAK-AMIL→ZAKA-FUND Admin, SADA-PROG/SADA-ADMIN, GENE-PROG/GENE-ADMIN, REST-PROG/REST-ADMIN, OPER-POOL→OPER-FUND Operations, CAPX-FUND→CAPX-FUND Capital. Always fetch live from GET /api/funds and GET /api/funding-buckets rather than hardcoding.

CONVENTIONS: JSON is camelCase. Dates are ISO strings — display as dates (no time) unless noted. IDs are ints. List endpoints return plain arrays (no pagination envelope) — paginate/search client-side in tables. Soft-delete everywhere: rows have isActive or isVoided; default list views hide inactive/voided, with a toggle to show them.
```

---

## Prompt 1 — Scaffold

```text
Create a new React admin app for "PhilCare MIS" (a charity/NGO management system) with this exact stack:

- Vite + React 18 + TypeScript (strict mode).
- Tailwind CSS configured, plus shadcn/ui initialized (neutral base color) with at least: button, input, label, card, table, dialog, dropdown-menu, select, badge, toast/sonner, tabs, form, skeleton, alert, separator, sheet, tooltip, checkbox, textarea, popover, calendar.
- TanStack Query v5 with a QueryClientProvider at the root (default staleTime 30s, retry 1).
- React Router v6 with a route shell: /login (public) and an authenticated layout wrapping everything else.
- Axios instance in src/lib/api.ts reading VITE_API_BASE_URL (default http://localhost:5182). Add request interceptor that attaches Authorization: Bearer <accessToken> from localStorage, and a response interceptor that on 401 attempts ONE token refresh (POST /api/auth/refresh with the stored refreshToken, expecting {accessToken, refreshToken, refreshTokenExpiresAt}, storing the rotated pair) then retries the original request; on refresh failure it clears storage and hard-redirects to /login. Use a shared in-flight refresh promise so concurrent 401s trigger only one refresh call.
- Vite dev server pinned to port 3000 (strictPort: true) — the backend's CORS only allows http://localhost:3000.
- .env with VITE_API_BASE_URL=http://localhost:5182, and .env.example.
- Folder structure: src/features/<module>/ (components + hooks per module), src/components/ (shared), src/lib/ (api, utils, auth), src/types/ (shared TypeScript types).
- src/types/api.ts with shared types: ProblemDetails {type?, title?, status?, detail?, errors?: Record<string, string[]>}, LookupItem {id, category, code, label, sortOrder, isActive}, and string-literal union types for these enums (they serialize as strings): UserRole = 'Admin'|'Finance'|'Program'|'Viewer'; Gender = 'Male'|'Female'|'Unspecified'; DonorType = 'Individual'|'Organization'|'Partner'; KydStatus = 'Pending'|'Review'|'Cleared'|'Rejected'; RiskRating = 'Low'|'Medium'|'High'; BucketType = 'Program'|'Admin'|'Operations'|'Capital'; AllocationType = 'Program'|'Admin'|'Amil'|'Opening'|'Income'; SponsorshipStatus = 'Active'|'Paused'|'Ended'; ZakatEligibilityStatus = 'Draft'|'Submitted'|'Approved'|'Rejected'.
- src/lib/format.ts with formatPhp(n) (Intl.NumberFormat en-PH, PHP currency, 2dp) and formatDate(iso) (date only).
- src/lib/lookups.ts with a useLookup(category: string) hook: GET /api/lookups/{category}, returns items sorted by sortOrder, filtered to isActive, cached 5 minutes.
- A generic getErrorMessage(err) helper that extracts ProblemDetails "detail" (or the first validation message from "errors") from an Axios error for toasts.

Placeholder pages are fine for now (just "Login" and "Dashboard" text). Verify it builds and runs on http://localhost:3000.
```

---

## Prompt 2 — Auth & App Shell

```text
Implement authentication and the authenticated app shell. Backend endpoints (base http://localhost:5182):

- POST /api/auth/login {email, password} → 200 {accessToken, refreshToken, refreshTokenExpiresAt}. Errors: 401 ProblemDetails (bad credentials — show detail), 423 (account locked after 5 failed attempts, 15 min — show a distinct lockout alert).
- GET /api/auth/me → {id, email, role} where role is 'Admin'|'Finance'|'Program'|'Viewer'.
- POST /api/auth/change-password {currentPassword, newPassword} → 204 (400 with validation errors or wrong current password).
- POST /api/auth/logout {refreshToken} → 204.
- POST /api/auth/revoke-all → 204 (revokes every session's refresh tokens).

Build:
1. /login page: centered card with email + password, zod validation, loading state, error alert (distinguish 423 lockout from 401). On success store both tokens + expiry in localStorage and navigate to /.
2. AuthContext (or a useAuth hook backed by TanStack Query on /api/auth/me): exposes {user, role, isLoading, logout()}. logout() calls POST /api/auth/logout with the stored refresh token (ignore errors), clears storage, navigates to /login.
3. ProtectedRoute wrapper: unauthenticated (no tokens) → redirect /login. While /api/auth/me loads show a full-page skeleton.
4. Role helpers: canFinanceWrite = role in (Finance, Admin); canProgramWrite = role in (Program, Admin); isAdmin = role === 'Admin'. Export a <RoleGate allow={...}> component that hides children when not allowed.
5. App shell: left sidebar (collapsible on mobile via sheet) with nav groups — Dashboard; Finance (Donors, Donations, Other Income, Expenses, Funds & Buckets, Reports); Programs (Programs, Projects, Activities, Participants, Distributions); People & Partners (Partners, Volunteers, Sponsorships, Zakat Eligibility); Administration (Users, Lookups — Admin only, hide the whole group otherwise). Top bar: current page title, user email + role badge, dropdown menu with "Change password" (dialog with current/new password fields), "Sign out everywhere" (confirm dialog → POST /api/auth/revoke-all → then logout), and "Log out".
6. Route skeleton for every nav item (placeholder pages), each wrapped in ProtectedRoute.

Keep the silent-refresh interceptor from the scaffold working — test that an expired access token transparently refreshes.
```

---

## Prompt 3 — Dashboard

```text
Build the Dashboard page (route "/") from these read-only report endpoints (all return 200 for any authenticated user):

- GET /api/reports/fund-summary → {buckets: [{fundCode, bucketCode, bucketName, allocatedAmount, expensedAmount, remaining}], grandTotalAllocated, grandTotalExpensed, grandTotalRemaining, overallAdminRatio}
- GET /api/reports/income-summary?year=2026 → {byType: [{incomeType, count, totalPhp}], grandTotalPhp}
- GET /api/reports/program-summary → [{programId, programName, projectCount, activityCount, totalProjectBudget, totalActivityBudget, totalDistributedValuePhp}]
- GET /api/reports/distribution-summary → [{distributionType, distributionCount, distinctParticipants, totalQuantity, totalValuePhp}]
- GET /api/reports/sponsorship-summary → [{sponsorshipType, status, count, totalMonthlyCommitmentPhp}]

Layout (responsive grid):
1. Four stat cards: Total Allocated (grandTotalAllocated), Total Expensed (grandTotalExpensed), Remaining (grandTotalRemaining), Admin Ratio (overallAdminRatio as %, warn-colored if > 15%).
2. "Fund balances" horizontal bar chart (recharts — install it): one bar per bucket, allocated vs expensed, grouped by fundCode with the bucket name as label.
3. "Other income by type" donut/pie from income-summary byType (current year), with grand total in the center or legend.
4. "Programs at a glance" compact table from program-summary: name, projects, activities, distributed value (formatPhp).
5. "Distributions by type" table: type, count, distinct participants, total value.
6. "Active sponsorships" mini-cards: sum count + totalMonthlyCommitmentPhp where status === 'Active', grouped by sponsorshipType.

All money via formatPhp. Loading skeletons per card, error alert per card (one failing report must not blank the page). "Refresh" button that invalidates all dashboard queries.
```

---

## Prompt 4 — Donors

```text
Build the Donors module under /donors. Endpoints:

- GET /api/donors?includeInactive=true|false → [{id, name, type, email?, phone?, isActive, kydStatus, riskRating}] (type: 'Individual'|'Organization'|'Partner'; kydStatus: 'Pending'|'Review'|'Cleared'|'Rejected'; riskRating: 'Low'|'Medium'|'High')
- GET /api/donors/{id} → all list fields + {address?, country?, notes?, pepFlag, privacyConsent}
- POST /api/donors (Finance/Admin only) body {name, type, email?, phone?, address?, country?, notes?, riskRating, pepFlag, privacyConsent} → 201; new donors always start kydStatus 'Pending'.
- PUT /api/donors/{id} (Finance/Admin only) body = create fields + {isActive, kydStatus} → 200.

Screens:
1. /donors list: shadcn table with client-side search (name/email), donor-type filter, "show inactive" toggle (drives includeInactive). Columns: Name, Type badge, Email, Phone, KYD status badge (Pending=gray, Review=yellow, Cleared=green, Rejected=red), Risk badge (Low=green, Medium=yellow, High=red), Active. Row click → detail. "New donor" button (RoleGate: Finance write).
2. /donors/:id detail: card with all fields incl. PEP flag and privacy-consent indicators; "Edit" button (Finance write) opens the form.
3. Create/edit form (dialog or page): name (required), type select (enum), email, phone, address, country, notes (textarea), risk rating select, PEP flag checkbox, privacy consent checkbox. Edit additionally exposes: KYD status select (this is how compliance clears a donor — label it "KYD review status") and Active switch. Zod validation mirroring: name required ≤200, email ≤256.
4. Handle validation-error shape (field mapping) and ProblemDetails toasts.

There is no donor delete — deactivation is via edit (isActive=false).
```

---

## Prompt 5 — Donations

```text
Build the Donations module under /donations. Endpoints:

- GET /api/donations?donorId=&fundCode=&from=&to=&includeVoided= → [{id, donorId, donorName, amountPhp, fundCode, dateReceived, isVoided}]
- GET /api/donations/{id} → {id, donorId, donorName, amountOriginal, currency, fxRateToPhp, amountPhp, fundCode, dateReceived, channel, purpose?, adminAllowed, adminRateInput, adminRateCap, adminRateApplied, programAllocationPhp, adminAllocationPhp, notes?, isVoided, allocations: [{allocationType, targetBucketCode, allocationRate, allocatedAmountPhp}]}
- POST /api/donations (Finance/Admin) body {donorId, amountOriginal, currency, fxRateToPhp, dateReceived, channel, purpose?, restrictedFlag, programOrProject?, fundCode, receiptNo?, cashDocumentationStatus?, sourceVerified, adminAllowed, adminRateInput, notes?} → 201 (response includes allocationStatus, programBucketCode?, adminBucketCode?, allocations[]).
- DELETE /api/donations/{id} (Admin only) → 204 void. 409/400 ProblemDetails if already voided or funds already spent — surface detail.

Business context to reflect in the UI: a donation lands in ONE fund (fundCode from GET /api/funds) and the API auto-splits it into that fund's Program bucket and (if adminAllowed) Admin bucket. Admin split is capped — 12.5% for the Zakat fund (amil share), 15% for others. adminRateInput is the requested rate; the API applies min(input, cap).

Screens:
1. /donations list: filter bar (donor combobox fed by GET /api/donors, fund select fed by GET /api/funds, date range pickers, include-voided toggle). Columns: Date, Donor, Fund, Amount (formatPhp), Voided badge. Row → detail. "Record donation" (Finance write).
2. Create form: donor combobox (searchable), amount original (number), currency (text, default "PHP"), FX rate to PHP (number, default 1) with a live computed preview "≈ ₱X" (amountOriginal × fxRateToPhp), date received, channel (text, e.g. Bank Transfer), fund select (show restricted funds with a lock icon), purpose, restricted flag checkbox, program/project text, receipt no, cash documentation status, source-verified checkbox, admin-allowed checkbox revealing admin-rate input (as % — send as decimal, e.g. 0.125) with helper text "capped at 12.5% for Zakat, 15% otherwise", notes.
3. /donations/:id detail: header with amount + void badge; conversion breakdown (original → rate → PHP); admin-split card (rate input vs cap vs applied, program vs admin PHP amounts); allocations table (type, bucket, rate, amount); "Void donation" button (Admin only, destructive confirm dialog explaining it reverses the bucket allocations; failure toast shows the API detail, e.g. funds already spent).
```

---

## Prompt 6 — Other Income & Expenses

```text
Build two Finance modules: Other Income (/other-income) and Expenses (/expenses).

OTHER INCOME — non-donation inflows (capital income, grants, waqf, government support...). Endpoints:
- GET /api/other-income?incomeType=&fundingBucketCode=&from=&to=&includeVoided= → [{id, incomeType, source, amountPhp, fundingBucketCode, dateReceived, isVoided}]
- GET /api/other-income/{id} → + {amountOriginal, currency, fxRateToPhp, fundCode, receiptNo?, evidenceLink?, notes?, allocations[]}
- POST /api/other-income (Finance/Admin) {incomeType, source, dateReceived, amountOriginal, currency, fxRateToPhp, fundingBucketCode, receiptNo?, evidenceLink?, notes?} → 201
- DELETE /api/other-income/{id} (Admin) → 204 void (409 already voided / 400 funds already spent — show detail).

Rules to surface: incomeType comes from lookup category "income_type" (CAPITAL_INCOME, GRANT, WAQF_ENDOWMENT, ...). The receipt goes 100% into ONE funding bucket (no admin split). The Zakat fund is BLOCKED — posting into a ZAKA-FUND bucket returns 400 "OtherIncome.ZakatFundNotAllowed"; filter ZAKA-FUND buckets out of the bucket select and mention why in helper text ("zakat may only enter via zakat donations").

Screens: list with filters (income-type lookup select, bucket select from GET /api/funding-buckets, date range, include-voided), columns Date/Type label/Source/Bucket/Amount/Voided; create form (income type lookup select, source, date, amount+currency+fx with live ₱ preview, bucket select grouped by fund and excluding ZAKA-FUND, receipt no, evidence link URL, notes); detail page with allocation line + void button (Admin).

EXPENSES — the single source of money-out. Endpoints:
- GET /api/expenses?fundingBucketCode=&expenseCategory=&from=&to=&includeVoided= → [{id, fundingBucketCode, amountPhp, expenseCategory, expenseDate, isVoided}]
- GET /api/expenses/{id} → {id, fundCode, fundingBucketCode, amountOriginal, currency, fxRateToPhp, amountPhp, expenseCategory, paymentMethod, expenseDate, description, receiptNo?, approvalStatus, approvedBy?, zakatAsnaf?, beneficiaryCount?, isVoided}
- POST /api/expenses (Finance/Admin) {expenseDate, payeeVendor, expenseCategory, description, programOrProject?, paymentMethod, amountOriginal, currency, fxRateToPhp, receiptNo?, approvedBy?, supportingDocStatus?, linkedDonationId?, expenseFunction?, fundingBucketCode, zakatAsnaf?, beneficiaryCount?, beneficiaryType?, notes?} → 201; the response includes remainingBucketBalance — show it in the success toast ("Recorded. ₱X remaining in <bucket>").
- DELETE /api/expenses/{id} (Admin) → 204 void (restores the bucket balance).

Rules to surface: expenseCategory ← lookup "expense_category"; paymentMethod ← lookup "payment_method". Spending against the ZAK-PROG bucket REQUIRES zakatAsnaf (lookup "zakat_asnaf") and beneficiaryCount — when the selected bucket is ZAK-PROG, reveal and require those two fields (helper: "zakat distributions must be attributed to one of the 8 asnaf"). Overspending a bucket returns 400 with detail — show it. Bucket select should display each bucket's live remaining balance (from GET /api/funding-buckets) so users see what's spendable before submitting.

Screens: list with filters, columns Date/Category label/Bucket/Amount/Voided; create form as above with live ₱ preview and conditional zakat fields; detail page (all fields, approval status, zakat attribution if present) with Admin void button.
```

---

## Prompt 7 — Funds, Buckets & Finance Reports

```text
Build the Funds & Buckets browser (/funds) and the Finance Reports section (/reports/finance).

FUNDS & BUCKETS. Endpoints:
- GET /api/funds → [{id, code, name, isRestricted, policyNotes?, useCase?, separateTrackingRequired}]
- GET /api/funding-buckets?fundCode= → [{id, code, name, fundCode, bucketType, maxAdminRate, allocatedAmount, expensedAmount, remaining}]
- GET /api/funding-buckets/{id} → + {allocations: [{donationId?, allocationType, allocatedAmountPhp, allocationDate}], expenses: [{id, amountPhp, expenseCategory, expenseDate, isVoided}]}

/funds page: card per fund (code, name, restricted lock badge, policy notes) containing a table of its buckets: bucket name, type badge, max admin rate (%), allocated, expensed, remaining (color remaining red if ≤ 0), each with a progress bar (expensed/allocated). Bucket row click opens a detail sheet/page: recent allocations (type, source donation id or "income"/"opening", amount, date) and recent expenses.

FINANCE REPORTS (/reports/finance, tabbed). All GET, any authenticated role:
1. Fund Summary — /api/reports/fund-summary (same data as dashboard; render as a full table grouped by fund with grand-total footer row + admin ratio).
2. Admin Recovery — /api/reports/admin-recovery → {buckets: [{bucketCode, bucketName, policyCapRate, allocatedAmountPhp, expensedAmountPhp, remainingAmountPhp, status}], totalAllocatedPhp, totalExpensedPhp, totalRemainingPhp}. Table with cap as %, status badge.
3. Donor Utilization — /api/reports/donor-utilization/{donorId}: donor combobox (from GET /api/donors) then → {donorId, donorName, funds: [{fundCode, donationCount, totalAmountPhp, programAmountPhp, adminAmountPhp}]}. Table per fund + totals.
4. Income Summary — /api/reports/income-summary?year= (year select defaulting to current) → byType table + grand total.
5. Restricted Ledger — /api/reports/restricted-ledger?fundCode=&year=: fund select (only isRestricted funds) + year → {fundCode, fundName, openingBalancePhp, entries: [{date, transactionType, reference, amountIn, amountOut, runningBalance, notes?}], closingBalancePhp}. Ledger table: opening row, entries (reference looks like "DON-12", "INC-3", or "Opening"), running balance column, closing row emphasized.
6. Zakat & Amil — /api/reports/zakat-amil → {totalZakatCollectedPhp, maxAmilSharePhp, amilAllocatedPhp, amilExpensedPhp, amilRemainingPhp}. Stat cards; note "amil share capped at 12.5% of zakat collected".
7. Zakat Asnaf — /api/reports/zakat-asnaf → [{zakatAsnaf, totalAmountPhp, totalBeneficiaries, expenseCount}]. Table + bar chart; map asnaf codes to labels via lookup "zakat_asnaf".

Each report tab: loading skeleton, empty state, and a CSV export button (client-side generation from the fetched rows).
```

---

## Prompt 8 — Programs Hierarchy

```text
Build the Programs hierarchy: Programs (/programs) → Projects (/projects) → Activities (/activities). Writes need Program or Admin role; deactivations need Admin.

PROGRAMS:
- GET /api/programs?includeInactive= → [{id, name, category, status, isActive}]
- GET /api/programs/{id} → + {ownerDepartment?, notes?, projectCount}
- POST /api/programs (Program write) {name, category, ownerDepartment?, notes?} → 201 (category ← lookup "program_category")
- PUT /api/programs/{id} (Program write) {name, category, ownerDepartment?, status, notes?, isActive}
- DELETE /api/programs/{id} (Admin) → 204 soft-deactivate.

PROJECTS:
- GET /api/projects?programId=&implementationStatus=&includeInactive= → [{id, programId, programName, name, totalBudget, implementationStatus, isActive}]
- GET /api/projects/{id} → + {donorId?, fundCode?, targetBeneficiaries?, startDate?, endDate?, location?, projectManager?, approvalLevel?, notes?, activityCount}
- POST /api/projects (Program write) {programId, name, donorId?, fundCode?, totalBudget, targetBeneficiaries?, startDate?, endDate?, location?, projectManager?, approvalLevel?, notes?} → 201
- PUT /api/projects/{id} (Program write) same minus programId, plus {implementationStatus, isActive}. No delete endpoint — deactivate via edit.

ACTIVITIES:
- GET /api/activities?projectId=&implementationStatus=&from=&to=&includeInactive= → [{id, projectId, projectName, name, activityType, budget, implementationStatus, isActive}]
- GET /api/activities/{id} → full detail incl. {activityCategory?, targetGroup?, barangay?, city?, province?, region?, startDate?, endDate?, implementingPartner?, implementingPartnerId?, implementingPartnerName?, responsibleDepartment?, sdgAlignment?, implementationStatus, safeguardingRisk?, evidenceLink?, notes?, participantCount, distributionCount}
- POST /api/activities (Program write) {projectId, name, activityCategory?, activityType, targetGroup?, barangay?, city?, province?, region?, startDate?, endDate?, budget, implementingPartner?, implementingPartnerId?, responsibleDepartment?, sdgAlignment?, safeguardingRisk?, evidenceLink?, notes?} → 201
- PUT /api/activities/{id} (Program write) same minus projectId, plus {implementationStatus, isActive}. No delete endpoint.

Form rules:
- activityType ← lookup "activity_type"; region ← "region"; implementationStatus ← "implementation_status" (status selects on edit forms).
- Implementing partner: a combobox fed by GET /api/partners (active only) setting implementingPartnerId. When a partner is linked, the API derives the free-text implementingPartner name server-side — make the free-text field read-only/hidden when a partner is selected (legacy field, being phased out). Invalid partner → 404, inactive partner → 400; show detail.
- safeguardingRisk ← lookup "safeguarding_category" (NONE, CHILD, VULNERABLE_ADULT, HIGH_RISK) — the API rejects values outside this set (400 Activities.InvalidSafeguardingRisk). Any value other than NONE later gates volunteer enrollment (mention in helper text).

Screens per level: list (filters incl. parent + status + show-inactive), detail (fields + child count + link to filtered child list, e.g. program detail links to /projects?programId=X), create/edit forms (parent preselected when navigating from a parent detail), Admin deactivate for programs (confirm dialog). Activity detail additionally shows tabs for Participants and Volunteers rosters — leave those tabs as placeholders; later prompts fill them.
```

---

## Prompt 9 — Participants & Distributions

```text
Build Participants (/participants), the activity participant roster (inside activity detail), and Distributions (/distributions).

PARTICIPANTS (beneficiary registry):
- GET /api/participants?participantType=&status=&includeInactive= → [{id, fullName, participantType, gender, status, isActive}]
- GET /api/participants/{id} → + {ageGroup?, phone?, barangay?, city?, province?, region?, country?, vulnerabilityCategory?, safeguardingCategory?, consentOnFile, remarks?}
- POST /api/participants (Program write) {fullName, participantType, gender, ageGroup?, phone?, barangay?, city?, province?, region?, country?, vulnerabilityCategory?, safeguardingCategory?, consentOnFile, remarks?} → 201 (status starts "PENDING")
- PUT /api/participants/{id} (Program write) same + {status, isActive}.

Lookups: participantType ← "participant_type", ageGroup ← "age_group", vulnerabilityCategory ← "vulnerability_category", safeguardingCategory ← "safeguarding_category", status ← "beneficiary_status". gender is the enum Male|Female|Unspecified. Status badge colors: PENDING gray, VERIFIED blue, SERVED green, REJECTED red, INACTIVE muted.

ACTIVITY PARTICIPANT ROSTER (fill the placeholder tab on activity detail):
- GET /api/activities/{activityId}/participants → [{participantId, participantName, participantType, roleInActivity?, attendanceStatus?}]
- POST /api/activities/{activityId}/participants (Program write) {participantId, roleInActivity?, attendanceStatus?, consentRequired, evidenceLink?, remarks?} → 201. 409 "already enrolled" — show detail.
- DELETE /api/activities/{activityId}/participants/{participantId} (Program write) → 204.
Roster tab: table + "Enroll participant" dialog (participant combobox searching GET /api/participants, role, attendance, consent-required checkbox, evidence link, remarks) + remove button per row (confirm).

DISTRIBUTIONS (aid handed to a beneficiary):
- GET /api/distributions?participantId=&activityId=&distributionType=&from=&to=&includeVoided= → [{id, distributionType, participantId, participantName, totalValuePhp, distributionDate, isVoided}]
- GET /api/distributions/{id} → + {activityId?, fundingBucketCode?, quantity, location?, fieldVerified, receivedConfirmation, processedBy?, zakatAsnaf?, notes?}
- POST /api/distributions (Program write) {distributionType, participantId, activityId?, fundingBucketCode?, quantity, totalValuePhp, distributionDate, location?, fieldVerified, receivedConfirmation, processedBy?, zakatAsnaf?, notes?} → 201
- DELETE /api/distributions/{id} (Admin) → 204 void (409 already voided).

Create-form rules to surface:
- distributionType ← lookup "distribution_type". Participant combobox (active only). Optional activity combobox. Optional funding-bucket select (from GET /api/funding-buckets) — note the value is informational; money movement stays with Finance expenses.
- ZAKAT RULE: if the selected bucket belongs to the Zakat fund's Program bucket (ZAK-PROG), the participant must have an APPROVED, unexpired zakat eligibility case. The zakatAsnaf field may be left empty (the API auto-fills it from the approved case) but if provided must match. Possible 400s to show verbatim from detail: "Distributions.ZakatEligibilityRequired", "Distributions.ZakatAsnafMismatch". Add helper text on the bucket select: "Zakat distributions require an approved eligibility case for the participant" with a link to /zakat-eligibilities?participantId=<selected>.
- Checkboxes: field verified, received confirmation. totalValuePhp direct input (no FX here), quantity ≥ 1.

Screens: participants list/detail/form; distributions list with filters; distribution detail with Admin void; participant detail shows a "Distributions" tab (GET /api/distributions?participantId=) and a "Zakat cases" link.
```

---

## Prompt 10 — Partners & Volunteers

```text
Build Partners (/partners) and Volunteers (/volunteers) plus the volunteer roster tab on activity detail.

PARTNERS:
- GET /api/partners?partnerType=&includeInactive= → [{id, name, partnerType, contactPerson?, isActive}]
- GET /api/partners/{id} → + {email?, phone?, address?, city?, province?, region?, mouReference?, mouStartDate?, mouEndDate?, accreditationNotes?, notes?, activityCount}
- POST /api/partners (Program write) {name, partnerType, contactPerson?, email?, phone?, address?, city?, province?, region?, mouReference?, mouStartDate?, mouEndDate?, accreditationNotes?, notes?} → 201. Duplicate name → 409 "Partners.DuplicateName".
- PUT /api/partners/{id} (Program write) same + {isActive} (renaming a partner auto-updates the name shown on its linked activities server-side).
- DELETE /api/partners/{id} (Admin) → 204 soft-deactivate.

partnerType ← lookup "partner_type". List with type filter + inactive toggle; detail card incl. MOU section (reference + start/end dates, badge "MOU expired" if mouEndDate < today) and linked-activity count; create/edit form; Admin deactivate.

VOLUNTEERS (with safeguarding compliance tracking):
- GET /api/volunteers?status=&orientationCompleted=&includeInactive= → [{id, fullName, gender, status, orientationCompleted, isActive}]
- GET /api/volunteers/{id} → + {phone?, email?, barangay?, city?, province?, region?, skills?, orientationDate?, codeOfConductSigned, codeOfConductDate?, policeClearanceOnFile, notes?, activityCount}
- POST /api/volunteers (Program write) {fullName, gender, phone?, email?, barangay?, city?, province?, region?, skills?, orientationCompleted, orientationDate?, codeOfConductSigned, codeOfConductDate?, policeClearanceOnFile, notes?} → 201 (status starts "ACTIVE")
- PUT /api/volunteers/{id} (Program write) same + {status, isActive} (status ← lookup "volunteer_status")
- DELETE /api/volunteers/{id} (Admin) → 204 (sets inactive + status INACTIVE).

List: filters (status lookup select, orientation-completed toggle, inactive toggle); columns incl. a compliance cell with three small badges — Orientation ✓/✗, Code of Conduct ✓/✗, Police Clearance ✓/✗ (green when true). Detail: full compliance card with dates. Form: the three compliance checkboxes each revealing an optional date field.

ACTIVITY VOLUNTEER ROSTER (fill the second placeholder tab on activity detail):
- GET /api/activities/{activityId}/volunteers → [{volunteerId, volunteerName, roleInActivity?, attendanceStatus?, hoursServed?}]
- POST /api/activities/{activityId}/volunteers (Program write) {volunteerId, roleInActivity?, attendanceStatus?, hoursServed?, remarks?} → 201
- DELETE /api/activities/{activityId}/volunteers/{volunteerId} (Program write) → 204

SAFEGUARDING GATE to surface: if the activity's safeguardingRisk is set and ≠ "NONE", enrolling a volunteer whose orientationCompleted is false returns 400 "ActivityVolunteers.OrientationRequired". In the enroll dialog, when the activity has a safeguarding risk show an amber banner ("This activity has safeguarding risk <label> — only orientation-completed volunteers can be enrolled") and mark non-oriented volunteers in the combobox. Duplicate enrollment → 409; inactive volunteer → 400 — show detail. hoursServed is a decimal (0–9999.9, one decimal).
```

---

## Prompt 11 — Sponsorships

```text
Build Sponsorships (/sponsorships) — recurring pledges linking a donor to a beneficiary.

Endpoints:
- GET /api/sponsorships?donorId=&participantId=&status=&sponsorshipType= → [{id, donorId, donorName, participantId, participantName, sponsorshipType, monthlyAmountPhp, status}] (status: 'Active'|'Paused'|'Ended')
- GET /api/sponsorships/{id} → + {startDate, endDate?, caseWorker?, nextReviewDate?, notes?}
- POST /api/sponsorships (Program write) {donorId, participantId, sponsorshipType, monthlyAmountPhp, startDate, caseWorker?, nextReviewDate?, notes?} → 201, starts Active. 409 "Sponsorships.DuplicateActive" if this donor already has a non-ended sponsorship for this participant; 404/400 for missing/inactive donor or participant — show detail.
- PUT /api/sponsorships/{id} (Program write) {sponsorshipType, monthlyAmountPhp, caseWorker?, nextReviewDate?, notes?} → 200. 409 "Sponsorships.AlreadyEnded" once ended.
- POST /api/sponsorships/{id}/status (Program write) {status, endDate?} → {id, status, endDate?}. Legal transitions: Active→Paused, Active→Ended, Paused→Active, Paused→Ended. Ended is terminal (any transition from Ended → 409).
- GET /api/reports/sponsorship-summary → [{sponsorshipType, status, count, totalMonthlyCommitmentPhp}]

sponsorshipType ← lookup "sponsorship_type" (CHILD, ORPHAN, FAMILY, STUDENT). monthlyAmountPhp is a plain PHP amount (no FX) — clarify in helper text that this is a pledge commitment; actual payments are recorded as Finance donations.

Screens:
1. /sponsorships list: filters (donor combobox, participant combobox, status select, type lookup select). Columns: Donor, Beneficiary, Type label, Monthly (formatPhp), Status badge (Active green, Paused yellow, Ended gray). Row → detail. "New sponsorship" (Program write).
2. Create form: donor combobox, participant combobox, type, monthly amount, start date, case worker, next review date, notes.
3. Detail: pledge card (all fields, formatted); lifecycle action buttons driven by current status — Active shows [Pause] [End]; Paused shows [Resume] [End]; Ended shows nothing but a terminal notice. [End] opens a dialog with optional end-date (defaults today) and a warning that ending is permanent. Edit button (disabled with tooltip once Ended).
4. Summary strip at the top of the list from sponsorship-summary: total active count + total monthly commitment, per-type chips.
```

---

## Prompt 12 — Zakat Eligibility Workflow

```text
Build the Zakat Eligibility module (/zakat-eligibilities) — the formal assessment + approval workflow that gates zakat distributions.

Endpoints:
- GET /api/zakat-eligibilities?participantId=&status=&asnaf= → [{id, participantId, participantName, asnafCategory, status, validUntil?}] (status: 'Draft'|'Submitted'|'Approved'|'Rejected')
- GET /api/zakat-eligibilities/{id} → {id, participantId, participantName, asnafCategory, monthlyIncomePhp?, householdSize?, assessmentDate, assessedBy?, assessmentNotes?, status, decisionDate?, decidedBy?, validUntil?, rejectionReason?, notes?}
- POST /api/zakat-eligibilities (Program write) {participantId, asnafCategory, monthlyIncomePhp?, householdSize?, assessmentDate, assessedBy?, assessmentNotes?, notes?} → 201, starts Draft.
- PUT /api/zakat-eligibilities/{id} (Program write) same minus participantId → 200. Only Draft cases are editable — 409 "Zakat.NotEditable" otherwise.
- POST /api/zakat-eligibilities/{id}/submit (Program write, no body) → {id, status}. Draft only; 409 "Zakat.AlreadyApproved" if the participant already holds an approved unexpired case.
- POST /api/zakat-eligibilities/{id}/decision (ADMIN ONLY) {approve: bool, decidedBy?, validUntil?, rejectionReason?} → {id, status, validUntil?, rejectionReason?}. Submitted only. Approving without validUntil defaults to +12 months. Rejecting REQUIRES rejectionReason (400 validation). 409 "Zakat.AlreadyApproved" if another live approval exists.

asnafCategory ← lookup "zakat_asnaf" (the 8 asnaf: FUQARA, MASAKIN, AMILIN, MUALLAF, RIQAB, GHARIMIN, FISABILILLAH, IBNU_SABIL) — always display labels.

Screens:
1. List: filters (participant combobox, status select, asnaf lookup select). Columns: Beneficiary, Asnaf label, Status badge (Draft gray, Submitted blue, Approved green, Rejected red), Valid until (show "Expired" in red when validUntil < today even if status is Approved). "New assessment" (Program write).
2. Create/edit form: participant combobox (create only), asnaf select, monthly income (₱), household size, assessment date, assessed by, assessment notes (textarea), notes. Edit reachable only while Draft — otherwise show a locked notice.
3. Detail: workflow stepper across the top (Draft → Submitted → Approved/Rejected, current step highlighted); assessment card; decision card (decision date, decided by, valid-until or rejection reason) when decided.
   Actions by status and role:
   - Draft + Program write: [Edit] [Submit for approval] (confirm dialog).
   - Submitted + ADMIN: [Approve] dialog (decided-by text prefilled with current user email, valid-until date picker with helper "defaults to 12 months if empty") and [Reject] dialog (rejection reason textarea, required).
   - Submitted + non-admin: banner "Awaiting admin decision".
   - Approved: green banner with validity period; note "This case authorizes zakat distributions for <name>".
   - Rejected: red banner with the reason.
4. Cross-link: participant detail (from the Participants module) gains a "Zakat cases" tab listing that participant's cases with statuses.
Surface every 409/400 detail message in toasts verbatim — the workflow rules (draft-only edits, one live approval per participant) come from the API.
```

---

## Prompt 13 — Admin (Users & Lookups) & Final Polish

```text
Finish with the Admin section (visible only to the Admin role — hide the nav group and guard the routes for everyone else) and a final polish pass.

USERS (/admin/users):
- GET /api/users (Admin) → [{id, email, role, isActive, createdAt}]
- POST /api/auth/register (Admin) {email, password, role} → 200 {id, email, role} — this is how new users are created.
- PUT /api/users/{id} (Admin) {role, isActive} → 200.
- DELETE /api/users/{id} (Admin) → 204 soft-deactivate + revokes their sessions. 400 "Users.CannotDeactivateSelf" — show detail.
Screen: table (email, role badge, active, created); "Create user" dialog (email, password with generate button, role select from the 4 roles: Admin, Finance, Program, Viewer); per-row edit dialog (role + active switch); deactivate button with confirm (disabled on your own row with a tooltip).

LOOKUPS (/admin/lookups):
- GET /api/lookups → all items; group client-side by category.
- POST /api/lookups (Admin) {category, code, label, sortOrder} → 201 (409 on duplicate category+code).
- PUT /api/lookups/{id} (Admin) {label, sortOrder, isActive} → 200 (code/category immutable).
- DELETE /api/lookups/{id} (Admin) → 204 soft-deactivate.
Screen: category list sidebar (19 categories) → item table (code, label, sort order, active) sorted by sortOrder; add-item dialog (category preselected, code UPPER_SNAKE hint, label, sort order); inline edit dialog (label/sort/active only — explain codes are immutable because records reference them); deactivate toggle. Warning banner: "Deactivating a lookup hides it from new entries; existing records keep their code."

FINAL POLISH PASS (whole app):
1. Toasts everywhere: success on create/update/void/decision; error toasts via getErrorMessage.
2. Empty states with helpful CTAs on every list ("No donors yet — record your first donor").
3. Loading skeletons on all tables/cards; disable submit buttons while mutating.
4. A shared 403 screen ("You don't have permission") and 404 route.
5. Confirm dialogs on every destructive action (voids, deactivations, revoke-all, end sponsorship) stating the consequence.
6. Every TanStack Query mutation invalidates the relevant list + detail + any dashboard/report queries it affects.
7. Number/date formatting audit: all money through formatPhp, all dates through formatDate.
8. Responsive check: sidebar collapses to a sheet under lg; tables scroll horizontally on mobile.
9. Title bar per route (document.title = "PhilCare — <page>").
10. Verify the full happy path end-to-end against the running API: login → create donor → record zakat donation → see fund-summary move → create program/project/activity → register participant → zakat assessment → submit → approve (admin) → record ZAK-PROG distribution (asnaf auto-fills) → record expense against ZAK-PROG with asnaf → check reports → void something as admin → user management.
```

---

## Prompt 14 — Governance

```text
Build the Governance module under /governance (Admin-write, any-authenticated-read, same as the rest of the app). This models the org's board/committee structure, meetings, and minutes.

KEY MODELING FACT: there is no separate "Board Trustees" or "Executive Team" entity. A person's board/executive membership is an Assignment (person × org body × role, with dates and a Current/Former status). The roster for any body comes from GET /api/governance/bodies/{id}/members, which resolves Current assignments live — don't try to build a separate "board members" screen from a dedicated endpoint, because there isn't one.

ORG BODIES (self-referencing hierarchy — General Assembly → Board of Trustees → Executive Management → committees/units):
- GET /api/governance/bodies?bodyType=&includeInactive= → [{id, name, bodyType, parentBodyId?, parentBodyName?, isActive}]
- GET /api/governance/bodies/{id} → +{quorumRule?, decisionThreshold?, meetingFrequency?, policyBasis?, notes?, currentMemberCount, childBodies: [{id, name, bodyType}]}
- GET /api/governance/bodies/{id}/members?asOf=&includeFormer= → [{personId, personFullName, assignmentId, roleName, positionTitle?, isPrimary, votingRights, startDate, endDate?, status}] — THIS is the board/exec roster.
- POST /api/governance/bodies {name, bodyType, parentBodyId?, quorumRule?, decisionThreshold?, meetingFrequency?, policyBasis?, notes?} → 201. Duplicate name → 409; unknown parent → 404.
- PUT /api/governance/bodies/{id} same + {isActive} → 200. Setting a parent that would create a cycle → 400 "Governance.CircularBodyHierarchy" — show this inline, it means the chosen parent is (transitively) this body's own child.
- DELETE /api/governance/bodies/{id} → 204. 409 "Governance.BodyInUse" if it has active child bodies or current assignments — show detail.
Note quorumRule/decisionThreshold are FREE TEXT policy strings from the org's governance manual (e.g. "50% + 1", "75% for strategic decisions") — display them as-is, don't try to parse/compute with them.

PEOPLE:
- GET /api/governance/people?personCategory=&status=&includeInactive= → [{id, fullName, personCategory, status, isActive}]
- GET /api/governance/people/{id} → +{email?, contactNumber?, defaultVotingRights, volunteerId?, notes?, assignmentCount}
- POST /api/governance/people {fullName, personCategory, email?, contactNumber?, defaultVotingRights, volunteerId?, notes?} → 201, status starts ACTIVE
- PUT /api/governance/people/{id} same + {status, isActive} → 200
- DELETE /api/governance/people/{id} → 204
personCategory ← lookup "person_category" (BOARD/EXECUTIVE/MEMBER); status ← "person_status".

GOVERNANCE ROLES:
- GET /api/governance/roles?roleCategory=&includeInactive= → [{id, name, roleCategory, isActive}]
- GET /api/governance/roles/{id} → +{defaultBodyId?, defaultBodyName?, defaultVotingRights?, countsForQuorum?, delegable?, notes?}
- POST /api/governance/roles {name, roleCategory, defaultBodyId?, defaultVotingRights?, countsForQuorum?, delegable?, notes?} → 201
- PUT /api/governance/roles/{id} same + {isActive} → 200
roleCategory ← lookup "governance_role_category". defaultVotingRights/countsForQuorum/delegable are free-text rules ("Depends on body", "Yes if eligible") — text inputs, not toggles.

ASSIGNMENTS (person holds a role in a body):
- GET /api/governance/assignments?personId=&bodyId=&roleId=&status= → [{id, personId, personFullName, orgBodyId, orgBodyName, governanceRoleId, governanceRoleName, isPrimary, status}]
- GET /api/governance/assignments/{id} → +{positionTitle?, startDate, endDate?, votingRights, isTemporary, notes?}
- POST /api/governance/assignments {personId, orgBodyId, governanceRoleId, positionTitle?, startDate, isPrimary, votingRights, isTemporary, notes?} → 201, status starts Current. 409 "Governance.DuplicatePrimaryAssignment" if this person already has a primary current assignment elsewhere — explain in the UI that a person can only have ONE primary role at a time (non-primary/secondary assignments are unlimited).
- PUT /api/governance/assignments/{id} {positionTitle?, isPrimary, votingRights, isTemporary, notes?} → 200 (same 409 rule applies)
- POST /api/governance/assignments/{id}/end {endDate?} → {id, status, endDate} — terminal, ends the assignment (defaults endDate to today).
Create form: person combobox, org body combobox, role combobox (optionally filtered to the role's defaultBodyId), start date, primary/voting/temporary checkboxes.

MEETINGS:
- GET /api/governance/meetings?bodyId=&meetingType=&status=&from=&to= → [{id, orgBodyId, orgBodyName, meetingType, meetingDate, status, hasMinutes}]
- GET /api/governance/meetings/{id} → +{mode, calledBy?, chairPersonId?, chairPersonName?, secretaryPersonId?, secretaryPersonName?, quorumRequired?, decisionThreshold?, publicationDeadline?, notes?, participantCount, hasMinutes}
- POST /api/governance/meetings {orgBodyId, meetingType, meetingDate, mode, calledBy?, chairPersonId?, secretaryPersonId?, publicationDeadline?, notes?} → 201, status starts Scheduled. quorumRequired/decisionThreshold are auto-copied from the body (don't send them) and publicationDeadline defaults to meetingDate+10 days if omitted.
- PUT /api/governance/meetings/{id} {meetingType, meetingDate, mode, calledBy?, chairPersonId?, secretaryPersonId?, status, publicationDeadline?, notes?} → 200. status is a select: Scheduled/Held/Cancelled/Postponed — marking a meeting Held is what unlocks recording minutes.
- GET /api/governance/meetings/{id}/quorum → {meetingId, eligibleCount, presentCount, countsForQuorumPresentCount, presentPercentage?, quorumRequired?, decisionThreshold?}. Show as a stat strip on the meeting detail page: "X of Y eligible present (Z%)" alongside the raw policy text — do not compute a pass/fail verdict, the org's rule is often conditional prose.
meetingType ← lookup "meeting_type"; mode ← "meeting_mode".

MEETING PARTICIPANTS (roster + attendance/voting):
- GET /api/governance/meetings/{meetingId}/participants → [{personId, personFullName, roleInMeeting?, attendanceStatus, votingRight, countsForQuorum}]
- POST /api/governance/meetings/{meetingId}/participants {personId, assignmentId?, roleInMeeting?, attendanceStatus, votingRight, countsForQuorum, participationMode?, remarks?} → 201. 409 if already added; 400 "Governance.AssignmentPersonMismatch" if the chosen assignmentId doesn't belong to the chosen person — when a person combobox is selected, filter the assignment dropdown to that person's assignments only to avoid this.
- DELETE /api/governance/meetings/{meetingId}/participants/{personId} → 204
attendanceStatus ← lookup "attendance_status"; roleInMeeting ← "meeting_role"; participationMode ← "meeting_mode".

MINUTES + DECISIONS (minutes are 1:1 per meeting; decisions are a separate list under a minutes record):
- GET /api/governance/meetings/{meetingId}/minutes → {id, meetingId, preparedByPersonId?, preparedByPersonName?, approvedByPersonId?, approvedByPersonName?, summary?, nextMeetingDate?, documentLink?, publicationStatus, decisionCount}
- POST /api/governance/meetings/{meetingId}/minutes {preparedByPersonId?, approvedByPersonId?, summary?, nextMeetingDate?, documentLink?} → 201, status starts Draft. 400 "Governance.MeetingNotHeld" if the meeting isn't Held yet; 409 "Governance.MinutesAlreadyExist" if minutes already exist — route the user to edit instead.
- PUT /api/governance/meetings/{meetingId}/minutes same + {publicationStatus} → 200. publicationStatus select: Draft/Submitted/Approved/Published/Returned. 409 "Governance.MinutesPublished" once Published — lock the edit form and show a read-only view instead.
- GET /api/governance/minutes/{minutesId}/decisions → [{id, decisionText, actionPoints?, responsiblePersonId?, responsiblePersonName?, dueDate?, decisionStatus}]
- POST /api/governance/minutes/{minutesId}/decisions {decisionText, actionPoints?, responsiblePersonId?, dueDate?, decisionStatus, notes?} → 201. Also 409 once minutes are Published.
- PUT /api/governance/decisions/{id} same → 200
decisionStatus ← lookup "decision_status" (OPEN/IN_PROGRESS/COMPLETED/CANCELLED).

Screens:
1. /governance/bodies — hierarchy tree or indented list (use parentBodyId to nest), body type badges, click into detail showing policy fields + child bodies + "View current members" (→ bodies/{id}/members) + "Meetings" tab (→ meetings?bodyId=).
2. /governance/people — list/detail/form as usual; person detail shows an "Assignments" tab (assignments?personId=) and a "Meetings attended" tab if you have time.
3. /governance/roles — simple list/detail/form.
4. Assignment management lives on the Person detail page and the OrgBody detail page (both link to a shared "New assignment" dialog prefilling whichever side you came from).
5. /governance/meetings — list (filters: body, type, status, date range) → detail page with tabs: Overview (quorum stat strip + policy text), Participants (roster + add-participant dialog), Minutes (create/edit/publish flow, nested Decisions list with add/edit).
6. Governance summary widget on the main dashboard (optional) or its own /governance report page: GET /api/reports/governance-summary?from=&to= → [{orgBodyId, orgBodyName, currentMemberCount, meetingsHeld, minutesPublished, minutesPending, openDecisions}].

Add "Governance" to the sidebar nav group list from Prompt 2 (Admin-focused, but readable by everyone).
```

---

## Endpoint coverage checklist

Every API endpoint appears in exactly one prompt: Auth 7 (P0/P2, register in P13), Users 3 (P13), Lookups 5 (P0 read, P13 manage), Donors 4 (P4), Donations 4 (P5), Other Income 4 (P6), Expenses 4 (P6), Funds 1 + Buckets 2 (P7), Finance reports 7 (P3/P7), Programs 5 / Projects 4 / Activities 4 (P8), Participants 4 + Roster 3 + Distributions 4 + Program reports 2 (P9/P3), Partners 5 (P10), Volunteers 5 + Volunteer roster 3 (P10), Sponsorships 5 + report 1 (P11), Zakat 6 (P12), **Governance 35** (P14: People 5, OrgBodies 6, Roles 4, Assignments 5, Meetings 5, MeetingParticipants 3, Minutes 3, Decisions 3, governance-summary report 1) — **129 endpoints** as of Sprint 5. (Donor Engagements — Create/Update/List, 3 endpoints — shipped in the same sprint as Governance but is not yet covered by a dedicated prompt; fold it into Prompt 4 alongside Donors when picking this playbook back up.)
