# D348 - Auth onboarding product hardening assessment

Date: 2026-05-19

## Trigger

After staging sign-in and registration recovery stabilized, the remaining beta problem shifted from backend auth correctness to product clarity. The owner asked to document deferred auth decisions and then harden the next auth/onboarding surfaces: owner flow checklist, first-run app experience, team/member onboarding, and directory/hierarchy UX.

## Assessment

The auth model is now good enough for beta testing only if the user can understand which system owns which step:

- Auth0 owns account credentials, provider sessions, password reset, MFA, and email verification.
- Instrument Platform owns workspace reservation, owner/member records, tenant roles, and product access.
- Existing workspace sign-in is still beta one-email-one-workspace lookup.
- Multi-workspace accounts and cleaner account API naming are intentionally deferred as Q-056 and Q-057.

The app shell still had three practical clarity gaps:

- `/app` opened into the product without a clear first-run setup runway.
- `/app/team` exposed pending provider-link mechanics without clearly explaining that Auth0 must return the exact tenant email before activation.
- `/app/directory` showed subjects, groups, memberships, and manager links, but needed a stronger boundary between directory hierarchy and app authorization.

## Decision

Keep the current Auth0/platform split. Do not rename `/registration/workspace-sign-in` or implement a workspace picker in this slice. Instead, make the beta flow operable and testable by adding:

- A staging auth-flow checklist covering registration, sign-in, duplicate email, verification recovery, wrong-account recovery, and team-member first sign-in.
- First-run app setup actions on `/app`.
- Team-member onboarding steps and clearer pending-provider-link copy on `/app/team`.
- Directory setup-order guidance and explicit hierarchy-versus-app-role boundary copy on `/app/directory`.
- Focused Playwright coverage for the three product guidance surfaces plus the recent auth recovery paths.

## Verification

- Web production build passed through direct Node invocation.
- Focused Playwright run passed 8/8 against a local production preview:
  - generic home sign-in lookup
  - registration email-verification recovery
  - unverified workspace sign-in recovery
  - no-remembered-workspace verification recovery
  - existing workspace email lookup
  - first-run workspace runway
  - team member first sign-in guidance
  - directory hierarchy setup order

## Remaining risk

- Not deployed in this slice.
- Current existing-workspace lookup still assumes one active workspace per normalized email.
- Auth0 wrong-account recovery can still feel provider-session-dependent; staging owner testing remains the real proof.
- Directory/team guidance is clearer, but deeper information architecture and visual design still need a separate product design pass.
