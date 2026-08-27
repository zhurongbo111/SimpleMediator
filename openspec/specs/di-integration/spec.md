## Purpose

Provides dependency injection integration for registering mediator services and handlers with Microsoft.Extensions.DependencyInjection.

## ADDED Requirements

### Requirement: Service collection extension
The system SHALL provide an `AddSimpleMediator()` extension method on `IServiceCollection`.

#### Scenario: Add mediator with default assembly scanning
- **WHEN** `services.AddSimpleMediator()` is called
- **THEN** the calling assembly is scanned for all handler implementations and registered in DI

#### Scenario: Add mediator with custom assembly
- **WHEN** `services.AddSimpleMediator(typeof(MyHandler).Assembly)` is called
- **THEN** the specified assembly is scanned for handler implementations

#### Scenario: Add mediator with configuration callback
- **WHEN** `services.AddSimpleMediator(options => { ... })` is called
- **THEN** the configuration callback can customize registration behavior

### Requirement: Transient handler registration
The system SHALL register handlers as transient services by default.

#### Scenario: Handler lifetime is transient
- **WHEN** handlers are registered via `AddSimpleMediator()`
- **THEN** each resolution of a handler returns a new instance

### Requirement: IMediator registration
The system SHALL register `IMediator` as a singleton service.

#### Scenario: Singleton mediator
- **WHEN** `AddSimpleMediator()` is called
- **THEN** `IMediator` is registered as singleton and the same instance is returned on subsequent resolutions

### Requirement: Pipeline behavior registration
The system SHALL register pipeline behaviors when configured.

#### Scenario: Register pipeline behaviors
- **WHEN** `AddSimpleMediator()` is called
- **THEN** `IPipelineBehavior<,>` implementations are registered in DI

### Requirement: Pre/Post processor registration
The system SHALL register pre-processors and post-processors when configured.

#### Scenario: Register pre-processors
- **WHEN** `AddSimpleMediator()` is called
- **THEN** `IPreProcessor<>` implementations are registered in DI

#### Scenario: Register post-processors
- **WHEN** `AddSimpleMediator()` is called
- **THEN** `IPostProcessor<,>` implementations are registered in DI
