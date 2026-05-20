# D366 - Private beta validation rehearsal assessment - 2026-05-20

Status: Completed locally.

## Context

D365 found that the app has become an owner-controlled private-beta validation rehearsal candidate. The next useful agent task was not another product feature; it was to make the current staging walkthrough executable for owner validation calls and close obvious docs drift.

## Assessment

The long validation packet is accurate enough but too broad for a live call. A validator call needs a short rehearsal script with preflight checks, a timed walkthrough, hard boundaries, capture fields, and explicit rules for deciding whether a failure blocks the call or is a known beta limit.

Docs drift also remained around account switching language after the switch-account UI was intentionally removed. The correct product language is sign-out and wrong-account recovery, not account switching as a normal app feature.

## Implemented slice

- Added `private-beta-validation-rehearsal.md` as the owner-run D366 rehearsal script.
- Linked the rehearsal script from the validation walkthrough packet.
- Replaced stale account-switching references in validation materials with sign-out/wrong-account recovery language.
- Updated handoff queue state so the next default move is owner validation or feedback-driven fixes, not another broad feature sprint.

## Verification

- Docs-only `git diff --check` passed with only CRLF normalization warnings.

## Remaining risk

This slice did not run a real authenticated browser walkthrough because that requires the owner session and owner-selected data. The owner should run the D366 rehearsal before O01/O02/O03. If the rehearsal exposes a blocker, create one narrow fix slice.
