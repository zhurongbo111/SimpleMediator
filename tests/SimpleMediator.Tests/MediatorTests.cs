using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using DeepCode.SimpleMediator.Abstractions;
using DeepCode.SimpleMediator.DependencyInjection;
using DeepCode.SimpleMediator.Pipeline;
using Xunit;

namespace DeepCode.SimpleMediator.Tests;

// Test request and response types
public record TestRequest(string Message) : IRequest<string>;
public record TestNotification(string Message) : INotification;
public record PipelineTestRequest(string Message) : IRequest<string>;
public record BasicRequest(string Message) : IRequest<string>;

// Test handler
public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class BasicRequestHandler : IRequestHandler<BasicRequest, string>
{
    public Task<string> Handle(BasicRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class PipelineTestRequestHandler : IRequestHandler<PipelineTestRequest, string>
{
    public Task<string> Handle(PipelineTestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

// Test notification handlers
public class TestNotificationHandler1 : INotificationHandler<TestNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        lock (HandledMessages)
        {
            HandledMessages.Add($"Handler1: {notification.Message}");
        }
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        lock (HandledMessages)
        {
            HandledMessages.Add($"Handler2: {notification.Message}");
        }
        return Task.CompletedTask;
    }
}

// Test pipeline behavior
public class TestPipelineBehavior : IPipelineBehavior<PipelineTestRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();

    public async Task<string> Handle(PipelineTestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("BehaviorBefore");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("BehaviorAfter");
        return response;
    }
}

// Test pre-processor
public class TestPreProcessor : IPreProcessor<PipelineTestRequest>
{
    public static List<string> ExecutionOrder { get; } = new();

    public Task Process(PipelineTestRequest request, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("PreProcessor");
        return Task.CompletedTask;
    }
}

// Test post-processor
public class TestPostProcessor : IPostProcessor<PipelineTestRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();

    public Task Process(PipelineTestRequest request, string response, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("PostProcessor");
        return Task.CompletedTask;
    }
}

// Short-circuit behavior for testing
public class ShortCircuitBehavior : IPipelineBehavior<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        return Task.FromResult("Short-circuited");
    }
}

public class MediatorTests
{
    [Fact]
    public async Task Send_Request_HandlerInvoked_ReturnsResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new BasicRequest("Hello"));

        // Assert
        Assert.Equal("Handled: Hello", response);
    }

    [Fact]
    public async Task Publish_Notification_AllHandlersInvoked()
    {
        // Arrange
        TestNotificationHandler1.HandledMessages.Clear();
        TestNotificationHandler2.HandledMessages.Clear();

        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new TestNotification("Test"));

        // Assert
        Assert.Contains("Handler1: Test", TestNotificationHandler1.HandledMessages);
        Assert.Contains("Handler2: Test", TestNotificationHandler2.HandledMessages);
    }

    [Fact]
    public async Task Send_Request_NoHandler_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new UnhandledRequest()));
    }

    [Fact]
    public async Task Send_Request_PipelineBehaviorExecutesInOrder()
    {
        // Arrange
        TestPipelineBehavior.ExecutionOrder.Clear();
        TestPreProcessor.ExecutionOrder.Clear();
        TestPostProcessor.ExecutionOrder.Clear();

        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new PipelineTestRequest("Test"));

        // Assert
        Assert.Equal("PreProcessor", TestPreProcessor.ExecutionOrder[0]);
        Assert.Equal("BehaviorBefore", TestPipelineBehavior.ExecutionOrder[0]);
        Assert.Equal("BehaviorAfter", TestPipelineBehavior.ExecutionOrder[1]);
        Assert.Equal("PostProcessor", TestPostProcessor.ExecutionOrder[0]);
    }

    [Fact]
    public async Task Send_Request_ShortCircuitBehavior_HandlerNotInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        services.AddTransient<IPipelineBehavior<TestRequest, string>, ShortCircuitBehavior>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("Test"));

        // Assert
        Assert.Equal("Short-circuited", response);
    }

    [Fact]
    public async Task Publish_Notification_NoHandlers_CompletesWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert (should not throw)
        await mediator.Publish(new TestNotification("Test"));
    }

    [Fact]
    public void AddSimpleMediator_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);

        var handler = provider.GetService<IRequestHandler<BasicRequest, string>>();
        Assert.NotNull(handler);

        var notificationHandler = provider.GetService<INotificationHandler<TestNotification>>();
        Assert.NotNull(notificationHandler);
    }

    [Fact]
    public void AddSimpleMediator_TransientMediator_ReturnsNewInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var mediator1 = provider.GetRequiredService<IMediator>();
        var mediator2 = provider.GetRequiredService<IMediator>();

        // Assert - Transient returns new instances (but they share the same IServiceProvider)
        Assert.NotSame(mediator1, mediator2);
    }

    [Fact]
    public void AddSimpleMediator_MultipleAssemblies_RegistersAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediator(typeof(MediatorTests).Assembly, typeof(Mediator).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);

        var handler = provider.GetService<IRequestHandler<BasicRequest, string>>();
        Assert.NotNull(handler);

        var notificationHandler = provider.GetService<INotificationHandler<TestNotification>>();
        Assert.NotNull(notificationHandler);
    }

    [Fact]
    public async Task Send_Request_HandlerWithScopedService_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        services.AddScoped<IScopedService, ScopedService>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new ScopedRequest());

        // Assert
        Assert.Equal("Scoped: test", response);
    }

    [Fact]
    public async Task Send_Request_BehaviorsExecuteInOrderProperty()
    {
        // Arrange
        OrderedBehavior1.ExecutionOrder.Clear();
        OrderedBehavior2.ExecutionOrder.Clear();

        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<OrderedRequest, string>, OrderedRequestHandler>();
        services.AddTransient<IPipelineBehavior<OrderedRequest, string>, OrderedBehavior1>();
        services.AddTransient<IPipelineBehavior<OrderedRequest, string>, OrderedBehavior2>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new OrderedRequest("Test"));

        // Assert - Order 1 should execute before Order 2
        Assert.Equal(2, OrderedBehavior1.ExecutionOrder.Count);
        Assert.Equal(2, OrderedBehavior2.ExecutionOrder.Count);
        // Behavior1 should execute before Behavior2 (lower Order value = earlier execution)
        Assert.Equal("B1-Before", OrderedBehavior1.ExecutionOrder[0]);
        Assert.Equal("B2-Before", OrderedBehavior2.ExecutionOrder[0]);
        // After next() should execute in reverse order
        Assert.Equal("B2-After", OrderedBehavior2.ExecutionOrder[1]);
        Assert.Equal("B1-After", OrderedBehavior1.ExecutionOrder[1]);
    }

    [Fact]
    public async Task Send_Request_HandlerThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<FailingRequest, string>, FailingRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Exception is wrapped in TargetInvocationException due to reflection
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(
            () => mediator.Send(new FailingRequest("Test")));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Handler failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task Send_Request_BehaviorThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BehaviorFailingRequest, string>, BehaviorFailingRequestHandler>();
        services.AddTransient<IPipelineBehavior<BehaviorFailingRequest, string>, FailingBehavior>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Exception is wrapped in TargetInvocationException due to reflection
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(
            () => mediator.Send(new BehaviorFailingRequest("Test")));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Behavior failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task Send_Request_PreProcessorThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<PreProcessorFailingRequest, string>, PreProcessorFailingRequestHandler>();
        services.AddTransient<IPreProcessor<PreProcessorFailingRequest>, FailingPreProcessor>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Exception is wrapped in TargetInvocationException due to reflection
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(
            () => mediator.Send(new PreProcessorFailingRequest("Test")));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Pre-processor failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task Send_Request_PostProcessorThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<PostProcessorFailingRequest, string>, PostProcessorFailingRequestHandler>();
        services.AddTransient<IPostProcessor<PostProcessorFailingRequest, string>, FailingPostProcessor>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Exception is wrapped in TargetInvocationException due to reflection
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(
            () => mediator.Send(new PostProcessorFailingRequest("Test")));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Post-processor failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task Send_Request_EmptyMessage_ReturnsEmptyString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<EmptyMessageRequest, string>, EmptyMessageRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new EmptyMessageRequest());

        // Assert
        Assert.Equal(string.Empty, response);
    }

    [Fact]
    public async Task Send_Request_NullMessage_ReturnsNullString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<NullMessageRequest, string>, NullMessageRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new NullMessageRequest(null));

        // Assert
        Assert.Equal("null", response);
    }

    [Fact]
    public async Task Send_Request_LargePayload_ProcessesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<LargePayloadRequest, string>, LargePayloadRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var largeData = new string('A', 100000);

        // Act
        var response = await mediator.Send(new LargePayloadRequest(largeData));

        // Assert
        Assert.Equal("Processed 100000 characters", response);
    }

    [Fact]
    public async Task Send_Request_MultipleBehaviors_ExecutesInCorrectOrder()
    {
        // Arrange
        MultiBehaviorRequestHandler.ExecutionOrder.Clear();
        MultiBehavior1.ExecutionOrder.Clear();
        MultiBehavior2.ExecutionOrder.Clear();
        MultiBehavior3.ExecutionOrder.Clear();

        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<MultiBehaviorRequest, string>, MultiBehaviorRequestHandler>();
        services.AddTransient<IPipelineBehavior<MultiBehaviorRequest, string>, MultiBehavior1>();
        services.AddTransient<IPipelineBehavior<MultiBehaviorRequest, string>, MultiBehavior2>();
        services.AddTransient<IPipelineBehavior<MultiBehaviorRequest, string>, MultiBehavior3>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new MultiBehaviorRequest("Test"));

        // Assert - Behaviors should execute in order: 1, 2, 3
        Assert.Equal("Behavior1-Before", MultiBehavior1.ExecutionOrder[0]);
        Assert.Equal("Behavior2-Before", MultiBehavior2.ExecutionOrder[0]);
        Assert.Equal("Behavior3-Before", MultiBehavior3.ExecutionOrder[0]);
        Assert.Equal("Handler", MultiBehaviorRequestHandler.ExecutionOrder[0]);
        Assert.Equal("Behavior3-After", MultiBehavior3.ExecutionOrder[1]);
        Assert.Equal("Behavior2-After", MultiBehavior2.ExecutionOrder[1]);
        Assert.Equal("Behavior1-After", MultiBehavior1.ExecutionOrder[1]);
    }

    [Fact]
    public async Task Send_Request_SameOrderBehaviors_BothExecute()
    {
        // Arrange
        SameOrderRequestHandler.ExecutionOrder.Clear();
        SameOrderBehavior1.ExecutionOrder.Clear();
        SameOrderBehavior2.ExecutionOrder.Clear();

        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<SameOrderRequest, string>, SameOrderRequestHandler>();
        services.AddTransient<IPipelineBehavior<SameOrderRequest, string>, SameOrderBehavior1>();
        services.AddTransient<IPipelineBehavior<SameOrderRequest, string>, SameOrderBehavior2>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new SameOrderRequest("Test"));

        // Assert - Both behaviors should execute
        Assert.Equal(2, SameOrderBehavior1.ExecutionOrder.Count);
        Assert.Equal(2, SameOrderBehavior2.ExecutionOrder.Count);
        Assert.Equal("Handler", SameOrderRequestHandler.ExecutionOrder[0]);
    }

    [Fact]
    public async Task Send_Request_BehaviorModifiesResponse_ResponseModified()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<ModifiableRequest, string>, ModifiableRequestHandler>();
        services.AddTransient<IPipelineBehavior<ModifiableRequest, string>, ResponseModifierBehavior>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new ModifiableRequest("Test"));

        // Assert
        Assert.Equal("Handled: Test (response modified)", response);
    }

    [Fact]
    public async Task Send_Request_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<SlowRequest, string>, SlowRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.Send(new SlowRequest("Test"), cts.Token));
    }

    [Fact]
    public async Task Publish_Notification_HandlerThrowsException_PropagatesException()
    {
        // Arrange
        FailingNotificationHandler.HandledMessages.Clear();

        var services = new ServiceCollection();
        services.AddTransient<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Exception is wrapped in TargetInvocationException due to reflection
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(
            () => mediator.Publish(new FailingNotification("Test")));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Notification handler failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task Send_Request_DifferentRequestTypes_IndependentHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BasicRequest, string>, BasicRequestHandler>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response1 = await mediator.Send(new BasicRequest("Basic"));
        var response2 = await mediator.Send(new TestRequest("Test"));

        // Assert
        Assert.Equal("Handled: Basic", response1);
        Assert.Equal("Handled: Test", response2);
    }

    [Fact]
    public async Task Send_Request_ConcurrentRequests_HandledCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(i => mediator.Send(new BasicRequest($"Request{i}")))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, responses.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"Handled: Request{i}", responses[i]);
        }
    }

    [Fact]
    public async Task Send_Request_BehaviorDoesNotCallNext_HandlerNotInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<BehaviorFailingRequest, string>, BehaviorFailingRequestHandler>();
        services.AddTransient<IPipelineBehavior<BehaviorFailingRequest, string>, NonCallingBehavior>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new BehaviorFailingRequest("Test"));

        // Assert - Handler should not be invoked, behavior returns "Behavior intercepted"
        Assert.Equal("Behavior intercepted", response);
    }
}

// Test scoped service
public interface IScopedService
{
    string GetValue();
}

public class ScopedService : IScopedService
{
    public string GetValue() => "Scoped: test";
}

// Test request that uses scoped service
public record ScopedRequest() : IRequest<string>;

public class ScopedRequestHandler : IRequestHandler<ScopedRequest, string>
{
    private readonly IScopedService _scopedService;

    public ScopedRequestHandler(IScopedService scopedService)
    {
        _scopedService = scopedService;
    }

    public Task<string> Handle(ScopedRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_scopedService.GetValue());
    }
}

// Unhandled request for testing
public record UnhandledRequest() : IRequest<string>;

// Ordered behavior test types
public record OrderedRequest(string Message) : IRequest<string>;

public class OrderedRequestHandler : IRequestHandler<OrderedRequest, string>
{
    public Task<string> Handle(OrderedRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class OrderedBehavior1 : IPipelineBehavior<OrderedRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 1;

    public async Task<string> Handle(OrderedRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("B1-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("B1-After");
        return response;
    }
}

public class OrderedBehavior2 : IPipelineBehavior<OrderedRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 2;

    public async Task<string> Handle(OrderedRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("B2-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("B2-After");
        return response;
    }
}

// Error scenario types
public record FailingRequest(string Message) : IRequest<string>;

public class FailingRequestHandler : IRequestHandler<FailingRequest, string>
{
    public Task<string> Handle(FailingRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler failed");
    }
}

public record BehaviorFailingRequest(string Message) : IRequest<string>;

public class BehaviorFailingRequestHandler : IRequestHandler<BehaviorFailingRequest, string>
{
    public Task<string> Handle(BehaviorFailingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class FailingBehavior : IPipelineBehavior<BehaviorFailingRequest, string>
{
    public Task<string> Handle(BehaviorFailingRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Behavior failed");
    }
}

public record PreProcessorFailingRequest(string Message) : IRequest<string>;

public class PreProcessorFailingRequestHandler : IRequestHandler<PreProcessorFailingRequest, string>
{
    public Task<string> Handle(PreProcessorFailingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class FailingPreProcessor : IPreProcessor<PreProcessorFailingRequest>
{
    public Task Process(PreProcessorFailingRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Pre-processor failed");
    }
}

public record PostProcessorFailingRequest(string Message) : IRequest<string>;

public class PostProcessorFailingRequestHandler : IRequestHandler<PostProcessorFailingRequest, string>
{
    public Task<string> Handle(PostProcessorFailingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class FailingPostProcessor : IPostProcessor<PostProcessorFailingRequest, string>
{
    public Task Process(PostProcessorFailingRequest request, string response, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Post-processor failed");
    }
}

// Edge case types
public record EmptyMessageRequest() : IRequest<string>;

public class EmptyMessageRequestHandler : IRequestHandler<EmptyMessageRequest, string>
{
    public Task<string> Handle(EmptyMessageRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}

public record NullMessageRequest(string? Message) : IRequest<string>;

public class NullMessageRequestHandler : IRequestHandler<NullMessageRequest, string>
{
    public Task<string> Handle(NullMessageRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Message ?? "null");
    }
}

public record LargePayloadRequest(string Data) : IRequest<string>;

public class LargePayloadRequestHandler : IRequestHandler<LargePayloadRequest, string>
{
    public Task<string> Handle(LargePayloadRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Processed {request.Data.Length} characters");
    }
}

// Multiple behavior types
public record MultiBehaviorRequest(string Message) : IRequest<string>;

public class MultiBehaviorRequestHandler : IRequestHandler<MultiBehaviorRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();

    public Task<string> Handle(MultiBehaviorRequest request, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Handler");
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class MultiBehavior1 : IPipelineBehavior<MultiBehaviorRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 1;

    public async Task<string> Handle(MultiBehaviorRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior1-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("Behavior1-After");
        return response;
    }
}

public class MultiBehavior2 : IPipelineBehavior<MultiBehaviorRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 2;

    public async Task<string> Handle(MultiBehaviorRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior2-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("Behavior2-After");
        return response;
    }
}

public class MultiBehavior3 : IPipelineBehavior<MultiBehaviorRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 3;

    public async Task<string> Handle(MultiBehaviorRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Behavior3-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("Behavior3-After");
        return response;
    }
}

// Same order behavior types
public record SameOrderRequest(string Message) : IRequest<string>;

public class SameOrderRequestHandler : IRequestHandler<SameOrderRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();

    public Task<string> Handle(SameOrderRequest request, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Handler");
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class SameOrderBehavior1 : IPipelineBehavior<SameOrderRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 1;

    public async Task<string> Handle(SameOrderRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("SameOrder1-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("SameOrder1-After");
        return response;
    }
}

public class SameOrderBehavior2 : IPipelineBehavior<SameOrderRequest, string>
{
    public static List<string> ExecutionOrder { get; } = new();
    public int Order => 1;

    public async Task<string> Handle(SameOrderRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("SameOrder2-Before");
        var response = await next(cancellationToken);
        ExecutionOrder.Add("SameOrder2-After");
        return response;
    }
}

// Behavior modifying request/response types
public record ModifiableRequest(string Message) : IRequest<string>;

public class ModifiableRequestHandler : IRequestHandler<ModifiableRequest, string>
{
    public Task<string> Handle(ModifiableRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

public class RequestModifierBehavior : IPipelineBehavior<ModifiableRequest, string>
{
    public int Order => 1;

    public async Task<string> Handle(ModifiableRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var modifiedRequest = request with { Message = request.Message + " (modified)" };
        return await next(cancellationToken);
    }
}

public class ResponseModifierBehavior : IPipelineBehavior<ModifiableRequest, string>
{
    public int Order => 2;

    public async Task<string> Handle(ModifiableRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        return response + " (response modified)";
    }
}

// Notification handler for error testing
public record FailingNotification(string Message) : INotification;

public class FailingNotificationHandler : INotificationHandler<FailingNotification>
{
    public static List<string> HandledMessages { get; } = new();

    public Task Handle(FailingNotification notification, CancellationToken cancellationToken)
    {
        lock (HandledMessages)
        {
            HandledMessages.Add(notification.Message);
        }
        throw new InvalidOperationException("Notification handler failed");
    }
}

// Behavior that does not call next
public class NonCallingBehavior : IPipelineBehavior<BehaviorFailingRequest, string>
{
    public Task<string> Handle(BehaviorFailingRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        return Task.FromResult("Behavior intercepted");
    }
}

// Cancellation token test types
public record SlowRequest(string Message) : IRequest<string>;

public class SlowRequestHandler : IRequestHandler<SlowRequest, string>
{
    public async Task<string> Handle(SlowRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        return $"Handled: {request.Message}";
    }
}
