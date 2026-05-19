# ADR-0013 - Anonymous invite-only distribution

**Status:** Proposed
**Date:** 2026-05-19
**Deciders:** Martin Majer
**Supersedes:** -

## Context

Research studies often need a selected audience without identified answers. A tenant may invite 10 known people and still require that reports and exports do not expose which person gave which answer.

The platform already distinguishes response identity modes and already has anonymous invitation tokens, anonymous assignments, and email notification delivery. The missing boundary was product semantics: saved audience rules were treated as if they always implied identified assignments.

That was wrong for the core workflow. Distribution identity answers "who receives access." Response identity answers "whether the submitted answers are connected to a named respondent in product surfaces and exports." These are related but not the same.

## Decision

We will support anonymous invite-only distribution by allowing saved audience rules on anonymous waves. At launch, saved audience rules materialize anonymous email invitation assignments. The platform may keep operational delivery metadata for sending, retry, audit, and withdrawal workflows, but product reports and exports must continue to treat responses from this mode as anonymous.

Anonymous repeat-participation waves remain separate. Saved audience invitations for repeat participation are deferred until participant-code and invitation semantics are designed together.

## Alternatives considered

| Option | Pros | Cons | Why not |
|---|---|---|---|
| Anonymous means public link only | Simple UI and backend | Does not fit normal selected-research-audience workflow | Rejected because tenants need selected invitations without identified answers |
| Audience rules imply identified collection | Simple assignment model | Forces unnecessary identity exposure for common anonymous studies | Rejected because it confuses distribution identity with response identity |
| Anonymous invite-only distribution | Fits real study workflow; reuses existing anonymous invitation infrastructure | Requires clear privacy wording and export discipline | Chosen |
| Cryptographic unlinkability between invitations and responses | Stronger anonymity | Larger design: token scrubbing, delivery audit limits, reminder limits, abuse handling | Deferred |

## Consequences

### Positive

- Researchers can invite a selected audience while keeping answer reporting anonymous.
- Launch readiness can check recipient emails before collection starts.
- Existing anonymous token, assignment, notification, and outbox paths are reused.

### Negative / costs

- Operational metadata can still prove that a person was invited and whether a token was used. This is not the same as cryptographic anonymity.
- Product surfaces must avoid presenting anonymous invite-only responses as named respondent results.
- Repeat-participation invite-only semantics remain unresolved.

### Reversibility

Moderate. The response mode stays `anonymous`, so reversing the launch materialization path would not require a schema migration. However, if tenants rely on anonymous invite-only collection, removing it later would affect product behavior and documentation.

## Open questions raised

- Anonymous repeat-participation invite-only collection remains deferred until participant-code and invitation semantics are designed.
- Stronger unlinkability, token scrubbing, reminder rules, and delivery metadata retention need a future privacy/security design.

## References

- Internal: [custom study builder](../30-features/custom-study-builder.md)
- Internal: [ADR-0005 anonymous longitudinal self-code](0005-anonymous-longitudinal-self-code.md)
