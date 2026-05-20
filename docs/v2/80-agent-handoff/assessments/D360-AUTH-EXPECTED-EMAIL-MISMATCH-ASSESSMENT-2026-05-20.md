# D360 - Auth expected-email mismatch assessment - 2026-05-20

## Status

Fixed locally. Deployment evidence should be appended after staging deploy.

## Assessment

Owner identified an auth hardening gap in the existing-workspace sign-in flow:

1. User enters `x@example.test` on `/signin`.
2. The platform resolves the workspace and redirects to Auth0 with `login_hint=x@example.test`.
3. Auth0 can still return `y@example.test` if the browser has a stale provider session or the user chooses another account.

This was not a direct privilege-escalation path because the platform resolver still required the returned Auth0 email to be an active member of the requested tenant. However, it was a real security/UX ambiguity: `login_hint` is not an identity constraint, and the app should not silently continue after Auth0 returns a different email than the account the user used to find the workspace.

## Fix

- `/auth/login` stores the normalized `login_hint` as an expected login email in the protected auth challenge properties.
- `PlatformOidcEvents.TokenValidated` compares the real Auth0 email claim to that expected email before calling the tenant resolver.
- Mismatch fails closed with `auth=email_mismatch`; the tenant resolver is not called.
- `/app?auth=email_mismatch` shows a wrong-account recovery state and offers `Choose account again` plus `Sign out completely`.

## Verification

- RED focused auth tests failed before the production change because `EmailMismatchFailureReason` and `ExpectedLoginEmailPropertyName` did not exist.
- GREEN focused auth endpoint suite passed: 67/67.
- Web production build passed with the existing large-chunk warning.
- Local production-preview browser check for `/app?auth=email_mismatch` passed with `/auth/session` mocked to 401, proving the recovery title, explanation, retry action, and sign-out action render on mobile.

## Remaining risk

Owner should still test the real Auth0 UI path on staging because Auth0 controls the visible account chooser and stale provider-session behavior. The platform callback guard is automated and covered.
