# Registration email verification grace design

Date: 2026-05-19

## Decision

Use a one-session grace path for new workspace registration.

A valid registration-token callback may create the workspace and first app session even when Auth0 returns `email_verified=false`. The app then shows an in-app verification reminder. After the user signs out or the first session expires, normal sign-in requires `email_verified=true`.

## Why

The strict flow forced users through Auth0 signup, email verification, and a second registration-token login. That exposed implementation details as `Finish workspace setup` and created stale-session/account-switch pain. Creating the workspace during the first registration callback gives a normal signup experience while still enforcing verified email before continued access.

## Flow

1. User submits email, workspace name, and beta code.
2. API creates a registration intent and redirects to Auth0 signup.
3. Auth0 callback with a valid registration token creates the tenant, owner user, provider binding, and first app session even if the email is not verified.
4. The app opens the workspace and shows a persistent verify-email banner when the session identity is unverified.
5. Later normal tenant login rejects unverified provider email with `auth=email_unverified`.
6. A later normal tenant login with `email_verified=true` updates the stored provider binding to verified and allows access.

## Data model

Add verification state to `external_auth_identity`:

- `email_verified_at` nullable timestamp.
- `email_verification_grace_used_at` nullable timestamp.

The first registration callback sets `email_verification_grace_used_at` when the provider email is unverified. A verified callback sets `email_verified_at`.

## Error handling

- Registration-token mismatch still fails closed and does not fall back to a stale app session.
- Duplicate workspace email still returns `registration.email_exists`.
- Normal sign-in with unverified email fails with `auth=email_unverified` and tells the user to verify email before signing back in.
- The old pending-registration `Finish workspace setup` state should be removed or narrowed to true interrupted registration cases only.

## Tests

- Registration-token callback with `email_verified=false` creates workspace and session.
- Normal tenant login with `email_verified=false` is rejected when the identity is not verified.
- Normal tenant login with `email_verified=true` records verification and signs in.
- App session response exposes whether email verification is required.
- Web app shows an in-app verification banner for an unverified active session.
