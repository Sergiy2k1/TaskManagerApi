# ADR 0003: Use explicit version optimistic concurrency

- Status: Accepted
- Date: 2026-09-16

## Context

`Project` and `TaskItem` are editable resources that can be read by one request and updated later by another. Without concurrency protection, two clients can read the same state and the later write can silently overwrite the earlier write.

The project uses PostgreSQL through EF Core. Possible concurrency approaches included an explicit numeric version, PostgreSQL system columns such as `xmin`, HTTP ETags, or accepting last-write-wins behavior.

The API also needs a concurrency model that is visible in contracts and easy to explain independently of one database provider.

## Decision

Use an explicit `long Version` property on `Project` and `TaskItem` and configure it as an EF Core concurrency token.

Clients receive the current version when reading an editable resource and send the expected version back on update. The application rejects an already-stale version before applying domain changes. On save, EF Core also uses the originally tracked version as part of the database update condition, so a race that occurs after the application-level check is still detected.

Successful updates increment the version. Concurrency conflicts are translated to application conflict semantics and returned as HTTP `409 Conflict`.

## Consequences

### Positive

- the concurrency contract is explicit to API clients;
- the mechanism is database-provider-independent at the domain/application boundary;
- stale disconnected edits are detected before mutation where possible;
- true database races are still detected by EF Core;
- the version is straightforward to test in unit, integration, and API tests.

### Trade-offs

- clients must round-trip the version value on editable PUT operations;
- application code must increment and expose the version consistently;
- the version is an implementation-level concurrency token rather than a semantic business revision number.

## Alternatives considered

### PostgreSQL `xmin`

Rejected for this project because it couples the concurrency contract to PostgreSQL internals and is less explicit in the API/domain model. It remains a valid provider-specific optimization in systems where that trade-off is desirable.

### HTTP ETag / `If-Match`

Not used as the primary mechanism. ETags would provide strong HTTP semantics, but an explicit version field keeps the concurrency model visible in the current JSON contracts and application layer. An ETag representation could be added later on top of the same version token if HTTP cache/concurrency requirements justify it.

### Last write wins

Rejected because it allows silent lost updates when concurrent users edit the same project or task.

### Explicit database locking

Rejected because pessimistic locking would add contention and transaction complexity for a collaborative API where optimistic conflicts are expected to be uncommon.
