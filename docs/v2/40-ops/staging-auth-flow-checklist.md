# Staging auth flow checklist

Last updated: 2026-05-19

This checklist captures the auth/onboarding flows that must stay healthy on staging before another public test pass. It is intentionally product-facing: the goal is to prove that the Auth0 identity state, platform workspace state, and tenant app state line up for a normal user.

## Rules under test

- Auth0 owns credentials, email verification, MFA, password reset, and provider sessions.
- Instrument Platform owns workspace reservation, workspace ownership, tenant membership, and product access.
- Product screens require a tenant login context. Generic sign-in must first resolve the workspace by email.
- A user with an unverified email may enter immediately after signup, but the app must show a verification banner and require verification on later sign-ins.
- Wrong-account recovery must offer a real Auth0 logout path, not a loop back into the same stale provider session.

## New workspace registration

1. Open `/register`.
2. Enter a new owner email, workspace name, and beta code.
3. Continue to Auth0 signup with the same email prefilled.
4. Return to the app after Auth0 signup.
5. Expect the workspace to open without a second workspace setup step.
6. If the Auth0 email is not verified, expect an in-app verification reminder.

## Existing workspace sign-in

1. Open `/signin`.
2. Enter the owner/member email.
3. Expect the app to resolve the workspace by email before Auth0 login.
4. Continue to Auth0.
5. Expect a successful login to land on `/app` without a stale `?auth=failed` marker.

## Duplicate registration email

1. Open `/register`.
2. Enter an email that already owns a workspace.
3. Expect the app to stop before Auth0 signup.
4. Expect the recovery action to send the user to existing-workspace sign-in with the attempted email as the login hint.

## Email verification recovery

1. Register a new owner.
2. Sign out before verifying the email.
3. Sign in again with the same email.
4. Expect a verification-required screen, not a wrong-workspace error.
5. Verify the email in Auth0.
6. Continue sign-in.
7. Expect the app to open the same workspace.

## Wrong Auth0 account recovery

1. Start sign-in with one workspace email.
2. Let Auth0 return a different account.
3. Expect the platform callback to reject the login before tenant resolution with `auth=email_mismatch`.
4. Expect the app to show a wrong-account recovery screen using "selected account" language.
5. Use "Sign out completely" or "Choose account again".
6. Expect Auth0 to clear the provider session and return to a useful sign-in surface.

## Wrong account during registration

1. Start registration with the workspace owner email that should be used.
2. Let Auth0 return a different account.
3. Expect the platform callback to reject the login with `auth=email_mismatch`.
4. Expect `/register` to show "Choose the account you started with".
5. Retry registration sign-in with the same owner email or sign out completely if the browser keeps choosing the wrong account.

## Team member first sign-in

1. Create or prepare a tenant member from `/app/team`.
2. Share the generated first sign-in link.
3. Sign in through Auth0 with the exact invited email.
4. Expect the tenant member to move from pending provider link to active after Auth0 returns that email.
5. If the email is unverified, expect the same verification reminder behavior as the owner flow.

## Evidence to capture

- Browser URL after each redirect.
- User email shown by the app after Auth0 returns.
- Whether `?auth=failed` remains after a successful session.
- API log lines for `workspace_sign_in_*`, `auth_login_*`, and registration completion failures.
- Screenshot of any recovery screen that blocks forward progress.
