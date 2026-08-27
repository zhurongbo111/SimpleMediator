## Purpose

Provides a pipeline behavior chain for cross-cutting concerns, enabling pre/post processing of requests and responses.

## ADDED Requirements

### Requirement: Pipeline behavior chain
The system SHALL execute pipeline behaviors in registration order around request handling.

#### Scenario: Behavior wraps handler execution
- **WHEN** a request is sent and pipeline behaviors are registered
- **THEN** behaviors execute in order, each calling `next()` to proceed to the next behavior or the final handler

#### Scenario: Behavior can short-circuit
- **WHEN** a pipeline behavior does not call `next()`
- **THEN** the handler and subsequent behaviors are NOT executed, and the behavior returns its own response

### Requirement: Pre-processor execution
The system SHALL invoke pre-processors before the request handler executes.

#### Scenario: Pre-processor runs before handler
- **WHEN** a request has registered `IPreProcessor<TRequest>` implementations
- **THEN** all pre-processors are invoked before the handler processes the request

#### Scenario: Pre-processor can modify request context
- **WHEN** a pre-processor executes
- **THEN** it can inspect and enrich the request context passed to subsequent steps

### Requirement: Post-processor execution
The system SHALL invoke post-processors after the request handler executes.

#### Scenario: Post-processor runs after handler
- **WHEN** a request has registered `IPostProcessor<TRequest, TResponse>` implementations
- **THEN** all post-processors are invoked after the handler returns a response

#### Scenario: Post-processor can modify response
- **WHEN** a post-processor executes
- **THEN** it can inspect the response and context, but response modification is only possible if the pipeline supports it

### Requirement: Behavior ordering
The system SHALL execute behaviors in the order they were registered.

#### Scenario: Registration order preserved
- **WHEN** behaviors A, B, C are registered in that order
- **THEN** execution follows: A → B → C → Handler → PostProcessors
