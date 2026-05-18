# Dummy Burnout Noncanonical Fixtures

Status: A49b generic fixture/engine parity foundation.

These fixtures are synthetic, non-canonical, and tenant-safe. They must not be
used as platform-canonical instrument evidence, public item text, OLBI examples,
or official validity claims.

What A49b covers here:

- legacy `mean`/`sum` replay from A49a;
- current graph operations: `select_answers`, `reverse_code`, `mean`, `sum`,
  `count_valid`, and `subscale_aggregate`;
- successful multi-output graph replay;
- `min_valid_count` success and failure boundaries;
- `produces.scores`, tenant-attested interpretation metadata, and compatibility
  metadata validation through `ScoringRuleValidator`;
- expected `value`, `n_valid`, `n_expected`, and `missing_policy_status` for
  successful engine outputs.

Known gap to the canonical spec:

- the proposed canonical fixture format in `docs/v2/10-domain/scoring-rules-spec.md`
  is YAML under `fixtures/instruments/<code>/v<version>/canonical/`;
- this non-canonical harness still reads JSON under
  `fixtures/instruments/dummy-burnout/noncanonical/`;
- intermediate value assertions and canonical fixture CODEOWNERS are not
  implemented.

S05 follow-up status:

- `n_valid`, `n_expected`, and `missing_policy_status` are now persisted for
  synchronous setup-lab score rows and returned by the scoring API;
- the physical `score.n` column remains the storage column for `n_valid`.
