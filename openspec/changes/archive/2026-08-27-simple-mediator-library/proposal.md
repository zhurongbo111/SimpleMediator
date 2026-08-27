## Why

Existing .NET mediator libraries like MediatR are feature-rich but come with complexity and overhead that isn't always needed. Many projects only need a subset of mediator functionality but still pull in the full library. A lightweight, focused mediator implementation for .NET 8+ would provide a simpler alternative with clean DI integration, published as a reusable NuGet package.

## What Changes

- New .NET 8+ class library implementing the Mediator pattern
- Request/Response support (`IRequest<T>`, `IRequestHandler<TRequest, TResponse>`)
- Notification support (`INotification`, `INotificationHandler<TNotification>`)
- Pipeline behaviors for cross-cutting concerns (`IPipelineBehavior<TRequest, TResponse>`)
- Pre/Post processors for request processing hooks (`IPreProcessor<TRequest>`, `IPostProcessor<TRequest, TResponse>`)
- DI integration via `Microsoft.Extensions.DependencyInjection` extension methods
- NuGet package configuration for publishing

## Capabilities

### New Capabilities

- `mediator-core`: Core mediator interfaces and implementation - request/response routing, notification dispatch, and the `IMediator` contract
- `pipeline-behaviors`: Pipeline behavior chain for cross-cutting concerns with pre/post processor support
- `di-integration`: Dependency injection integration with service registration extensions

### Modified Capabilities

<!-- No existing capabilities to modify -->

## Impact

- New class library project targeting `net8.0`
- Dependency on `Microsoft.Extensions.DependencyInjection.Abstractions` for DI integration
- Source code organized under `src/SimpleMediator/` namespace
- NuGet package metadata in `.csproj` for publishing
- No existing code affected - greenfield project
