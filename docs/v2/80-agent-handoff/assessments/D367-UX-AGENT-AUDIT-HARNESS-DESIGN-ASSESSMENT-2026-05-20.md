# D367 - UX agent audit harness design assessment - 2026-05-20

Status: Completed locally.

## Context

Owner validation and repeated manual UX passes are creating fatigue. The app has enough surface area that the owner is effectively playing first-time researcher, professor, OSH consultant, workspace owner, respondent, and QA tester. That is not sustainable.

## Assessment

The next useful UX investment is a repeatable audit harness, not another manual polish sprint. The harness should let a persona-style agent review evidence from local browser missions and produce structured findings. Starting local-first avoids Auth0/staging cookie noise and makes reruns cheap after fixes.

Subagents can help as persona reviewers, but they need Playwright evidence to avoid hallucinating. The first system should capture screenshots, URL, visible text, visible controls, and step logs, then ask reviewers to classify findings by severity.

## Decision

Proceed with UXA01: a local-first Playwright evidence plus persona review harness.

Do not start with random clicking. Start with bounded missions and max-step limits. Defer staging-cookie mode and automatic issue creation until local runs are useful.

## Outputs

- Design: `docs/plans/2026-05-20-uxa01-local-ux-agent-audit-harness-design.md`
- Implementation plan: `docs/plans/2026-05-20-uxa01-local-ux-agent-audit-harness.md`

## Remaining risk

This design does not replace real validation calls. It should reduce owner fatigue and catch UI/UX blockers, but product direction should still come from O01/O02/O03 evidence.
