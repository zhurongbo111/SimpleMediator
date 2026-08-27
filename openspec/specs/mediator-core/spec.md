## Purpose

Provides core mediator interfaces and implementation for request/response routing and notification dispatch in .NET applications.

## ADDED Requirements

### Requirement: Request/Response routing
The system SHALL route a request to its corresponding handler and return a response.

#### Scenario: Successful request handling
- **WHEN** a request implementing `IRequest<TResponse>` is sent via the mediator
- **THEN** the system invokes the registered `IRequestHandler<TRequest, TResponse>` and returns the response

#### Scenario: No handler registered
- **WHEN** a request is sent but no handler is registered for its type
- **THEN** the system SHALL throw a meaningful exception indicating no handler was found

### Requirement: Notification dispatch
The system SHALL dispatch a notification to all registered handlers.

#### Scenario: Multiple notification handlers
- **WHEN** a notification implementing `INotification` is published via the mediator
- **THEN** ALL registered `INotificationHandler<TNotification>` implementations are invoked

#### Scenario: No notification handlers
- **WHEN** a notification is published but no handlers are registered
- **THEN** the system SHALL complete without error (no-op)

### Requirement: IMediator interface
The system SHALL expose an `IMediator` interface as the primary entry point.

#### Scenario: Send request
- **WHEN** consumer calls `Send<TResponse>(IRequest<TResponse>)`
- **THEN** returns `Task<TResponse>` from the matched handler

#### Scenario: Publish notification
- **WHEN** consumer calls `Publish<TNotification>(TNotification)`
- **THEN** all handlers for that notification type are invoked concurrently

### Requirement: Handler registration
The system SHALL support registering handlers via dependency injection.

#### Scenario: Register handler via DI
- **WHEN** `AddSimpleMediator()` is called on `IServiceCollection`
- **THEN** all `IRequestHandler<,>` and `INotificationHandler<>` implementations in the assembly are registered

#### Scenario: Register handlers from specific assembly
- **WHEN** an assembly parameter is provided to `AddSimpleMediator()`
- **THEN** only handlers from that assembly are registered
