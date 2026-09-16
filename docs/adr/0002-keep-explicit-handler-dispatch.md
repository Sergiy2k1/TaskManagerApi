# ADR 0002: Keep explicit handler dispatch

- Status: Accepted
- Date: 2026-09-16

## Context

The application layer uses small generic interfaces:

```csharp
ICommandHandler<TCommand, TResult>
IQueryHandler<TQuery, TResult>
```

ASP.NET Core controllers receive the concrete closed handler interfaces they use through constructor injection. The largest current example is `ProjectsController`, which coordinates project lifecycle, project queries, and project-member endpoints and therefore has a comparatively long constructor.

A mediator/dispatcher abstraction such as MediatR could reduce the number of constructor parameters by replacing explicit handlers with one dispatcher dependency. The question is whether that indirection is justified for the current modular-monolith scope.

## Decision

Keep explicit handler injection and do not add MediatR or a custom service-locator-style dispatcher at this stage.

The long constructor is treated as a signal about controller responsibility, not as sufficient justification for hiding all use-case dependencies behind one generic dispatcher.

If the HTTP surface grows further, the preferred first refactoring is to split controllers by cohesive resource area while preserving the same application handlers and routes. For example, project-member endpoints can move from `ProjectsController` to a dedicated controller before introducing a mediator.

## Consequences

### Positive

- controller dependencies remain visible at compile time;
- handler invocation is direct and easy to trace while debugging;
- there is no additional mediator package or custom dispatch infrastructure;
- registration failures remain ordinary ASP.NET Core DI failures rather than runtime handler-resolution failures;
- the CQRS-style separation stays visible without framework coupling.

### Trade-offs

- controllers with many use cases can have long constructors;
- handler registrations are explicit and somewhat repetitive;
- adding cross-cutting handler pipeline behaviors would require deliberate infrastructure rather than mediator pipeline hooks.

These costs are acceptable at the current scale. Constructor growth should be addressed first by reviewing controller cohesion and boundaries.

## Alternatives considered

### Add MediatR

Rejected for now. It would shorten constructors and provide pipeline behaviors, but the current application does not need enough dispatch/pipeline functionality to justify another dependency and more implicit control flow.

### Add a custom `ICommandDispatcher` / `IQueryDispatcher`

Rejected because it would recreate mediator behavior locally while adding service resolution and indirection without solving a current requirement.

### Inject `IServiceProvider` and resolve handlers inside actions

Rejected because it hides dependencies, weakens constructor-level validation, and turns normal dependency injection into a service-locator pattern.

### Split large controllers

Accepted as the preferred future response if controller responsibilities continue to grow. It reduces constructor size by improving HTTP-layer cohesion rather than hiding dependencies.
