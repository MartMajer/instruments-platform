# D350 Follow-Up Execution Design

Status: approved by owner direction to continue the D350 follow-up goal.

## Purpose

Execute the D350 follow-up queue without drifting back into broad feature work.

The execution model is acceptance-first:

- BR01 defines the private-beta acceptance bar.
- QB01 proves the exposed questionnaire formats against that bar.
- DIR04 removes the manual-audience-entry bottleneck.
- AUD01 makes recipient selection understandable to researchers.
- RSLT01 expands Results setup from one score to dimensions/subscales.
- VAL08 refreshes the validation packet to match the current app.

## Design choices

### Acceptance bar before features

BR01 comes first because the app has enough surface area that subjective polish is no longer a useful guide. The checklist must say what beta-ready means, route by route, and classify gaps as blockers, known beta limits, or later backlog.

### Sequential slices with assessment after each

Each slice should leave behind:

- a commit;
- an assessment;
- updated queue state;
- updated session log;
- targeted verification evidence appropriate to the slice.

### No dormant canonical named-instrument work

The active lane remains tenant-private and owner/tenant-attested content. No platform-canonical named-instrument seeding, public item text, official validity labels, or rights claims are part of this run.

### Legal and production boundaries stay intact

Q-053 still blocks real-person production data and production legal/GDPR/DPA claims. The work may use synthetic, seed, demo, or owner-controlled test data only.

## Non-goals

- Do not add multi-workspace account switching.
- Do not add M2 COPSOQ/UWES canonical content.
- Do not add SurveyJS Creator or broad SurveyMonkey-class design breadth.
- Do not add CI/deployment automation unless manual deployment becomes the bottleneck.
- Do not claim production readiness from private-beta proof.
