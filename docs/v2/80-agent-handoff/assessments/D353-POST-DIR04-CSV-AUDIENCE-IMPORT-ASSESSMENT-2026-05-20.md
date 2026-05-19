# D353 Post-DIR04 CSV Audience Import Assessment - 2026-05-20

## Context

D350 selected M1 private-beta acceptance quality as the active agent lane. BR01 defined the beta acceptance checklist and QB01 closed the exposed questionnaire-format respondent parity gap. DIR04 was selected next because realistic owner validation cannot depend on manually creating every person, group, and group membership one record at a time.

DIR04 implemented a tenant-scoped CSV audience import MVP:

- `POST /subjects/imports/csv` accepts pasted/uploaded CSV content through the product-surface API.
- The backend validates CSV headers and row identity using `external_id` or `email`.
- Imports create or update tenant subjects, create or match groups, and add or skip group memberships.
- The Directory page exposes a paste/file import panel with accepted columns and row-level results.
- Focused frontend API coverage and API endpoint coverage pass.

The slice intentionally does not implement HRIS sync, background jobs, manager hierarchy import, rich merge review, or multi-workspace audience sharing.

## Assessment

DIR04 materially reduces the private-beta blocker for preparing a normal study audience. A researcher can now prepare a realistic tenant directory from a CSV instead of manually building the whole subject/group structure.

The implementation is production-shaped but not deployment-proven yet because Docker-backed store tests could not run in the current local environment. The store path touches tenant-scoped persistence and duplicate/idempotency behavior, so it needs Docker/Testcontainers proof before deployment or owner walkthrough.

The remaining product gap is not import plumbing. The sharper next blocker is still AUD01: collection setup must let a researcher select recipients in plain language and understand which imported audience will receive invitations.

## Verification

Passed:

- Frontend product API test: `apps/web/src/lib/api/product.test.ts` passed 30/30 with explicit Node invocation.
- Product-surface endpoint test: `ProductSurfaceEndpointTests.Import_subject_directory_csv_endpoint_binds_request_and_requires_setup_manage` passed 1/1.

Added but not fully executable here:

- Docker-backed `ProductSurfaceWriteStoreTests.Import_subject_directory_csv_*` coverage for upsert, group creation, membership creation, and idempotency.

Blocked:

- Running the Docker-backed store tests with `RUN_POSTGRES_INTEGRATION_TESTS=1` failed before test execution because Testcontainers could not connect to the local Docker engine at `npipe://./pipe/docker_engine`.

Known unrelated verification debt:

- Broader `svelte-check` still fails on existing unrelated TypeScript/Svelte issues recorded in D352. No errors were reported in the DIR04 touched frontend files during that run.

## Decision

Treat DIR04 as locally implemented and queue-corrected, with a deployment gate: run the Docker-backed import store tests in a Docker-enabled environment before deploying the slice or using imported audiences in owner validation.

Proceed to AUD01 as the next active implementation slice because the audience model now has enough import support, but the researcher-facing selection flow remains too model-driven.

## Next slice

AUD01 - rework collection audience selection into researcher-facing recipient UX.

Acceptance direction:

- Let the researcher choose recipients as normal product concepts: everyone in the study audience, a named group, or a supported relationship path.
- Keep anonymous invite-only behavior clear: invitations are operational delivery records, while answer reporting remains anonymous.
- Preview recipient counts and examples before save.
- Make empty-audience launch blockers obvious and actionable.
- Preserve existing respondent-rule backend mechanics unless the UX cannot be made coherent on top of them.
