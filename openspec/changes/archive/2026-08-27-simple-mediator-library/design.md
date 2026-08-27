## Context

This is a greenfield .NET 8+ class library project. No existing code exists. The library will be published to NuGet as a reusable package. The project targets `net8.0` and depends on `Microsoft.Extensions.DependencyInjection.Abstractions` for DI integration.

## Goals / Non-Goals

**Goals:**
- Provide a lightweight mediator pattern implementation for .NET 8+
- Support Request/Response, Notifications, Pipeline Behaviors, and Pre/Post Processors
- Integrate seamlessly with Microsoft.Extensions.DependencyInjection
- Publish as a NuGet package for easy reuse
- Keep the API surface minimal and focused

**Non-Goals:**
- Backward compatibility with older .NET versions (.NET 6, .NET Standard)
- Feature parity with MediatR (no validation, no streaming, no enumerable requests)
- Support for custom DI containers
- Source generators or compile-time registration

## Decisions

### D1: Project Structure
**Decision:** Single class library project at `src/SimpleMediator/`

**Rationale:** Simple, focused library. No need for multiple projects at this stage.

**Alternatives considered:**
- Separate projects for core/DI/pipeline → Rejected: adds complexity for a small library

### D2: Handler Discovery Strategy
**Decision:** Assembly scanning via `Microsoft.Extensions.DependencyInjection` registration

**Rationale:** Standard .NET pattern. Handlers are auto-discovered and registered when `AddSimpleMediator()` is called.

**Alternatives considered:**
- Manual registration → Rejected: verbose, error-prone
- Source generators → Rejected: adds complexity, requires Roslyn tooling

### D3: Pipeline Behavior Pattern
**Decision:** Decorator pattern using `IPipelineBehavior<TRequest, TResponse>` with `Func<TRequest, CancellationToken, Task<TResponse>>` next delegate

**Rationale:** Proven pattern from MediatR. Behaviors wrap around each other, each calling `next()` to proceed.

**Alternatives considered:**
- Middleware pattern → Rejected: less familiar to .NET developers
- Aspect-oriented approach → Rejected: requires proxy generation

### D4: Notification Dispatch
**Decision:** Concurrent invocation of all handlers via `Task.WhenAll`

**Rationale:** Notifications are fire-and-forget. Concurrent execution maximizes throughput.

**Alternatives considered:**
- Sequential invocation → Rejected: slower, notifications don't need ordering guarantees

### D5: NuGet Package Configuration
**Decision:** Include metadata in `.csproj` file using MSBuild properties

**Rationale:** Standard .NET approach. No need for separate `.nuspec` file.

### D6: Namespace Convention
**Decision:** Root namespace `SimpleMediator` with sub-namespaces for features

**Structure:**
```
SimpleMediator                    → IMediator
SimpleMediator.Abstractions       → IRequest, IRequestHandler, etc.
SimpleMediator.Pipeline           → IPipelineBehavior, IPreProcessor, IPostProcessor
SimpleMediator.DependencyInjection → AddSimpleMediator extension
```

**Rationale:** Clean separation of concerns. Users only reference what they need.

## Risks / Trade-offs

- **[Risk] Limited feature set** → Users needing advanced features (validation, streaming) may outgrow this library → Mitigation: Document limitations clearly, provide extension points
- **[Risk] .NET 8+ only** → Excludes users on older frameworks → Mitigation: Acceptable for "simple" library; can add netstandard2.0 target later if demand exists
- **[Trade-off] Simplicity over features** → Fewer features than MediatR → Intentional: this is the value proposition
- **[Trade-off] Single assembly** → All features in one package → Users get everything even if they only need request/response → Could split later if package size matters
