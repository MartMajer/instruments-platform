# Open Questions

Single inbox for unresolved decisions across `docs/v2/`. Open questions first, closed below.

Format:
```
### Q-NNN — <one-line title>
**Raised:** YYYY-MM-DD by <who>
**Context:** 2–4 lines of why this matters
**Options:** if known
**Blocks:** what work cannot proceed
**Resolution:** filled when answered (link to ADR if applicable)
**Closed:** date
```

---

## Open

### Q-001 — Single product brand vs separate brand for B2B vs academic
**Raised:** 2026-05-01
**Context:** Academic free tier needs different positioning, possibly different domain/brand from paid B2B (hospitals, OSH consultants). Single brand is simpler but risks confusing buyers.
**Options:** Single brand w/ academic tier; separate brand-light academic site; co-branding with prof's institution
**Blocks:** Marketing site, domain choice, sales narratives
**Resolution:** —

### Q-002 — Self-host option for hospitals with strict data residency
**Raised:** 2026-05-01
**Context:** Some hospitals (especially university hospitals with research data agreements) demand on-prem or single-tenant cloud. Adds support burden.
**Options:** SaaS-only year 1; offer self-host year 2; separate tenant cluster per hospital on managed Postgres
**Blocks:** Hospital sales pitch, infra cost model
**Resolution:** —

### Q-003 — Croatian public sector aggressiveness vs EU-wide first
**Raised:** 2026-05-01
**Context:** Public-sector procurement is slow but big. EU-wide grant funding routes may be faster for proof points.
**Options:** Defer public sector to year 2; pursue Horizon Europe grant-funded route year 1
**Blocks:** Sales priorities, hiring (procurement specialist?)
**Resolution:** —

### Q-004 — Validate P1 wedge by direct conversation with prof lead
**Raised:** 2026-05-01
**Context:** "If I gave you a tenant-provided validated-instrument workflow with anonymous longitudinal tracking, SPSS/analysis-ready export, and EU-hosted data: would you (a) use it for next study, (b) recommend to 5 colleagues, (c) write me into your next grant?" Owner currently deems v2 direction/demand sufficient to keep building because v2 materially surpasses v1, but this conversation is still needed before treating the P1 wedge and sales narrative as externally validated.
**Options:** —
**Blocks:** Formal M1 market validation, sales narrative, and grant/pilot commitment. Does not block GF-track code continuation.
**Resolution:** —

### Q-005 — Identify 3 OSH consultants for P2 validation
**Raised:** 2026-05-01
**Context:** Need to confirm OSH consultants would pay for white-label COPSOQ + NMQ + compliance PDF. Outreach via HZZZSR network or LinkedIn.
**Blocks:** P2 sales narrative; confirms M3 priority
**Resolution:** —

### Q-006 — Hospital pilot via prof lead's clinical contacts
**Raised:** 2026-05-01
**Context:** Need warm intro to one hospital (KBC Zagreb / KBC Split / KB Sestre / KB Merkur) wellbeing officer. Prof lead may have contacts; if not, need alternate route.
**Blocks:** M4 pilot commitment
**Resolution:** —


### Q-007 — Subject vs user_account: one table or two
**Raised:** 2026-05-01
**Context:** Subject = measurement target (may have no login). User = identity for login. Could collapse to one with optional auth fields, but that mixes concerns and complicates permissions.
**Blocks:** Closed by F09.
**Resolution:** Resolved 2026-05-06 in F09. Keep `subject` and `user_account` separate. `subject.user_account_id` may link a measured subject to a login identity, but subjects can exist without accounts and user accounts stay auth/RBAC identities.
**Closed:** 2026-05-06

### Q-008 — Subject merge workflow
**Raised:** 2026-05-01
**Context:** Same person with two records due to data import or external_id mismatch. Need merge UI + audit + foreign key fix-up.
**Blocks:** Data import feature
**Resolution:** —

### Q-009 — Cross-tenant question library — global vs opt-in
**Raised:** 2026-05-01
**Context:** Validated instruments are global. Custom-question sharing across tenants requires explicit opt-in.
**Options:** No cross-tenant custom sharing; explicit "publish to community" flag
**Blocks:** Question library UX
**Resolution:** —

### Q-010 — Mind Garden MBI license/partnership
**Raised:** 2026-05-01
**Context:** MBI is the gold standard but paid via Mind Garden. Need to evaluate license cost + reseller agreement viability before committing to ship MBI.
**Blocks:** MBI shipping in catalog
**Resolution:** —

### Q-011 — Croatian translation strategy
**Raised:** 2026-05-01
**Context:** Pay for back-translation by 2 independent translators per instrument (€500–1500 each) for academic-grade vs accept community translation as starting point.
**Options:** Paid back-translation for first 5 instruments; community for rest with disclaimer
**Blocks:** future canonical named-instrument translation workflow; M2 COPSOQ translation
**Resolution:** —

### Q-012 — Norm tables — derive vs license
**Raised:** 2026-05-01
**Context:** Norm tables (population statistics for benchmark overlay) can be derived from accumulated data (slow) or licensed/sourced from publications (faster).
**Options:** Source published norms per instrument; derive Croatian-specific norms after N respondents
**Blocks:** Benchmark dashboard feature (M2)
**Resolution:** —

### Q-013 — PHQ-9 Item 9 (suicidality) safety protocol
**Raised:** 2026-05-01
**Context:** PHQ-9 Item 9 measures suicidal ideation. Standard practice requires immediate safety message + referral to crisis hotline + (in identified studies) clinician notification. Design this carefully.
**Options:** Anonymous: respondent-facing message + crisis line link; identified: anonymous-route to wellbeing officer; never the platform decides clinical action
**Blocks:** PHQ-9 shipping (M2 or M3)
**Resolution:** —

### Q-015 — Score table partition strategy
**Raised:** 2026-05-01
**Context:** Score volume per campaign is low; high if many tenants. Partitioning by month may be premature.
**Options:** No partitioning initially; monitor; partition when needed
**Blocks:** None now
**Resolution:** Closed 2026-05-12. Do not partition `score` initially. Keep `score_run` and `score` ordinary tables for M1/proof and early tenant use; monitor row volume, query latency, retention pressure, and tenant count before adding partitioning. Revisit only when measured volume or retention operations justify the complexity.
**Closed:** 2026-05-12

### Q-016 — Question codebook on `question` vs separate table
**Raised:** 2026-05-01
**Context:** Codebook fields (variable_name, label, value_labels, measurement_level) live on `question` per current sketch. Separate table only if export tooling needs richer (likely fine on `question`).
**Options:** Keep on question; revisit if export gets complex
**Blocks:** None now
**Resolution:** Closed 2026-05-12. Keep the baseline codebook fields on `question` for M1. Generated export codebooks may derive richer sidecar metadata from `question`, template, scoring-rule, launch-snapshot, disclosure, and export context. Add a separate `question_codebook`/export-codebook table only if production export tooling needs independent versioning or fields that do not belong to the question definition.
**Closed:** 2026-05-12

### Q-018 — Per-tenant column-level encryption
**Raised:** 2026-05-01
**Context:** DPIA outcome may require encrypting email/free-text per-tenant key. Per-tenant key adds complexity.
**Options:** Single per-platform encryption key; per-tenant keys; column-level only on PII fields
**Blocks:** DPIA and security design (M4)
**Resolution:** —

### Q-022 — Score worker: in-monolith Hangfire vs separate process
**Raised:** 2026-05-01
**Context:** Premature extraction. Stay in monolith via Hangfire job until volume justifies.
**Options:** Hangfire in monolith now; dedicated score worker later if backlog/parallelism pushes it.
**Blocks:** None now
**Resolution:** Closed 2026-05-07. Use Hangfire in-process for M1; revisit dedicated score-worker only after post-M1 throughput pressure.
**Closed:** 2026-05-07

### Q-024 — SvelteKit for both surfaces vs split
**Raised:** 2026-05-01
**Context:** Simpler to use one framework; split if team grows.
**Options:** SvelteKit both for M1–M3; revisit at M4
**Blocks:** Frontend scaffolding (M1)
**Resolution:** Closed 2026-05-07. ADR-0004 selected SvelteKit for both respondent and admin surfaces now; split only at M4 or later if scale and team size justify.
**Closed:** 2026-05-07

### Q-026 — Native mobile app priority
**Raised:** 2026-05-01
**Context:** PWA covers most cases. Native via Capacitor is year 2 unless required by buyer.
**Blocks:** None now
**Resolution:** Closed 2026-05-07. Keep PWA-first; native Capacitor app stays deferred to year 2 unless a buyer-specific requirement appears earlier.
**Closed:** 2026-05-07

### Q-027 — White-label theming depth
**Raised:** 2026-05-01
**Context:** Tenant-controlled CSS = flexibility but security/maintenance risk. Design tokens only = safer, more constrained.
**Options:** Design tokens (logo, colors, font) only for M3; revisit
**Blocks:** White-label feature (M3)
**Resolution:** —

### Q-028 — Solo execution vs hire by M3
**Raised:** 2026-05-01
**Context:** Roadmap is realistic with 3-person team. Solo would slip ~2x. Decide hire timing.
**Blocks:** Personal runway planning
**Resolution:** —

### Q-031 — Postgres version upgrade discipline
**Raised:** 2026-05-01
**Context:** Major upgrades are work. Pin to LTS-equivalent or track every major.
**Options:** Pin to current major; upgrade once per year
**Blocks:** None now
**Resolution:** Closed 2026-05-12. Pin M1 to PostgreSQL 16+ as documented in the backend/schema docs. Review major Postgres upgrades annually and when managed-provider support windows require it; do not chase every major release by default. Upgrade only after staging restore/migration rehearsal and RLS/partition/extension smoke coverage.
**Closed:** 2026-05-12

### Q-032 — Slice extraction to separate service
**Raised:** 2026-05-01
**Context:** Vertical slices stay in monolith for v2. Reconsider only at 10x scale or org constraint.
**Blocks:** None
**Resolution:** Closed 2026-05-12. Keep the platform as a modular monolith with vertical slices through M1-M3. Use outbox-backed boundaries and worker jobs where needed, but do not extract application services until measured scale, deployment isolation, compliance isolation, or team ownership creates a concrete need.
**Closed:** 2026-05-12

### Q-033 — Instrument tenant subscription model
**Raised:** 2026-05-01
**Context:** All free instruments available to all tenants? Paid instruments require explicit subscription.
**Options:** Free = all-available; paid = opt-in subscription per tenant
**Blocks:** Tenant settings UI (M2 or M3)
**Resolution:** —

### Q-034 — Instrument version migration policy
**Raised:** 2026-05-01
**Context:** When COPSOQ-III → COPSOQ-IV, what happens to ongoing campaigns? Frozen on prior version. New campaigns can opt new version.
**Options:** Versions immutable; researcher chooses per new campaign
**Blocks:** Instrument versioning UI (post-M3)
**Resolution:** Closed 2026-05-12. Published instrument/template/scoring versions are immutable for launched campaigns. A launched campaign keeps its frozen launch snapshot and scoring rule. New campaigns or new waves may select newer versions, but cross-version wave comparisons require explicit compatibility metadata and default-deny when missing.
**Closed:** 2026-05-12

### Q-041 — Public deprecation of v1
**Raised:** 2026-05-01
**Context:** When/whether to publicly deprecate v1. Likely not until v2 has paying customer.
**Blocks:** None now
**Resolution:** —

### Q-042 - Campaign launch snapshot physical model

**Status:** Closed for M1 baseline on 2026-05-17; future enrichment remains non-blocking.

**Resolution:** LS01 implemented and validated a hybrid physical model: existing scalar freeze columns remain for compatibility, and `campaign_launch_snapshot.launch_packet` now stores a required versioned JSONB production packet. The packet captures template, instrument lineage status, scoring, policy, identity, respondent-rule materialization, launch readiness, and provenance sections while excluding raw answers, tokens, salts, recipient/provider details, tenant IDs, and public instrument/item text.

**Validation:** `dotnet build --no-restore` passed with 0 warnings/errors; targeted unit tests passed 2/2; targeted integration tests passed 31 with 135 expected skips and 0 failures.

**Follow-up:** Canonical norm references, ethics approvals, and richer instrument metadata should be added only when those domains have concrete runtime models. They are not blockers for the current launch snapshot baseline.
### Q-044 — Derivative lifecycle states and permissions
**Raised:** 2026-05-02
**Context:** M1 allows tenant-owned derivatives from tenant-provided or future canonical instruments with content and scoring edits. We need exact states, permissions, audit events, and publish gates for derivative creation and launch.
**Options:** Simple draft/published/retired states; full review workflow; require owner approval for every derivative launch
**Blocks:** Instrument library UI, derivative launch, audit design
**Resolution:** —

### Q-046 — Derivative validity wording in reports and exports
**Raised:** 2026-05-02
**Context:** Derivatives must not claim official validity by default, but reports need clear wording that is accurate, not alarming, and acceptable to researchers.
**Options:** "Custom derivative score"; "Modified from source instrument"; "Not official platform score"; combine label plus explanatory tooltip
**Blocks:** Dashboards, exports, consent copy, derivative UI
**Resolution:** Closed 2026-05-12. Use explicit non-official wording for tenant-owned/custom derivatives by default: "Custom/non-official score" plus provenance such as "derived from tenant-provided/private instrument" or "modified from source instrument where tenant attests rights." Reports, exports, consent copy, and codebooks must not use official/canonical validity labels unless the instrument is platform-canonical or a later revalidation process explicitly grants that status.
**Closed:** 2026-05-12

### Q-049 — Outbox relay worker: Hangfire vs dedicated background service
**Raised:** 2026-05-04 (by ADR-0009 draft)
**Context:** ADR-0009 originally chose Hangfire recurring job for the outbox relay in M1. F07 implemented the relay unit (`OutboxRelay.ProcessDueAsync`) and verified its `FOR UPDATE SKIP LOCKED` claim/dispatch/update behavior. OUT02 added dispatcher routing and unknown-event failure behavior so a future scheduler will not silently mark unknown events published. D78 selected OUT03 as the narrow M1 scheduler/bootstrap slice. OUT03 dependency review found current Hangfire packages are LGPL/commercial, which is conditional under the repo dependency policy, so OUT03 intentionally implemented the M1 relay scheduler with a standard .NET `BackgroundService` instead of adding Hangfire silently. At higher throughput, if owner/legal approves Hangfire, or when M2 broker dispatch lands, revisit whether to keep the hosted service, add Hangfire, or introduce a dedicated worker with explicit batching, per-aggregate locking, and circuit-breaker control.
**Options:** Keep the OUT03 hosted service through M1; add Hangfire later if owner/legal approves the conditional dependency and dashboard/scheduling value justifies it; introduce a dedicated worker/broker topology when MassTransit/RabbitMQ lands in M2+
**Blocks:** None now. M1 has a hosted-service bootstrap. Revisit before M2 broker introduction or before adding Hangfire.
**Resolution:** —

### Q-050 — Audit retention policy specifics
**Raised:** 2026-05-04 (by ADR-0009 draft)
**Context:** ADR-0009 defaults audit retention to "indefinite for security/audit purposes pending DPIA outcome." F06 implemented the `audit_event` parent table, a default partition for inserts, RLS, and an append-only trigger, but it intentionally did **not** implement retention rules or automated monthly partition creation/drop. A14 produced the GDPR retention posture and record classes in `40-ops/compliance-gdpr.md`, but final numeric retention periods remain unresolved because they depend on legal review, tenant segment, backup design, and deployment/runbook decisions.
**Options:** Single platform-wide retention (simplest); per-entity-type retention matrix (most accurate); per-tenant override (max flexibility, max ops complexity)
**Blocks:** Audit table partitioning strategy; external DPA/retention promises; A33/A35 deployment-runbook retention automation; M3 hospital-pilot DPIA
**Resolution:** —

### Q-052 — `instrument_item.question_id` missing FK to `question(id)`
**Raised:** 2026-05-05 (by Agent D during A38)
**Context:** `instrument_item.question_id` is declared as nullable `UUID` with no FK constraint to `question(id)`. ADR-0003 implies that items under a canonical `template_version` map to question rows in that locked template. Without the FK, an item may reference a non-existent question id, and cascade behavior on question deletion is undefined. Pre-existing in the schema before A38 — not introduced by ADR-0003 acceptance, just newly visible.
**Options:** Add `FOREIGN KEY (question_id) REFERENCES question(id)` with `ON DELETE` policy (RESTRICT vs SET NULL — RESTRICT is safer for canonical/locked templates); keep nullable but add FK; tighten to NOT NULL only for items bound to a published locked template_version (would need a partial constraint via trigger).
**Blocks:** Not blocking now (no instrument-item write paths exist yet). Should be resolved before the first canonical-instrument seeding migration runs.
**Resolution:** Closed 2026-05-06 by F11. `instrument_item.question_id` remains nullable for draft/import staging but now has `FOREIGN KEY (question_id) REFERENCES question(id) ON DELETE RESTRICT`.
**Closed:** 2026-05-06
### Q-053 — Production GDPR legal package finalization
**Raised:** 2026-05-06 (by A14)
**Context:** A14 populated `40-ops/compliance-gdpr.md` as an implementation and legal-review brief, not final legal advice. OB01 refreshed the owner-ready counsel packet on 2026-05-14. OB04 added [`40-ops/q053-counsel-ready-legal-packet.md`](../40-ops/q053-counsel-ready-legal-packet.md) as a structured counsel-intake artifact. Owner clarified the current posture: there is no company/legal entity yet, all hosted/demo data is proof-of-concept data, and real participant/student/employee production data is blocked until a validator explicitly asks to use the platform with real people and the legal path is handled first. Owner clarified again on 2026-05-15 that Q-053 is a launch/legal-use gate, not an engineering blocker for production-grade mechanics built and tested with synthetic, seed, demo, or owner-controlled test data.
**Options:** Interim: demos and synthetic/owner-controlled test data only. Before real people: establish legal entity/path, DPA/privacy notice, GDPR review, retention numbers, sub-processors, counsel/legal review, and backup/restore proof for the target environment.
**Blocks:** Real-person participant/student/employee/patient/customer data; paid production tenant launch; production tenant DPA/privacy notice; production tenant launch gates; external security/compliance packet; pricing/sales claims involving GDPR posture. Does not block production-grade engineering, auth hardening, RBAC hardening, dashboard/report work, deployment work, observability, or tenant workflows when they use synthetic, seed, demo, or owner-controlled test data.
**Resolution:** Interim decision 2026-05-14, clarified 2026-05-15: proof/demo use is unblocked only with synthetic, seed, demo, or owner-controlled test data and no final GDPR/DPA/legal claims. OB04 gives the owner/counsel a reusable intake packet, but production use with real participants remains blocked until qualified counsel or explicit owner legal authority approves the target scenario.

---

### Q-054 - Operational notification email recipient policy
**Raised:** 2026-05-18 by Codex during D265 assessment
**Context:** Operational notifications are now durable in-app events for report PDF terminal states and withdrawal request lifecycle, and campaign email delivery has provider/config/readiness/requeue foundations. Sending operational notifications by email is not just a transport task: the platform must decide which tenant roles/users receive which notification types, whether anonymous/requester acknowledgements are ever sent, how opt-out/preferences work, which severities trigger email, and what content is safe for email bodies. Without that policy, email routing could leak operational pointers, spam wrong tenant members, or imply requester acknowledgements that the product has not promised.
**Options:** Tenant-admin-only for warning/severe events; setup.manage members for all unread operational events; configurable tenant notification preferences; no operational email in M1 and keep in-app only; separate requester acknowledgement workflow after legal/product decision.
**Blocks:** Operational-notification email routing, requester/admin email workflows, notification preference UI, SMTP-backed operational alerting, and any product claim that operational events are emailed.
**Resolution:** -

### Q-056 - Auth/account API route boundary
**Raised:** 2026-05-19 by Codex during Auth0 registration/sign-in hardening
**Context:** The beta existing-workspace lookup endpoint is currently `POST /registration/workspace-sign-in` because it reuses registration email lookup rules and rate limiting. That route is functional, but conceptually it belongs to auth/account discovery, not workspace creation. Renaming it now would be mostly mechanical, but doing it before the account/session boundary is designed may create another temporary name.
**Options:** Keep the beta route until a broader auth/account API cleanup; add a stable alias such as `/auth/workspace-sign-in`; move account discovery under `/account/workspace-sign-in`; replace it entirely when a workspace picker exists.
**Blocks:** Clean public API naming for existing-workspace sign-in. Does not block current beta registration or tenant sign-in behavior.
**Resolution:** -

### Q-057 - Multi-workspace account picker
**Raised:** 2026-05-19 by Codex during Auth0 registration/sign-in hardening
**Context:** M1/beta currently enforces one active workspace membership per normalized email, so `/signin` can look up a single workspace by email before redirecting to Auth0. If one Auth0 identity/email can belong to multiple workspaces, email lookup cannot safely or correctly choose a tenant. A proper picker likely needs an account-level authenticated state before issuing a tenant session.
**Options:** Keep one-email-one-workspace for beta; cheap email lookup returns multiple workspace choices; proper flow signs in with Auth0 first, creates an account-level context, then lets the user choose an authorized workspace; later add in-app workspace switching.
**Blocks:** Multiple active workspace memberships for the same normalized email; workspace switching UX; production-grade multi-tenant account selection. Does not block current beta where one email maps to one workspace.
**Resolution:** -

## Closed

### Q-058 - M1 private-beta acceptance bar
**Raised:** 2026-05-20 by Codex during D350 project-state roadmap assessment
**Context:** The app moved from proof spine to private-beta product shell, but the roadmap exit criterion and the active queue did not define the exact browser-level acceptance path that must work before owner-led validator calls rely on the app as current proof.
**Options:** Define a narrow checklist around one anonymous/open-link study; define a broader checklist that includes registration, team, directory, anonymous invite-only, results, exports, and waves; make BR01 produce the checklist and then split blockers into follow-up slices.
**Blocks:** Closed for the current private-beta acceptance bar. Follow-up blockers are QB01, DIR04, AUD01, RSLT01, and VAL08.
**Resolution:** Closed by BR01 on 2026-05-20. The acceptance bar is `docs/v2/80-agent-handoff/private-beta-acceptance-checklist.md`; D351 selects QB01 as the next slice.
**Closed:** 2026-05-20

### Q-055 — Beta duplicate workspace registration by email
**Raised:** 2026-05-19 by owner during Auth0 registration flow hardening
**Context:** Auth0 owns identity accounts, while the platform owns workspaces, memberships, roles, and sessions. Allowing the same platform email to create multiple beta workspaces creates duplicate tenants and requires a workspace picker that is not part of the current beta flow. The platform should not query Auth0 to discover whether an email exists because that adds Management API coupling and account-enumeration risk.
**Options:** Allow one email to own multiple workspaces; block duplicate owner emails only; block registration when the email is already any platform workspace member.
**Blocks:** Closed for M1/beta registration policy. Future multi-workspace support requires a workspace picker/account switcher design before this rule is relaxed.
**Resolution:** For M1/beta, one email may belong to one active platform workspace membership. `POST /registration/intents` should reject a valid registration attempt when the normalized email already belongs to any workspace member, return a clear "workspace already exists for this email" error, and provide a sign-in path with `login_hint` set to the attempted email. Do not query Auth0 for existing emails; enforce this in platform data after beta-code validation.
**Closed:** 2026-05-19

### Q-021 — Hosting choice
**Raised:** 2026-05-01
**Context:** Hetzner cheap + EU but manual; OVHcloud EU + managed; AWS Frankfurt / Azure West Europe = standard but expensive. OB01 refreshed the owner-ready decision packet on 2026-05-14. The owner then clarified that the existing Hostinger VPS is technically allowed because v1 ran there and v2 is Docker-portable. The real decision is the allowed M1 hosting posture, not permanent vendor lock-in.
**Options:** Portable Docker Compose on the existing Hostinger VPS; Hetzner-style EU VPS/object-storage baseline; OVHcloud managed PostgreSQL/managed EU services; AWS Frankfurt/Azure West Europe; future homelab plus cheap VPS interface; single-tenant/self-host only when explicitly funded.
**Blocks:** Closed for M1 validation hosting. Q-053 still blocks real-person production data and production legal/GDPR claims. Q-002 still covers hospital/self-host demands.
**Resolution:** Closed by [ADR-0012 - M1 Portable Docker Hosting Posture](../70-decisions/0012-m1-portable-docker-hosting-posture.md). Use a portable Docker Compose posture for M1 validation and first production-like proof. The current owner-paid Hostinger VPS is the default validation host because it is already paid and configured, but the product must stay portable to another EU VPS or future homelab/interface-VPS setup. No GitHub Actions, registry promotion, Terraform, or managed database is required before validation demos. Before real-person data, run Q-053 for the tenant scenario and prove backup/restore for the target environment. Before hospital/public-sector/enterprise/SLA/DPA claims, reassess managed DB, provider, hyperscaler, or single-tenant needs.
**Closed:** 2026-05-14

### Q-020 — Auth0 (paid SaaS) vs Keycloak (self-host)
**Raised:** 2026-05-01
**Context:** Auth0 saves ops time, costs money. Keycloak is free but adds production identity-server operations. OB01 refreshed the owner-ready decision packet on 2026-05-14; the owner then reviewed Auth0 Free pricing/features and accepted Auth0 Free for M1.
**Options:** Auth0 Free for M1; Keycloak self-host for control; hybrid Auth0-now/Keycloak-trigger-later.
**Blocks:** Closed for provider selection. Provider-specific login/callback/refresh/session-store implementation remains future work under ADR-0011.
**Resolution:** Closed by [ADR-0011 - M1 Auth Provider](../70-decisions/0011-m1-auth-provider.md). Use Auth0 Free for M1 validation and first production-like auth. Keep the application OIDC-provider neutral; the app owns tenants, roles, permissions, and authorization. Keep Keycloak as fallback if Auth0 becomes financially, operationally, data-residency, or procurement-wise unsustainable. No paid Auth0 plan without explicit owner decision.
**Closed:** 2026-05-14

### Q-051 — Outbox event payload size limits + truncation strategy
**Raised:** 2026-05-04 (by ADR-0009 draft)
**Context:** ADR-0009 stores `payload` as JSONB and `last_error` as TEXT. F07 truncates `last_error` defensively in code, but it did **not** enforce a payload byte cap or pointer-pattern for large events. Without a cap, a runaway domain event could bloat relay scans and audit joins.
**Options:** Hard byte cap on `payload` (e.g., 64 KiB) with relay rejection above; soft cap with WARN log + truncation marker; pointer-pattern; separate handling per event_type.
**Blocks:** Closed for M1 subscriber contract and production payload validation. Future large-event slices must use pointer payloads rather than widening the cap by default.
**Resolution:** Closed by OUT01 on 2026-05-14. M1 outbox event payloads have a 64 KiB UTF-8 serialized JSON hard cap. `OutboxPayload.Create` rejects oversized payloads before returning a `JsonDocument`, and `OutboxEvent.Create` also validates direct `JsonDocument` payloads before persistence. Event payloads are not truncated and do not use a "payload truncated" envelope. Existing `last_error` truncation remains separate at 2,000 characters. Large response/session/export/PDF events must publish small ids/links and use a future object-storage or authorized-resource pointer pattern if they need large data.
**Closed:** 2026-05-14

### Q-047 — OLBI canonical source and license evidence
**Raised:** 2026-05-02
**Context:** OLBI was the historical M1 proof-instrument, but platform-canonical rights evidence is incomplete. The active product is no longer an OLBI product; it is a generic validated-instrument runtime for tenant-provided or rights-attested instruments.

**Decision state (2026-05-13):** Closed by product de-scope, not by legal permission. The platform will not ship, seed, market, or claim OLBI as platform-canonical active M1 content.

**Current decision boundary:**

- **Allowed now:** generic instrument builder, generic scoring rules engine, tenant-owned/private named-instrument imports when the tenant attests rights, owner/demo private work, and non-canonical internal validation fixtures using tenant attestation.
- **Not in active scope:** platform-canonical OLBI preset/seed, public claims that OLBI is included, official OLBI validity labeling, public item-text fixtures/samples, or launch flows that present OLBI rights as platform-granted.

**Options:** closed by owner/product decision to remove OLBI from active claims. Future platform-shipped named-instrument content needs a new owner/legal canonical-publish decision.

**Blocks:** nothing in the active tenant-private/generic proof spine.

**Current clarification:** Generic engineering work is unblocked. Q-047 is
closed as an active blocker because OLBI is de-scoped from platform claims.
This does not grant OLBI rights. It means the platform must not make public
OLBI included statements, canonical catalog seeds, official validity labels, or
rights-critical tenant-onboarding claims. Tenant-owned/private imports remain
allowed only through tenant attestation.

**D73 roadmap clarification (2026-05-13):** Legacy F-track blocker chains must
not be read as current generic-runtime blockers. Generic tenant-private work
continues through the GF/S/P/C/R/X/U/UX/OPS proof-spine rows. Only canonical
OLBI seed/preset/public item text/official labels/platform-granted rights
claims remain blocked by this question.

**D73A resolution (2026-05-13):** Closed by de-scope. Active product and M1
docs must not claim or imply OLBI as included, official, platform-canonical, or
platform-granted. OLBI may remain only as historical context, private
tenant-provided/tenant-attested content, or an internal capability-class
example. Reopening OLBI as platform-shipped content requires a new owner/legal
decision and evidence package.
**Closed:** 2026-05-13

Use the implementation intent in [olbi-capable-engine-principle.md](olbi-capable-engine-principle.md) for all agent implementation notes.

### Q-023 — SignalR vs SSE for dashboard live updates
**Raised:** 2026-05-01
**Context:** SignalR is bidirectional, SSE simpler one-way. Dashboards mostly need server→client push.
**Options:** SSE for M1; SignalR if interaction needs grow
**Blocks:** None now
**Resolution:** Closed by A11 in [`../20-architecture/realtime.md`](../20-architecture/realtime.md). M1 uses SSE/EventSource for one-way dashboard push. SignalR is deferred until a bidirectional use case exists.
**Closed:** 2026-05-06

### Q-048 — RLS cross-tenant read test pattern
**Raised:** 2026-05-04 (by ADR-0001 acceptance)
**Context:** ADR-0001 mandates a CI-enforced test that creates 2 tenants, performs cross-tenant read attempts through application code, and asserts empty results. Need exact test scaffolding (xUnit fixture, test categories, CI gate, run frequency) before any multi-tenant feature reaches production.
**Options:** xUnit class fixture with tenant pair; integration-test suite gated in CI; per-feature smoke + global tenant isolation suite (defense-in-depth)
**Blocks:** Production deploy of any multi-tenant write path; M1 release gate
**Resolution:** Closed by A36 in [`../40-ops/security.md`](../40-ops/security.md#rls-cross-tenant-test-pattern). Required pattern: Testcontainers Postgres integration suite, two-tenant fixture, runtime `platform_app` assertions, seed-role-only setup, direct read/write denial checks, missing-scope fail-closed check, global `is_global` read/write checks, Dapper protection check, outbox subscriber tenant-scope check, schema-policy meta-tests, per-feature smoke tests, and PR/nightly CI gates.
**Closed:** 2026-05-05

### Q-014 — Subject and user_account separation
**Raised:** 2026-05-01
**Context:** See Q-007. Architectural duplicate; consolidate.
**Blocks:** Schema finalization
**Resolution:** Duplicate of Q-007. Keep Q-007 as the active question.
**Closed:** 2026-05-02

### Q-029 — Hetzner vs OVHcloud vs hyperscaler for prod
**Raised:** 2026-05-01
**Context:** Same as Q-021. Tracked separately because prod choice may differ from dev.
**Blocks:** Production deployment (M1)
**Resolution:** Duplicate of Q-021. Q-021 is now closed for M1 by [ADR-0012 - M1 Portable Docker Hosting Posture](../70-decisions/0012-m1-portable-docker-hosting-posture.md). Future hospital/public-sector, enterprise, managed database, hyperscaler, or single-tenant demands reopen through Q-002/Q-053 or a new hosting ADR, not through this duplicate.
**Closed:** 2026-05-02

### Q-030 — Skip Designer in M1 for faster wedge
**Raised:** 2026-05-01
**Context:** Original question assumed M1 would be a narrow named-instrument runtime and could skip designer/admin work.
**Blocks:** None
**Resolution:** Superseded by ADR-0007 and D73A. M1 now includes validated instrument lifecycle admin workflows for tenant-provided instruments, derivatives, and scoring edits. Generic survey designer remains out of M1.
**Closed:** 2026-05-02

### Q-019 — MediatR vs hand-rolled dispatcher
**Raised:** 2026-05-01
**Context:** MediatR commercial license terms changed in v12. Hand-rolled dispatcher avoids licensing concerns.
**Options:** MediatR (battle-tested, documented patterns); hand-roll (no fee, ~200 LOC)
**Blocks:** Architecture scaffolding (M1)
**Resolution:** **MediatR.** Small-business tier (<$5M USD ARR) is free under current licensing; pay when revenue justifies. Captured in ADR-0002.
**Closed:** 2026-05-04

### Q-017 — Instrument canonical_template_version_id circular FK
**Raised:** 2026-05-01
**Context:** `instrument` references `template_version`; `template_version` is platform-global for instruments. Need deferred FK constraint or two-step seeding.
**Options:** Deferred FK in Postgres; or seed template_version first, then update instrument
**Blocks:** Instrument seeding implementation (M1)
**Resolution:** **Two-step seeding.** Insert template_version with instrument_id NULL → insert instrument → UPDATE template_version.instrument_id. Avoids deferred-constraint interaction with EF Core change tracking. Captured in ADR-0003.
**Closed:** 2026-05-04

### Q-025 — SurveyJS license cost validation
**Raised:** 2026-05-01
**Context:** Confirm current pricing; evaluate volume discount terms.
**Blocks:** Frontend scaffolding (M1)
**Resolution:** Pricing confirmed 2026-05-04 — Essential free, Basic €499/dev/yr (Creator), PRO €899/dev/yr (Dashboard + PDF), Enterprise €1998+. **Decision: Essential (free) for M1; defer Basic to M2 reassessment; skip PRO; skip Enterprise.** No Creator in M1 — simple custom derivative editor (A40) instead. PDF via PuppeteerSharp, not SurveyJS PRO. Captured in ADR-0004.
**Closed:** 2026-05-04

### Q-035 — Participant code recipe defaults
**Raised:** 2026-05-01
**Context:** What recipe to recommend by default?
**Resolution:** Platform ships 3 reviewed default recipes each exceeding 60 bits entropy. Recipe editor enforces ≥ 60-bit floor; shorter recipes rejected. Captured in ADR-0005.
**Closed:** 2026-05-04

### Q-036 — Participant code rejoin if forgotten
**Raised:** 2026-05-01
**Context:** Per-study toggle for security-question fallback (weakens anonymity slightly but allows rejoin).
**Resolution:** **NO security-question fallback.** Forbidden platform-wide; per-study toggle not offered. Forgotten code = lost trajectory; documented as known and acceptable cost of true anonymity. Captured in ADR-0005.
**Closed:** 2026-05-04

### Q-037 — Cross-tenant participant codes (federated)
**Raised:** 2026-05-01
**Context:** In federated study across tenants, participant code at federated level or per-tenant?
**Resolution:** Federated campaign series uses a federated-level salt shared by participating tenants. Per-tenant (per-series) salt is the default for non-federated studies. Schema accommodates this from M1; federated mode activates in M5. Captured in ADR-0005.
**Closed:** 2026-05-04

### Q-038 — Hash algorithm + salt rotation
**Raised:** 2026-05-01
**Context:** SHA-256 + per-tenant salt is fine now. NIST guidance evolves.
**Resolution:** **Argon2id at `m=64 MiB, t=3, p=4`** is the platform default. SHA-256 is NOT used for participant codes (too fast — enumerable in milliseconds under realistic recipe entropy). Salt is per-campaign-series, not per-tenant. Salt rotation remains impossible without invalidating existing codes (feature, not bug). Track NIST; revisit parameters every 2 years. Captured in ADR-0005.
**Closed:** 2026-05-04

### Q-039 — v2 repo name
**Raised:** 2026-05-01
**Context:** Pick a working name for the new repo. Brand-name TBD; technical placeholder needed.
**Resolution:** Working codename **`instruments-platform`**. Final brand-name TBD; rename later (one command). Captured in ADR-0006.
**Closed:** 2026-05-04

### Q-040 — License model for v2
**Raised:** 2026-05-01
**Context:** MIT, AGPL, source-available (Elastic-style), or proprietary closed-source.
**Resolution:** **Proprietary, closed-source, all rights reserved.** Source code not published. Commercial EULA sold to customers. **Dependency policy forbids AGPL/SSPL** (their copyleft would force the platform itself open-source). MIT/Apache-2.0/BSD/MPL-2.0/ISC permitted with attribution. Captured in ADR-0006.
**Closed:** 2026-05-04

### Q-043 — Scoring rule JSON schema final shape
**Raised:** 2026-05-02
**Context:** ADR-0008 proposes scoring rule JSON as the source of truth. The operation palette, schema versioning, validation errors, fixture format, and compatibility metadata need exact shape before implementation.
**Options:** Hand-authored JSON Schema; C# source model with generated schema; both with schema-version migration tests
**Blocks:** Scoring editor, scoring worker, future canonical named-instrument fixtures
**Resolution:** Locked in [`../10-domain/scoring-rules-spec.md`](../10-domain/scoring-rules-spec.md) (🟡 Proposed final). Top-level document shape (`schema_version`, `engine_min_version`, `rule_id`, `rule_version`, `instrument_ref`, `compatibility`, `inputs`, `nodes`, `outputs`, `missing_data`, `validation`) defined; M1 operation palette (15 ops) covers burnout-style reverse-coding/subscale scoring with extensibility hooks for COPSOQ-III subscale composition, UWES-9 mean-with-reverse, and MBI subscale + cut-points; semver schema versioning + forward-compatible operation registry with deprecation policy; structured validation error envelope (RFC 6901 pointer + line/column + did-you-mean) with live/preview/publish UX; per-instrument YAML fixtures with CI gate; mixed-version three-state compatibility model (closes Q-045); engine snapshotting (snapshot row + frozen JSONB) with retention policy; explicit determinism guarantee with test cadence; visual-builder = strict-subset invariant with op promotion process; worked canonical-style rule + derivative example. Implementation now unblocked: schema files, engine handlers, editor UX, and generic fixture sourcing can begin.
**Closed:** 2026-05-04

### Q-045 — Mixed-version compatibility metadata
**Raised:** 2026-05-02
**Context:** Reports may compare waves only when scoring/template compatibility is known. Without metadata, mixed-version deltas are blocked or descriptive/non-equivalent.
**Resolution:** **Three-state compatibility model accepted** (`output_equivalent_with` / `descriptive_only_with` / `incompatible_with`) with default-deny for undeclared cross-version deltas; per-output `scope` overrides supported. Owner confirmed 2026-05-04 that `descriptive_only` is a useful distinct state (vs collapsing into `incompatible + warning`) — rationale: COPSOQ-III pattern where most subscales remain stable across editions and one or two get reworked needs per-output granularity, and "compute with caveat" matches scientific practice better than binary "compute or don't." Compatibility resolved at report-generation time against snapshotted rule. Captured in [`../10-domain/scoring-rules-spec.md`](../10-domain/scoring-rules-spec.md#5-mixed-version-wave-compatibility) (Section 5).
**Closed:** 2026-05-04


