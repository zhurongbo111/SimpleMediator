using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Abstractions;
using SimpleMediator.DependencyInjection;
using SimpleMediator.Pipeline;
using Xunit;

namespace SimpleMediator.Tests;

// Test request and response types
public record TestRequest(string Message) : IRequest<string>;
public record TestNotification(string Message) : INotification;
public record PipelineTestRequest(string Message) : IRequest<string>;

// Test handler
public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
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

// Short-circuit behavior
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
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("Hello"));

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
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler1>();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler2>();
        services.AddSingleton<IMediator, Mediator>();
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
        services.AddSingleton<IMediator, Mediator>();
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
        services.AddTransient<IRequestHandler<PipelineTestRequest, string>, PipelineTestRequestHandler>();
        services.AddTransient<IPipelineBehavior<PipelineTestRequest, string>, TestPipelineBehavior>();
        services.AddTransient<IPreProcessor<PipelineTestRequest>, TestPreProcessor>();
        services.AddTransient<IPostProcessor<PipelineTestRequest, string>, TestPostProcessor>();
        services.AddSingleton<IMediator, Mediator>();
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
        services.AddTransient<IPipelineBehavior<TestRequest, string>, ShortCircuitBehavior>();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();
        services.AddSingleton<IMediator, Mediator>();
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
        services.AddSingleton<IMediator, Mediator>();
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

        var handler = provider.GetService<IRequestHandler<TestRequest, string>>();
        Assert.NotNull(handler);

        var notificationHandler = provider.GetService<INotificationHandler<TestNotification>>();
        Assert.NotNull(notificationHandler);
    }

    [Fact]
    public void AddSimpleMediator_SingletonMediator_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(MediatorTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var mediator1 = provider.GetRequiredService<IMediator>();
        var mediator2 = provider.GetRequiredService<IMediator>();

        // Assert
        Assert.Same(mediator1, mediator2);
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

        var handler = provider.GetService<IRequestHandler<TestRequest, string>>();
        Assert.NotNull(handler);

        var notificationHandler = provider.GetService<INotificationHandler<TestNotification>>();
        Assert.NotNull(notificationHandler);
    }
}

// Unhandled request for testing
public record UnhandledRequest() : IRequest<string>;
