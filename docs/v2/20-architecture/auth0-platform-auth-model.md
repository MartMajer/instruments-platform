# Auth0 and platform auth model

Last updated: 2026-05-19.

## Decision

Auth0 proves browser identity. The platform owns workspaces, tenant membership,
roles, permissions, app sessions, and every authorization decision.

The browser must never rely on a seeded/default tenant to sign in. Every normal
workspace login must carry explicit workspace context through `tenantId` or
through the beta workspace lookup endpoint.

## Auth0 responsibilities

- Collect password, social/enterprise login, and MFA.
- Return OIDC claims such as provider subject, email, and `email_verified`.
- Maintain the external identity-provider session.

Auth0 does not decide which platform tenant the user can open. The app does not
query Auth0 to decide whether an email exists before registration.

## Platform responsibilities

- Normalize and store platform emails for workspace members.
- Store tenants, user accounts, role assignments, external Auth0 identity
bindings, and app auth sessions.
- Resolve an Auth0 callback only when the returned email and provider subject
match the requested tenant context.
- Fail closed when tenant context is missing, ambiguous, or does not match a
platform membership.
- Keep raw tokens, provider subjects, session cookies, and secrets out of logs.

## New workspace registration

1. User opens `/register`.
2. User enters email, workspace name, and private beta access code.
3. `POST /registration/intents` validates the beta code and blocks duplicate
   active platform workspace membership for that normalized email.
4. The API returns an Auth0 signup URL with registration context and
   `login_hint`.
5. Auth0 creates or selects the identity account and returns to the platform.
6. The platform stores a pending registration identity in the app cookie.
7. User finishes the workspace form if needed.
8. `POST /registration/workspaces` creates the tenant, owner account, owner role
   assignment, external Auth0 identity binding, and initial app session.
9. The browser stores the created tenant id and workspace email locally so later
   sign-in targets the same workspace.

Unverified email is allowed only for the initial registration grace session.
After sign-out, normal workspace sign-in requires Auth0 to return
`email_verified=true`.

## Existing workspace sign-in

1. User opens `/signin` or a generic home Sign in link with no remembered
   workspace.
2. User enters the workspace email.
3. `POST /registration/workspace-sign-in` validates the email, uses the
   registration email lookup RLS guard, finds the active platform workspace, and
   returns a tenant-scoped `/auth/login` URL with `prompt=login` and
   `login_hint`.
4. Auth0 authenticates the user.
5. The platform callback resolves the email, provider subject, tenant
   membership, and role permissions before issuing the app cookie.

If the email has no active workspace membership, the user is told to create a
workspace first. If one email later belongs to multiple workspaces, this beta
endpoint must become a workspace picker instead of returning the first tenant.

## Account switching and sign-out

App sign-out clears the platform cookie and can also call provider logout when
the user needs to escape a stale Auth0 browser session. Recovery links use
`prompt=login` and `login_hint` where possible because Auth0 may otherwise reuse
the wrong browser account.

## Current beta limits

- One active workspace membership per normalized email.
- No multi-workspace picker yet.
- No Auth0 Management API dependency for registration validation.
- Browser local storage remembers only the last tenant id and workspace email as
  a recovery convenience, not as an authorization source.
- All server authorization remains tenant-scoped and membership-backed.
