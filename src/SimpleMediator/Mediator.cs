using System.Collections.Concurrent;
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

    // Cache for handler types: (requestType, responseType) -> handlerType
    private static readonly ConcurrentDictionary<Type, Type> _handlerTypeCache = new();

    // Cache for notification handler types: notificationType -> handlerType
    private static readonly ConcurrentDictionary<Type, Type> _notificationHandlerTypeCache = new();

    // Cache for Handle method: handlerType -> MethodInfo
    private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethodCache = new();

    // Cache for Order property: type -> PropertyInfo
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _orderPropertyCache = new();

    // Cache for pipeline behavior types: (requestType, responseType) -> behaviorType
    private static readonly ConcurrentDictionary<Type, Type> _behaviorTypeCache = new();

    // Cache for pre-processor types: requestType -> preProcessorType
    private static readonly ConcurrentDictionary<Type, Type> _preProcessorTypeCache = new();

    // Cache for post-processor types: (requestType, responseType) -> postProcessorType
    private static readonly ConcurrentDictionary<Type, Type> _postProcessorTypeCache = new();

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
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        // Get or create handler type
        var handlerType = _handlerTypeCache.GetOrAdd(requestType,
            rt => typeof(IRequestHandler<,>).MakeGenericType(rt, responseType));

        var handler = _serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler registered for request type {requestType.Name}");
        }

        // Get or create Handle method
        var handleMethod = _handleMethodCache.GetOrAdd(handlerType,
            ht => ht.GetMethod("Handle")!);

        // Get pipeline behavior type
        var behaviorType = _behaviorTypeCache.GetOrAdd(requestType,
            rt => typeof(IPipelineBehavior<,>).MakeGenericType(rt, responseType));

        var behaviors = _serviceProvider.GetServices(behaviorType)
            .OrderBy(b => GetOrder(b, behaviorType))
            .ToList();

        // Get pre-processor type
        var preProcessorType = _preProcessorTypeCache.GetOrAdd(requestType,
            rt => typeof(IPreProcessor<>).MakeGenericType(rt));

        var preProcessors = _serviceProvider.GetServices(preProcessorType)
            .OrderBy(p => GetOrder(p, preProcessorType))
            .ToList();

        // Get post-processor type
        var postProcessorType = _postProcessorTypeCache.GetOrAdd(requestType,
            rt => typeof(IPostProcessor<,>).MakeGenericType(rt, responseType));

        var postProcessors = _serviceProvider.GetServices(postProcessorType)
            .OrderBy(p => GetOrder(p, postProcessorType))
            .ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> pipeline = async ct =>
        {
            // Run pre-processors
            foreach (var preProcessor in preProcessors)
            {
                var preProcessMethod = _handleMethodCache.GetOrAdd(preProcessorType,
                    pt => pt.GetMethod("Process")!);
                await (Task)preProcessMethod.Invoke(preProcessor, new object[] { request, ct })!;
            }

            // Invoke the handler
            var response = await (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, ct })!;

            // Run post-processors
            foreach (var postProcessor in postProcessors)
            {
                var postProcessMethod = _handleMethodCache.GetOrAdd(postProcessorType,
                    pt => pt.GetMethod("Process")!);
                await (Task)postProcessMethod.Invoke(postProcessor, new object[] { request, response!, ct })!;
            }

            return response!;
        };

        // Wrap with behaviors (outermost first, so we reverse the sorted list)
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = pipeline;
            var behaviorCapture = behavior;
            pipeline = async ct =>
            {
                var behaviorHandleMethod = _handleMethodCache.GetOrAdd(behaviorType,
                    bt => bt.GetMethod("Handle")!);
                return await (Task<TResponse>)behaviorHandleMethod.Invoke(behaviorCapture, new object[] { request, next, ct })!;
            };
        }

        return await pipeline(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var notificationType = typeof(TNotification);

        // Get or create notification handler type
        var handlerType = _notificationHandlerTypeCache.GetOrAdd(notificationType,
            nt => typeof(INotificationHandler<>).MakeGenericType(nt));

        var handlers = _serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            return;
        }

        // Get or create Handle method
        var handleMethod = _handleMethodCache.GetOrAdd(handlerType,
            ht => ht.GetMethod("Handle")!);

        var tasks = handlers.Select(handler =>
        {
            return (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
        });

        await Task.WhenAll(tasks);
    }

    private static int GetOrder(object? instance, Type interfaceType)
    {
        if (instance is null)
        {
            return 0;
        }

        var property = _orderPropertyCache.GetOrAdd(interfaceType,
            t => t.GetProperty("Order"));

        if (property is null)
        {
            return 0;
        }

        return (int)(property.GetValue(instance) ?? 0);
    }
}
