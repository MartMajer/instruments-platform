# Registration Onboarding Design

Date: 2026-05-18

## Decision

Build private-beta registration as a gated tenant bootstrap flow. The product gets a public `/register` path, but a valid beta access code is required before Auth0 is invoked. Auth0 remains the identity provider only. The platform database creates and owns tenants, roles, permissions, owner membership, external identity binding, and app session state.

## Goals

- Let a new beta user create the first tenant/workspace without manual database seeding.
- Preserve the existing Auth0/OIDC boundary: no Auth0 Organizations, no Auth0 roles, no Auth0 tenant authority.
- Preserve platform-owned tenant/RBAC semantics.
- Prevent fully open public signup while the product is still private beta.
- Keep Q-053 and Q-054 visible: no real-person production legal/GDPR/DPA claim and no outbound operational email workflow.

## Non-goals

- No public self-serve launch without access code.
- No invitation email or operational notification email.
- No Auth0 Management API, Auth0 Organizations, SCIM, enterprise SSO, or paid Auth0 plan dependency.
- No billing, plan selection, contracts, DPA acceptance, or production real-person data approval.
- No platform-canonical named-instrument rights claim.

## User flow

1. User opens `/register` from the landing page.
2. User enters email, organization/workspace name, and beta access code.
3. Frontend posts to `POST /registration/intents`.
4. API validates registration is enabled, validates the access code against configured hashes, normalizes email/name, creates a short-lived pending registration intent, and returns an Auth0 login URL.
5. Browser redirects to `/auth/login?registrationToken=...&returnUrl=/app`.
6. Auth0 handles signup/login and verified email proof.
7. OIDC callback resolves the registration token, requires the verified provider email to match the pending intent, creates the tenant bootstrap records, consumes the intent, projects app-cookie claims, and redirects to `/app`.
8. `/app` loads as the new tenant owner.

## Backend architecture

Add `RegistrationIntent` as a pre-tenant platform table. It stores a hashed one-time registration token, normalized email, organization name, requested slug, status, timestamps, expiry, consumed tenant id, and consumed timestamp. It must not store raw access codes or raw registration tokens.

Add `POST /registration/intents` as an anonymous endpoint. It is available only when `Registration:Enabled=true`. It validates access codes using constant-time comparison against configured SHA-256 hashes. It rate-limits by client IP and normalized email. It returns only a login URL and expiry metadata.

Extend `/auth/login` to allow exactly one login context: existing `tenantId` for existing tenant login, or `registrationToken` for new tenant registration. The existing tenant login path remains unchanged.

Extend `PlatformOidcEvents.TokenValidated` with a registration path. If the OIDC properties contain a registration token, the callback uses a registration resolver instead of the existing tenant login resolver. The resolver consumes the intent transactionally and returns the same `PlatformOidcLoginResolution` shape as normal login.

Tenant bootstrap creates:

- `tenant` with unique slug and active status.
- global permissions if missing: `setup.manage`, `team.manage`, `export.read`.
- tenant roles: `tenant_owner`, `researcher`, `analyst`, `viewer`.
- role permissions: owner gets setup/team/export; researcher gets setup; analyst gets export; viewer gets no privileged permission.
- owner `user_account` with normalized email.
- owner tenant-scoped `role_assignment`.
- `external_auth_identity` with provider-subject hash only.
- `auth_session` with configured expiry.

## Frontend architecture

Add `apps/web/src/routes/register/+page.svelte` as a production-facing private-beta registration page using the current landing visual language. The form collects email, workspace name, and access code. On success it redirects to the returned login URL. Error copy should be plain and product-facing, not internal proof/smoke wording.

Add a small registration API helper under `apps/web/src/lib/api/registration.ts`. Keep credentials/default headers consistent with the existing API client, but do not send dev-auth headers from the public registration path.

Update the landing CTA to point to `/register`. Existing sign-in should remain for already-provisioned tenant users.

## Security and abuse controls

- Registration is disabled by default unless explicitly enabled by configuration.
- Beta access code values are configured as hashes, not raw codes.
- Raw access code and raw registration token are never persisted or logged.
- Registration token is one-time and expires quickly.
- Callback requires verified provider email and exact normalized email match with the intent.
- Callback rejects consumed, expired, missing, or mismatched intents.
- Tenant slug is normalized and unique; collision handling must not cross tenant boundaries.
- OIDC state/properties carry registration context; browser JavaScript never sees provider tokens.
- Public endpoint is rate-limited.

## Boundary wording

The UI may say private beta, EU-hosted staging/beta, and demo/test-data boundaries. It must not claim GDPR/DPA/legal readiness for real participant/student/employee/patient/customer data. It must not imply operational events are emailed. It must not claim official platform-shipped validated instruments.

## Acceptance criteria

- A user with a valid beta access code and verified matching Auth0 email can create a new tenant and land in `/app` as tenant owner.
- Existing `/auth/login?tenantId=...` behavior still works.
- Registration fails closed when disabled, access code is invalid, token is expired/consumed, provider email is unverified, or provider email does not match the intent.
- Tenant/RBAC bootstrap is platform-owned and queryable through existing `/auth/session`, `/tenant-settings`, `/tenant-members`, and `/tenant-roles` surfaces.
- No Auth0 Organizations/roles, outbound email workflow, or Q-053/Q-054 claim is added.