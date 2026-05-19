# D349 Custom Study Builder Assessment

## Assessment

Owner feedback exposed a product-risk gap in the setup workflow: the app was drifting toward a weak generic survey builder while still exposing internal scoring-rule and template concepts. Generic survey platforms already cover broad question authoring better. The v2 wedge must be governed, scored, repeatable studies, not commodity form building.

The existing questionnaire step had started moving toward researcher-facing authoring, but the scoring step still exposed rule keys, raw JSON documents, and produces metadata. That made the most differentiating part of the product feel less usable than a generic survey tool.

## Decision

For the current MVP, keep the visual builder intentionally constrained:

- Questionnaire authoring can expose common answer formats.
- Results setup should hide scoring-rule JSON and let the researcher configure one score output.
- The current visual scoring model supports rating, recommendation, and number questions only.
- Text, choice, date, and ranking answers can be collected but are not visually scored yet.
- Do not claim full generic survey-builder parity.

## Task

Rework the current setup UI so:

- Step 3 is named Results setup, not Scoring rule.
- The user chooses a result name, average/sum calculation, missing-answer policy, and included scoreable questions.
- Non-scoreable answers are shown as collected but not scored.
- Raw rule keys, scoring JSON, and produces JSON leave the normal setup path.
- The questionnaire model supports rating scales, recommendation scale, choice, number, text, date, and ranking payload metadata.
- Honest product limits are documented in `docs/v2/30-features/custom-study-builder.md`.

## Verification plan

Deployment build should be used as the syntax and production-bundle gate for this UI slice. Runtime product proof should be owner-tested on staging by creating a study with mixed question types, saving Results setup, creating a campaign draft, and running launch readiness.

No local verification has been run yet in this assessment entry.

## Remaining risk

The visual builder is still not complete. It does not yet cover branching, matrix/grid questions, multi-score subscales, choice scoring, interpretation bands, norms, reliability metrics, or full respondent-rendering parity proof for every exposed question type.
