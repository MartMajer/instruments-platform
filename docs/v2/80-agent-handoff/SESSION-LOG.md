
## 2026-05-19 - Collection hub production UX pass

Assessment: The Study collection / Collect responses surface still exposed implementation details in the researcher path: proof/local wording, fake invitation recipients, launch snapshot and assignment identifiers, raw readiness codes, local delivery language, and repeated operation-result records. This made the collection hub feel like a developer proof surface rather than a production workflow.

Task: Reframed the selected-series operations workflow as a researcher-facing collection path: pre-launch check, start collection, respondent access, monitor responses, and close collection. Removed the fake email invitation and local delivery steps from the primary workflow, added step navigation, moved IDs and audit records behind collapsed technical details, and localized visible collection timestamps with Croatian-style day/month/year and 24-hour time.

Files changed: apps/web/src/lib/product/operations-workflow.ts, apps/web/src/lib/product/SelectedSeriesOperationsWorkflow.svelte, apps/web/src/routes/app/campaign-series/[seriesId]/operations/+page.svelte, apps/web/src/lib/product/view-models.ts.

Verification: Staging deploy built web/api/worker/migrator images and passed VPS release checks. Public API ready, public home, and /app returned 200 after deploy.

Remaining risk: The invitation backend path still exists but is no longer exposed as a primary collection action. Real email invitation management should become a separate production slice with recipient entry, audience selection, send state, retries, and user-visible failure handling.