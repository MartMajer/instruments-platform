# Registration Onboarding Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a private-beta registration flow that lets a verified Auth0 user create a new tenant and become tenant owner while keeping tenant/RBAC authority inside the platform database.

**Architecture:** Add a short-lived pre-tenant registration intent, gate intent creation with configured beta access-code hashes, then let the OIDC callback consume the intent and bootstrap tenant/RBAC/session records transactionally. Keep existing tenant login unchanged and route the registration callback through a separate resolver path.

**Tech Stack:** ASP.NET Core minimal APIs, OpenID Connect events, EF Core/PostgreSQL migrations, SvelteKit, Playwright/Svelte tests, existing Result/MediatR patterns where applicable.

---

### Task 1: Add registration intent domain and EF mapping

**Files:**
- Create: `src/Platform.Domain/Auth/RegistrationIntent.cs`
- Modify: `src/Platform.Infrastructure/Data/ApplicationDbContext.cs`
- Create: `src/Platform.Infrastructure/Migrations/20260518193000_AddRegistrationOnboarding.cs`
- Modify: `src/Platform.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`
- Test: `tests/Platform.UnitTests/Domain/AuthEntitiesTests.cs`

**Steps:** write failing domain tests; implement `RegistrationIntent`; add EF mapping and migration; verify targeted unit tests.

### Task 2: Add registration options, access-code verifier, token hasher, and intent store

**Files:**
- Create: `src/Platform.Api/Registration/RegistrationOptions.cs`
- Create: `src/Platform.Api/Registration/BetaAccessCodeVerifier.cs`
- Create: `src/Platform.Api/Registration/RegistrationTokenProtector.cs`
- Create: `src/Platform.Api/Registration/RegistrationIntentStore.cs`
- Modify: API service-registration file that currently registers auth services
- Test: `tests/Platform.IntegrationTests/Api/RegistrationEndpointTests.cs`

**Steps:** test disabled registration, invalid code, valid code, raw-code non-persistence, and login URL shape; implement config defaults; implement constant-time SHA-256 code verification; implement one-time token generation/storage; implement intent store.

### Task 3: Add anonymous registration endpoint

**Files:**
- Create: `src/Platform.Application/Features/Registration/CreateRegistrationIntent.cs`
- Create: `src/Platform.Application/Features/Registration/RegistrationEndpointRouteBuilderExtensions.cs`
- Modify: `src/Platform.Api/Program.cs` or existing endpoint composition file
- Test: `tests/Platform.IntegrationTests/Api/RegistrationEndpointTests.cs`

**Steps:** test `POST /registration/intents`; implement request/response contracts; map anonymous rate-limited endpoint; return ProblemDetails-style failures.

### Task 4: Extend `/auth/login` for registration context

**Files:**
- Modify: `src/Platform.Api/Auth/AuthEndpointRouteBuilderExtensions.cs`
- Test: `tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs`

**Steps:** test `registrationToken` without `tenantId`, reject both/neither, preserve tenant login and prompt validation; implement exact-one context validation and store registration token in OIDC properties.

### Task 5: Bootstrap tenant/RBAC during OIDC callback

**Files:**
- Modify: `src/Platform.Api/Auth/PlatformOidcEvents.cs`
- Create: `src/Platform.Api/Registration/PlatformRegistrationLoginResolver.cs`
- Modify: `src/Platform.Api/Auth/PlatformAuthServiceCollectionExtensions.cs`
- Test: `tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs`
- Test: `tests/Platform.IntegrationTests/Infrastructure/RegistrationOnboardingStoreTests.cs`

**Steps:** test registration resolver path, tenant login preservation, missing/invalid context, unverified email, email mismatch, consumed/expired intent, and successful claim projection; implement transactional tenant/RBAC/session bootstrap; set tenant DB scope before tenant-scoped writes; avoid EF bulk operations.

### Task 6: Add registration frontend API and page

**Files:**
- Create: `apps/web/src/lib/api/registration.ts`
- Create: `apps/web/src/lib/api/registration.test.ts`
- Create: `apps/web/src/routes/register/+page.svelte`
- Add route test consistent with existing `apps/web/src/routes/*.e2e.ts` tests
- Modify: `apps/web/src/routes/+page.svelte`
- Modify: `apps/web/src/routes/app.css`

**Steps:** test request payload/no dev-auth headers/success redirect/error display/copy boundaries; implement API helper; implement polished `/register` page; update landing CTA.

### Task 7: Add operational docs and handoff

**Files:**
- Modify: `docs/v2/20-architecture/auth-and-authz.md`
- Modify: `docs/v2/40-ops/vps-staging-runbook.md`
- Modify: `docs/v2/80-agent-handoff/SESSION-LOG.md`
- Modify: `docs/v2/80-agent-handoff/NEXT-ACTIONS.md`
- Modify: `docs/v2/80-agent-handoff/OPEN-QUESTIONS.md` only if a new blocker is discovered

**Steps:** document private-beta registration boundary, Auth0 identity-only constraint, hashed access-code config, Q-053/Q-054 limits, verification, and remaining risks.

### Task 8: Final verification and completion audit

**Files:** no new files unless verification exposes gaps.

**Steps:** run focused backend registration/auth tests; run `dotnet build --no-restore`; run web `svelte-check` and `vite build`; audit objective against real evidence; commit scoped files.