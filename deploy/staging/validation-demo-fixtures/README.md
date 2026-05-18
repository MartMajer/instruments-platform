# Validation Demo Fixtures

These fixtures seed three proof-only demo tenants for validator walkthroughs:

- `validation-oh-research`: synthetic occupational-health and ergonomics proof flows.
- `validation-se-education`: synthetic student-experience and research proof flows.
- `validation-osh-consulting`: synthetic work-safety education and consulting proof flows.

The catalog is not a platform instrument library. Instruments are private tenant demo content, are not available to other tenants, and are not claimed as official, validated, legal-compliance, GDPR, medical, or work-safety advice.

VAL08 raised the fixture depth floor: each committed synthetic instrument now
has at least eight questions, and each tenant has story metadata for its main
proof series, linked-wave series, named campaign states, and response profiles.
The seed script validates and uses that metadata so fresh demo data reads as
occupational-health, education, and OSH proof data rather than generic seed
labels.

ORG01 adds synthetic directory hierarchy to the same normal tenants. Each
validation tenant now seeds subjects, groups, memberships, and manager links
through the `/app/directory` product APIs so demos can show hierarchy context
without a separate demo-only surface.

SEEDUX01 marks the seeded starter campaign series as read-only sample studies.
They remain normal tenant-private rows created through the regular APIs and
then labelled with `study_kind=sample`: there is no `/app/demo` fixture surface.
SEEDUX02 extends each tenant's starter set into four product-visible sample
states so the normal app can teach the first-session path: setup readiness,
collection in progress, results-ready mixed lifecycle, and longitudinal waves.
The seed runner validates that committed story metadata covers those states
before it talks to the API, and live seed runs assert that home plus Studies
surface the four sample series as read-only starter content.

Use the private
[validation demo walkthrough packet](../../../docs/v2/80-agent-handoff/validation-demo-walkthrough-packet.md)
for tenant selection, Auth0 smoke expectations, route order, validation
questions, and do-not-claim guardrails.

## Run Locally

Start the local staging stack first:

```powershell
deploy/staging/start-local-staging.ps1
```

Validate the fixture catalog without touching the database or API:

```powershell
deploy/staging/seed-validation-demo.ps1 -ValidateOnly
```

Validate the committed auth-user example without touching the database:

```powershell
deploy/staging/bootstrap-validation-demo-auth.ps1 `
  -UsersFile deploy/staging/validation-demo-fixtures/validation-demo-auth-users.example.json `
  -ValidateOnly `
  -AllowPlaceholderEmails
```

For Auth0-backed staging, copy the example auth-user file to the ignored local
file, replace placeholder identities with owner-controlled Auth0 test
identities, and validate it:

```powershell
Copy-Item `
  deploy/staging/validation-demo-fixtures/validation-demo-auth-users.example.json `
  deploy/staging/validation-demo-fixtures/validation-demo-auth-users.local.json

deploy/staging/bootstrap-validation-demo-auth.ps1 -ValidateOnly
```

Bootstrap tenant rows plus platform auth memberships:

```powershell
deploy/staging/bootstrap-validation-demo-auth.ps1
```

The auth bootstrap creates only platform `permission`, `role`,
`role_permission`, `user_account`, and tenant-scoped `role_assignment` rows.
It does not create `external_auth_identity`; AUTH02 creates that binding on
first verified Auth0 login.

Seed tenant-private demo flows after auth memberships are ready:

```powershell
deploy/staging/seed-validation-demo.ps1
```

The seed runner is intentionally not append-idempotent. By default it refuses
to seed when the validation tenants already contain demo instruments,
campaign-series, campaigns, response sessions, or export artifacts. Reset the
disposable staging database before reseeding, or pass `-AllowDuplicateSeed`
only when deliberately testing duplicate demo-data behavior:

```powershell
deploy/staging/seed-validation-demo.ps1 -AllowDuplicateSeed
```

For Auth0-backed browser walkthroughs, `PUBLIC_TENANT_ID` selects one validation
tenant at a time. A role slot from another tenant should fail cleanly until the
operator switches `PUBLIC_TENANT_ID` in the ignored local `.env` file and
rebuilds/restarts the local staging shape. Prefer the helper so secrets and
unrelated values stay preserved:

```powershell
deploy/staging/select-validation-demo-tenant.ps1 validation-oh-research -Restart
deploy/staging/select-validation-demo-tenant.ps1 validation-se-education -Restart
deploy/staging/select-validation-demo-tenant.ps1 validation-osh-consulting -Restart
```

The helper edits only the ignored local staging `.env` file. It is an operator
convenience for selecting the normal app tenant used by the browser, not a
product tenant-switching feature.

Before a live walkthrough, run the read-only preflight for the tenant you plan
to show:

```powershell
deploy/staging/smoke-validation-demo-preflight.ps1 validation-oh-research
deploy/staging/smoke-validation-demo-preflight.ps1 validation-se-education
deploy/staging/smoke-validation-demo-preflight.ps1 validation-osh-consulting
```

For static preparation without a running local stack, use:

```powershell
deploy/staging/smoke-validation-demo-preflight.ps1 validation-oh-research -SkipLiveChecks
```

For a remote staging endpoint check from an operator machine, use `-RemoteOnly`
with explicit public origins. This skips local `.env`, auth role-slot file,
tenant-switch helper, and database-count checks, then verifies only read-only
API/web/session/CORS/login redirect behavior:

```powershell
deploy/staging/smoke-validation-demo-preflight.ps1 validation-oh-research `
  -RemoteOnly `
  -ApiOrigin https://staging-api.example.com `
  -WebOrigin https://staging.example.com
```

The preflight validates selected tenant, URL, fixture catalog, and auth role-slot
shape, then optionally checks live API/web/session/CORS/login redirect behavior
and non-sensitive selected-tenant counts. It also prints the self-serve
walkthrough contract for the selected tenant: home, Studies, setup sample,
collection sample, results sample, longitudinal sample, `Duplicate as study`
for setup-manager roles, and read-only analyst/viewer sample inspection. It is
read-only: it does not create Auth0 users, mutate Auth0 or VPS resources, seed
data, duplicate sample studies, or approve real-person data.

If tenant rows are already present in the target database, skip direct database bootstrap and seed through the API only:

```powershell
deploy/staging/seed-validation-demo.ps1 -SkipTenantBootstrap
```

The script prints owner inspection routes and created resource IDs only. It must not print open-link tokens, participant codes, invitation tokens, salts, raw answers, secrets, or real validator email addresses.

## Boundaries

- Use only synthetic or owner-controlled data.
- Keep proof runs on local staging or the current demo VPS until production hosting and compliance decisions are closed.
- Do not invite real participants from these fixtures until Q-053 is closed for that concrete tenant and use case.
- Keep `validation-demo-auth-users.local.json` untracked. It may contain owner-controlled Auth0 test identities for proof runs only.
- Treat all seeded instruments as tenant-private proof content, not canonical platform instruments.
