## 1. Project Setup

- [x] 1.1 Create class library project at `src/SimpleMediator/SimpleMediator.csproj` targeting `net8.0` with `Microsoft.Extensions.DependencyInjection.Abstractions` dependency, verify project builds with `dotnet build`
- [x] 1.2 Configure NuGet package metadata in `.csproj` (PackageId, Version, Description, Authors, PackageTags), verify with `dotnet pack`

## 2. Core Abstractions

- [x] 2.1 Create `IRequest<TResponse>` interface in `SimpleMediator.Abstractions` namespace, verify interface compiles
- [x] 2.2 Create `IRequestHandler<TRequest, TResponse>` interface with `Handle(TRequest, CancellationToken)` method, verify interface compiles
- [x] 2.3 Create `INotification` marker interface in `SimpleMediator.Abstractions` namespace, verify interface compiles
- [x] 2.4 Create `INotificationHandler<TNotification>` interface with `Handle(TNotification, CancellationToken)` method, verify interface compiles

## 3. Pipeline Abstractions

- [x] 3.1 Create `RequestHandlerDelegate<TResponse>` delegate type for pipeline next-step invocation, verify compiles
- [x] 3.2 Create `IPipelineBehavior<TRequest, TResponse>` interface with `Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)` method, verify compiles
- [x] 3.3 Create `IPreProcessor<TRequest>` interface with `Process(TRequest, CancellationToken)` method, verify compiles
- [x] 3.4 Create `IPostProcessor<TRequest, TResponse>` interface with `Process(TRequest, TResponse, CancellationToken)` method, verify compiles

## 4. Mediator Interface

- [x] 4.1 Create `IMediator` interface with `Send<TResponse>(IRequest<TResponse>, CancellationToken)` and `Publish<TNotification>(TNotification, CancellationToken)` methods, verify interface compiles

## 5. Mediator Implementation

- [x] 5.1 Create `Mediator` class implementing `IMediator` using `IServiceProvider`, verify class compiles
- [x] 5.2 Implement `Send` method: resolve handler, build pipeline with behaviors, invoke pre-processors, handler, post-processors, verify with unit test
- [x] 5.3 Implement `Publish` method: resolve all notification handlers, invoke concurrently via `Task.WhenAll`, verify with unit test
- [x] 5.4 Add exception handling for missing handlers (`InvalidOperationException`), verify exception is thrown when no handler registered

## 6. Pipeline Behavior Chain

- [x] 6.1 Implement pipeline builder that chains `IPipelineBehavior` instances, verify behaviors execute in registration order
- [x] 6.2 Implement pre-processor execution before handler in pipeline, verify pre-processor runs before handler
- [x] 6.3 Implement post-processor execution after handler in pipeline, verify post-processor runs after handler
- [x] 6.4 Support behavior short-circuiting (not calling `next()`), verify handler is not invoked when behavior short-circuits

## 7. DI Integration

- [x] 7.1 Create `SimpleMediatorServiceCollectionExtensions` class with `AddSimpleMediator()` extension method on `IServiceCollection`, verify extension method is discoverable
- [x] 7.2 Implement assembly scanning to register all `IRequestHandler<,>` implementations as transient, verify handlers are resolved from DI
- [x] 7.3 Implement assembly scanning to register all `INotificationHandler<>` implementations as transient, verify notification handlers are resolved from DI
- [x] 7.4 Register `IMediator` as singleton in DI, verify same instance returned on multiple resolutions
- [x] 7.5 Register `IPipelineBehavior<,>` implementations in DI, verify pipeline behaviors are resolved
- [x] 7.6 Register `IPreProcessor<>` and `IPostProcessor<,>` implementations in DI, verify processors are resolved
- [x] 7.7 Support custom assembly parameter for handler scanning, verify only specified assembly handlers are registered

## 8. NuGet Package

- [x] 8.1 Configure package license, repository URL, and README in `.csproj`, verify package metadata with `dotnet pack --dry-run`
- [x] 8.2 Add XML documentation comments to all public interfaces and classes, verify with `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

## 9. Tests

- [x] 9.1 Create test project at `tests/SimpleMediator.Tests/SimpleMediator.Tests.csproj` with xUnit, verify test project builds
- [x] 9.2 Write unit tests for request/response flow: send request → handler invoked → response returned, verify test passes
- [x] 9.3 Write unit tests for notification flow: publish notification → all handlers invoked concurrently, verify test passes
- [x] 9.4 Write unit tests for pipeline behaviors: behavior chain executes in order, verify test passes
- [x] 9.5 Write unit tests for pre/post processors: processors run at correct pipeline stages, verify test passes
- [x] 9.6 Write unit tests for DI integration: `AddSimpleMediator()` registers all services correctly, verify test passes
- [x] 9.7 Write unit tests for error cases: missing handler throws, empty notifications handled gracefully, verify tests pass
