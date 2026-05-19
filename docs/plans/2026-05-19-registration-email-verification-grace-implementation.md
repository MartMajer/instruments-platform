# Registration Email Verification Grace Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Let the first registration callback create and open the workspace even when Auth0 email is not verified, then require verified email for later normal sign-ins.

**Architecture:** Persist provider email verification state on `external_auth_identity`. Registration-token callbacks get a one-session grace path; normal tenant login rejects unverified identities unless Auth0 now reports `email_verified=true`, in which case the binding is marked verified and login continues. `/auth/session` exposes verification state so the web app can show an in-app banner without blocking the first session.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core/PostgreSQL migrations/RLS, Auth0 OIDC events, SvelteKit frontend, xUnit integration tests, Playwright route tests.

---

### Task 1: Persist external identity email verification state

**Files:**
- Modify: `src/Platform.Domain/Auth/ExternalAuthIdentity.cs`
- Modify: `src/Platform.Infrastructure/Data/ApplicationDbContext.cs`
- Create: `src/Platform.Infrastructure/Migrations/20260519183000_AddExternalAuthEmailVerification.cs`
- Test: `tests/Platform.UnitTests/Domain/AuthEntitiesTests.cs`
- Test: `tests/Platform.IntegrationTests/Infrastructure/ApplicationDbContextModelTests.cs` if model metadata tests already cover auth entities.

**Step 1: Write the failing domain test**

Add tests proving:

```csharp
var now = DateTimeOffset.UtcNow;
var identity = new ExternalAuthIdentity(
    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "auth0", "hash", "owner@example.test", now);

Assert.Null(identity.EmailVerifiedAt);
Assert.Null(identity.EmailVerificationGraceUsedAt);

identity.RecordEmailVerificationGrace(now);
Assert.Equal(now, identity.EmailVerificationGraceUsedAt);
Assert.Null(identity.EmailVerifiedAt);

identity.RecordEmailVerified(now.AddMinutes(1));
Assert.Equal(now.AddMinutes(1), identity.EmailVerifiedAt);
```

Also assert repeated calls do not move `EmailVerificationGraceUsedAt` backward and do not clear `EmailVerifiedAt`.

**Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests\Platform.UnitTests\Platform.UnitTests.csproj --filter "FullyQualifiedName~AuthEntitiesTests"
```

Expected: fails because `ExternalAuthIdentity` has no verification properties/methods.

**Step 3: Implement domain + EF mapping**

In `ExternalAuthIdentity` add:

```csharp
public DateTimeOffset? EmailVerifiedAt { get; private set; }

public DateTimeOffset? EmailVerificationGraceUsedAt { get; private set; }

public bool IsEmailVerified => EmailVerifiedAt.HasValue;

public void RecordEmailVerified(DateTimeOffset verifiedAt)
{
    EmailVerifiedAt ??= verifiedAt;
    LastSeenAt = verifiedAt;
}

public void RecordEmailVerificationGrace(DateTimeOffset graceUsedAt)
{
    EmailVerificationGraceUsedAt ??= graceUsedAt;
    LastSeenAt = graceUsedAt;
}
```

In `ApplicationDbContext` external identity mapping add:

```csharp
builder.Property(identity => identity.EmailVerifiedAt).HasColumnName("email_verified_at");
builder.Property(identity => identity.EmailVerificationGraceUsedAt).HasColumnName("email_verification_grace_used_at");
```

Create migration:

```csharp
migrationBuilder.AddColumn<DateTimeOffset>(
    name: "email_verified_at",
    table: "external_auth_identity",
    type: "timestamp with time zone",
    nullable: true);

migrationBuilder.AddColumn<DateTimeOffset>(
    name: "email_verification_grace_used_at",
    table: "external_auth_identity",
    type: "timestamp with time zone",
    nullable: true);
```

**Step 4: Run test to verify it passes**

Run the same unit test filter.

Expected: pass.

**Step 5: Commit**

```powershell
git add src/Platform.Domain/Auth/ExternalAuthIdentity.cs src/Platform.Infrastructure/Data/ApplicationDbContext.cs src/Platform.Infrastructure/Migrations/20260519183000_AddExternalAuthEmailVerification.cs tests/Platform.UnitTests/Domain/AuthEntitiesTests.cs
git commit -m "feat(auth): persist email verification state"
```

---

### Task 2: Allow registration-token grace but enforce verified email for normal tenant login

**Files:**
- Modify: `src/Platform.Api/Auth/PlatformOidcEvents.cs`
- Modify: `src/Platform.Api/Auth/EfPlatformRegistrationLoginResolver.cs`
- Test: `tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs`
- Test: `tests/Platform.IntegrationTests/Api/RegistrationLoginResolverTests.cs`

**Step 1: Write failing callback tests**

In `AuthEndpointTests`, add/adjust tests:

```csharp
[Fact]
public async Task Oidc_token_validation_allows_unverified_email_for_registration_token()
{
    var registrationResolver = new FakeRegistrationLoginResolver
    {
        Resolution = new PlatformOidcLoginResolution(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [PlatformPermissions.SetupManage])
    };
    var events = CreateOidcEvents(new FakeOidcLoginResolver(), registrationResolver: registrationResolver);
    var context = CreateTokenValidatedContext(
        "owner@example.test",
        emailVerified: false,
        registrationToken: "registration-token");

    await events.TokenValidated(context);

    Assert.Null(context.Result?.Failure);
    Assert.Single(registrationResolver.Calls);
}
```

Keep existing normal tenant login test `Oidc_token_validation_rejects_unverified_email_by_default` green: unverified normal login must still fail with `email_unverified`.

**Step 2: Run tests to verify failure**

Run:

```powershell
dotnet test tests\Platform.IntegrationTests\Platform.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests.Oidc_token_validation_allows_unverified_email_for_registration_token|FullyQualifiedName~AuthEndpointTests.Oidc_token_validation_rejects_unverified_email_by_default"
```

Expected: new registration-token test fails because `TokenValidated` currently rejects all unverified email before checking registration-token grace.

**Step 3: Change `PlatformOidcEvents.TokenValidated`**

Keep email claim and subject validation before verification check. Change verification enforcement to:

```csharp
var emailVerified = IsEmailVerified(context.Principal);
if (RequiresVerifiedEmail() && !emailVerified && !hasRegistrationLogin)
{
    logger.LogWarning("OIDC login rejected because the email claim was not verified.");
    MarkAuthFailure(context, EmailUnverifiedFailureReason);
    context.Fail("platform_login_verified_email_required");
    return;
}
```

Pass verification state to resolvers. Update interfaces and fake resolvers:

```csharp
Task<PlatformOidcLoginResolution?> ResolveAsync(
    Guid tenantId,
    string email,
    bool emailVerified,
    string provider,
    string providerSubject,
    CancellationToken cancellationToken);

Task<PlatformOidcLoginResolution?> ResolveAsync(
    string registrationToken,
    string email,
    bool emailVerified,
    string provider,
    string providerSubject,
    CancellationToken cancellationToken);
```

**Step 4: Update normal login resolver behavior**

In `EfPlatformOidcLoginResolver.ResolveAsync`, after loading/creating the binding:

```csharp
if (!emailVerified && binding is not null && !binding.IsEmailVerified)
{
    return null;
}

if (emailVerified)
{
    binding.RecordEmailVerified(now);
}
else
{
    binding.RecordSeen(now);
}
```

Do not create a new normal-login binding for an unverified provider email.

**Step 5: Update registration resolver behavior**

In `EfPlatformRegistrationLoginResolver.ResolveAsync`, after creating the binding:

```csharp
if (emailVerified)
{
    binding.RecordEmailVerified(now);
}
else
{
    binding.RecordEmailVerificationGrace(now);
}
```

Registration-token mismatched email must still return null and consume nothing.

**Step 6: Add Docker-backed registration test**

In `RegistrationLoginResolverTests`, add a variant of `ResolveAsync_consumes_pending_intent_and_bootstraps_tenant_owner` for unverified registration. Assert:

```csharp
Assert.Null(binding.EmailVerifiedAt);
Assert.NotNull(binding.EmailVerificationGraceUsedAt);
Assert.NotNull(resolution);
```

Add another focused normal-login resolver test if practical: seed an unverified binding and assert normal resolver returns null when `emailVerified:false`, then returns a resolution and sets `EmailVerifiedAt` when `emailVerified:true`.

**Step 7: Run focused tests**

Run:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS='1'; dotnet test tests\Platform.IntegrationTests\Platform.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests.Oidc_token_validation_allows_unverified_email_for_registration_token|FullyQualifiedName~AuthEndpointTests.Oidc_token_validation_rejects_unverified_email_by_default|FullyQualifiedName~RegistrationLoginResolverTests.ResolveAsync"
```

Expected: pass.

**Step 8: Commit**

```powershell
git add src/Platform.Api/Auth/PlatformOidcEvents.cs src/Platform.Api/Auth/EfPlatformRegistrationLoginResolver.cs tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs tests/Platform.IntegrationTests/Api/RegistrationLoginResolverTests.cs
git commit -m "fix(auth): allow first registration session before verification"
```

---

### Task 3: Expose verification warning through `/auth/session`

**Files:**
- Modify: `src/Platform.Application/Features/Auth/GetCurrentSession/GetCurrentSessionResponse.cs`
- Modify: `src/Platform.Application/Features/Auth/GetCurrentSession/GetCurrentSessionEndpoint.cs`
- Modify: `src/Platform.Api/Auth/PlatformSessionCookieEvents.cs` or session validator if that is the only session DB read seam.
- Modify: `apps/web/src/lib/api/setup.ts`
- Test: `tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs`
- Test: `apps/web/src/lib/api/setup.test.ts` if session shape is asserted.

**Step 1: Choose session-state source**

Preferred minimal source: add a claim during `ProjectPlatformClaims`:

```csharp
public const string EmailVerified = "email_verified";
```

When projecting claims, use `resolution.EmailVerified` and add:

```csharp
identity.AddClaim(new Claim(PlatformClaimTypes.EmailVerified, resolution.EmailVerified ? "true" : "false"));
```

This requires extending `PlatformOidcLoginResolution` with `bool EmailVerified = true`.

**Step 2: Write failing API/session test**

In `AuthEndpointTests`, add a test for `/auth/session` with a test auth principal containing `email_verified=false`, expecting JSON:

```json
{
  "emailVerificationRequired": true
}
```

If the test auth handler cannot project the claim, add a direct unit-style test for `GetCurrentSessionEndpoint` only if simpler.

**Step 3: Implement response shape**

Change `GetCurrentSessionResponse`:

```csharp
public sealed record GetCurrentSessionResponse(
    Guid UserId,
    Guid TenantId,
    string? Email,
    string[] Permissions,
    bool EmailVerificationRequired = false);
```

Change endpoint to read current actor/principal claim. If `ICurrentActor` does not expose it, add the property there and populate it from `PlatformClaimTypes.EmailVerified`.

Frontend type:

```ts
export type AuthSessionResponse = {
    userId: string;
    tenantId: string;
    email?: string | null;
    permissions: string[];
    emailVerificationRequired?: boolean;
};
```

**Step 4: Run focused backend/session tests**

Run:

```powershell
dotnet test tests\Platform.IntegrationTests\Platform.IntegrationTests.csproj --filter "FullyQualifiedName~AuthEndpointTests"
```

Expected: pass for touched auth tests.

**Step 5: Commit**

```powershell
git add src/Platform.Application/Features/Auth/GetCurrentSession src/Platform.Application/Auth src/Platform.Api/Auth apps/web/src/lib/api/setup.ts tests/Platform.IntegrationTests/Api/AuthEndpointTests.cs
git commit -m "feat(auth): expose email verification session state"
```

---

### Task 4: Replace pending setup recovery with active-session verification banner

**Files:**
- Modify: `apps/web/src/routes/app/+layout.svelte`
- Modify: `apps/web/src/routes/setup-workspace.e2e.ts`
- Optional modify: `apps/web/src/lib/product/session-profile.ts` if the session callout should include verification posture.

**Step 1: Write failing Playwright tests**

Add test:

```ts
test('shows verify-email banner for active unverified registration session', async ({ page }) => {
  await page.unroute('**/auth/session');
  await page.route('**/auth/session', async (route) => {
    await route.fulfill({ json: { ...sampleAuthSession, emailVerificationRequired: true } });
  });

  await page.goto('/app');

  await expect(page.getByRole('status', { name: 'Email verification required' })).toContainText(
    'Verify your email to keep access after signing out'
  );
  await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
});
```

Update failed-auth pending-registration test so it no longer talks about `Finish workspace setup` unless the pending URL exists because the first callback genuinely failed before creating a workspace.

**Step 2: Run focused test to verify failure**

Use local preview server pattern or normal config if `npm` is on PATH:

```powershell
cd apps\web
$env:PATH='D:\Program Files\nodejs;' + $env:PATH
node .\node_modules\@playwright\test\cli.js test src/routes/setup-workspace.e2e.ts -g "verify-email banner"
```

Expected: fails because no authenticated-session banner exists.

**Step 3: Implement banner**

In authenticated branch near session callout:

```svelte
{#if authSession.emailVerificationRequired}
  <div class="email-verification-reminder" role="status" aria-label="Email verification required">
    <div class="email-verification-reminder__icon" aria-hidden="true">!</div>
    <div class="email-verification-reminder__body">
      <h2 class="email-verification-reminder__title">Verify your email</h2>
      <p class="email-verification-reminder__text">
        Verify your Auth0 email to keep access after signing out. This workspace is open for your first registration session.
      </p>
    </div>
  </div>
{/if}
```

Keep the provider logout recovery for wrong-account failed-auth states, but remove misleading “finish setup” copy from normal post-signup path once backend grace makes it unnecessary.

**Step 4: Run focused Playwright and web build**

Run:

```powershell
cd apps\web
node .\node_modules\@sveltejs\kit\svelte-kit.js sync
node .\node_modules\vite\bin\vite.js build
node .\node_modules\@playwright\test\cli.js test src/routes/setup-workspace.e2e.ts -g "verify-email banner|failed workspace sign-in"
```

Expected: build passes; focused tests pass.

**Step 5: Commit**

```powershell
git add apps/web/src/routes/app/+layout.svelte apps/web/src/routes/setup-workspace.e2e.ts apps/web/src/lib/api/setup.ts
git commit -m "feat(web): show email verification banner in app"
```

---

### Task 5: Deploy and staging probes

**Files:**
- No source files unless deploy scripts need adjustment.

**Step 1: Push branches**

```powershell
git push origin main
git push origin main:staging
```

**Step 2: Deploy API/web/migrator**

Because this includes a migration, rebuild migrator too:

```powershell
ssh instruments-vps-codex "cd /opt/instruments-platform && git pull --ff-only && docker compose -f deploy/staging/docker-compose.yml -f deploy/staging/docker-compose.vps.yml --env-file deploy/staging/.env build migrator api web && docker compose -f deploy/staging/docker-compose.yml -f deploy/staging/docker-compose.vps.yml --env-file deploy/staging/.env up --force-recreate migrator && docker compose -f deploy/staging/docker-compose.yml -f deploy/staging/docker-compose.vps.yml --env-file deploy/staging/.env up -d --no-deps api web"
```

**Step 3: Verify staging basics**

```powershell
Invoke-WebRequest -UseBasicParsing -Uri 'https://validatedscale-api-staging.croat.dev/health/ready'
Invoke-WebRequest -UseBasicParsing -Uri 'https://validatedscale-staging.croat.dev/register'
Invoke-WebRequest -UseBasicParsing -Uri 'https://validatedscale-staging.croat.dev/app'
```

Expected: API health 200, web pages 200.

**Step 4: Manual browser validation**

Use a fresh email:

1. Open `/register`.
2. Enter email, workspace name, beta code.
3. Complete Auth0 signup/password.
4. Do not verify email yet.
5. Expected: app opens workspace and shows verify-email banner.
6. Sign out completely.
7. Try normal sign-in with same unverified email.
8. Expected: blocked with verify-email message.
9. Verify email in Auth0 email.
10. Sign in again.
11. Expected: app opens workspace without banner.

**Step 5: Commit deployment note if handoff docs are tracked/forced**

Append safe summary to `docs/v2/80-agent-handoff/SESSION-LOG.md`.

---
