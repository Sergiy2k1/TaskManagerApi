# ADR 0001: Keep authentication access-token-only

- Status: Accepted
- Date: 2026-09-16

## Context

The API currently authenticates users with a signed JWT access token issued after successful email/password login.

The existing baseline already includes:

- ASP.NET Core Identity password hashing;
- generic invalid-credential responses that do not reveal whether an email exists;
- issuer, audience, signing-key, and lifetime validation;
- a unique JWT `jti`;
- a short default access-token lifetime of 15 minutes;
- rate limiting on login and registration;
- HTTPS-ready bearer-token semantics;
- no server-side session table.

The remaining question was whether refresh tokens should be added only to make the authentication design appear more complete.

Refresh tokens are not a small isolated feature. A production-quality implementation would also need a persistent session model, hashed refresh-token storage, token rotation, reuse detection, revocation/logout semantics, cleanup/expiry rules, database migrations, race handling, and integration tests.

Adding only a long-lived refresh-token string without those lifecycle controls would increase risk and complexity without demonstrating a sound session design.

## Decision

Keep the current authentication model access-token-only for this project.

Access tokens remain short-lived and stateless. Clients authenticate again after the access token expires.

Refresh tokens and persistent sessions will be introduced only if a real requirement appears for one or more of the following:

- long-lived client sessions without repeated credential entry;
- explicit logout/revocation across devices;
- per-device session management;
- immediate server-side session invalidation;
- mobile/browser UX that requires silent token renewal.

This is a deliberate scope decision, not an unfinished placeholder.

## Consequences

### Positive

- authentication remains small and auditable;
- no long-lived bearer credential is stored by the API;
- no refresh-token rotation/reuse protocol has to be implemented partially;
- the API remains stateless between authenticated requests;
- the architecture stays aligned with the project's modular-monolith scope.

### Trade-offs

- clients must log in again after the short access token expires;
- a stolen access token remains valid until its expiry unless the signing key is rotated;
- deactivating a user or changing a password does not revoke an already-issued access token immediately;
- there is no per-device session list or logout endpoint.

The 15-minute lifetime deliberately bounds those trade-offs. If immediate revocation becomes a requirement, refresh tokens alone would not solve it; the design would need server-side session/revocation state or another token-validation mechanism.

## Alternatives considered

### Add refresh tokens now

Rejected for the current scope because a proper implementation requires session persistence, rotation, reuse detection, revocation, expiry cleanup, and additional concurrency/security handling. Implementing only the token endpoint would create a weaker design.

### Increase access-token lifetime

Rejected because it improves convenience by increasing the exposure window of a stolen bearer token.

### Validate the user against the database on every request

Rejected for now because it removes most of the stateless-token benefit and adds a database dependency to every authenticated request. It can be revisited if immediate account-state enforcement becomes a requirement.
