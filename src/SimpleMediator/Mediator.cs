using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Abstractions;
using SimpleMediator.Pipeline;

namespace SimpleMediator;

/// <summary>
/// Default implementation of IMediator using dependency injection.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler registered for request type {request.GetType().Name}");
        }

        var handleMethod = handlerType.GetMethod("Handle")!;

        // Get pipeline behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType).Reverse().ToList();

        // Get pre-processors
        var preProcessorType = typeof(IPreProcessor<>).MakeGenericType(request.GetType());
        var preProcessors = _serviceProvider.GetServices(preProcessorType).ToList();

        // Get post-processors
        var postProcessorType = typeof(IPostProcessor<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var postProcessors = _serviceProvider.GetServices(postProcessorType).ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> pipeline = async ct =>
        {
            // Run pre-processors
            foreach (var preProcessor in preProcessors)
            {
                var preProcessMethod = preProcessorType.GetMethod("Process")!;
                await (Task)preProcessMethod.Invoke(preProcessor, new object[] { request, ct })!;
            }

            // Invoke the handler
            var response = await (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, ct })!;

            // Run post-processors
            foreach (var postProcessor in postProcessors)
            {
                var postProcessMethod = postProcessorType.GetMethod("Process")!;
                await (Task)postProcessMethod.Invoke(postProcessor, new object[] { request, response!, ct })!;
            }

            return response!;
        };

        // Wrap with behaviors (outermost first)
        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var behaviorCapture = behavior;
            pipeline = async ct =>
            {
                var handleMethod = behaviorType.GetMethod("Handle")!;
                return await (Task<TResponse>)handleMethod.Invoke(behaviorCapture, new object[] { request, next, ct })!;
            };
        }

        return await pipeline(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TNotification));
        var handlers = _serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            return;
        }

        var tasks = handlers.Select(handler =>
        {
            var handleMethod = handlerType.GetMethod("Handle")!;
            return (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
        });

        await Task.WhenAll(tasks);
    }
}
