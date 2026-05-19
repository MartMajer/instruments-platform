# Auth onboarding hardening plan

Date: 2026-05-19

## Goal

Tighten the post-registration and first-run product experience after the Auth0 recovery work, without changing the Auth0/domain ownership model.

## Success criteria

- Deferred auth boundary and multi-workspace decisions are recorded as open questions.
- Staging auth flow has a repeatable checklist for owner, existing-user, duplicate-email, verification, wrong-account, and team-member paths.
- First successful workspace entry gives the owner concrete setup actions instead of a bare product shell.
- Team member setup explains the Auth0/platform split before the tenant admin shares an invitation/sign-in link.
- Directory setup explains groups, subjects, memberships, and manager relationships without implying that hierarchy equals app authorization.
- Focused local verification covers the new product guidance.

## Scope

- Documentation: auth checklist and handoff/open-question records.
- Web app: `/app`, `/app/team`, `/app/directory`.
- E2E coverage: focused Playwright checks for first-run, team member onboarding, and directory setup guidance.

## Out of scope

- Renaming `/registration/workspace-sign-in`.
- Multi-workspace account picker.
- New Auth0 tenant configuration changes.
- Deployment.

## Task list

1. Record the deferred auth route-boundary and multi-workspace questions.
2. Add a staging auth-flow checklist.
3. Add first-run setup runway cards to the app home page.
4. Clarify team/member first sign-in and pending-provider-link behavior.
5. Clarify directory hierarchy setup order and its difference from app roles.
6. Run focused build and E2E verification.
7. Update handoff docs with the final state and remaining risks.
