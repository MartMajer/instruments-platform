# D361 - Auth recovery UX hardening assessment - 2026-05-20

## Context

D360 added the expected-email guard so an Auth0 callback cannot silently continue with an account different from the workspace email entered on `/signin`.

Owner testing after D360 showed the security behavior was right, but the recovery surfaces still exposed provider-internal language and one registration-context mismatch path could still land on the app recovery screen instead of the registration recovery screen.

## Assessment

The platform auth model remains:

- Auth0 owns credentials, provider session, MFA, and email verification.
- The platform owns workspace lookup, tenant membership, role claims, and session creation.
- `login_hint` is only a provider hint, so the platform must bind and verify the returned email itself.
- Wrong-account recovery must make the user choose the same account again without implying the app can directly control the provider account picker.

The remaining gap was UX/state handling, not authorization bypass:

- Tenant membership checks already failed closed.
- D360 already rejected expected-email mismatches before tenant resolution.
- The registration-context wrong-account callback needed to return to `/register?auth=email_mismatch`.
- Public copy should say "sign-in provider" or "selected account", not "Auth0 returned...", on user-facing screens.

## Task

- Added a regression test for registration-context email mismatch recovery.
- Updated OIDC remote-failure fallback so registration-context `email_mismatch` returns to `/register`.
- Added `/register?auth=email_mismatch` copy using the saved registration sign-in link.
- Replaced remaining user-facing Auth0/internal-provider wording on `/app`, `/signin`, and `/register`.
- Added a visible app-session "Switch account" action that clears the provider session and returns to `/signin`.

## Verification

- RED: focused integration test failed because registration-context email mismatch redirected to `/app?auth=email_mismatch`.
- GREEN: focused integration test passed after the fallback change.
- `dotnet test tests/Platform.IntegrationTests/Platform.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~AuthEndpointTests"` passed 68/68.
- `apps/web` SvelteKit sync and Vite production build passed with the existing large-chunk warning.

## Remaining risk

Only a real provider account-selection flow can prove the browser/provider session UX end-to-end. Automated coverage now locks the platform callback fallback and email-mismatch guard, but owner/manual staging still needs to exercise:

- `/signin` email X followed by provider account Y.
- `/register` email X followed by provider account Y.
- Switch-account provider logout returning to `/signin`.
