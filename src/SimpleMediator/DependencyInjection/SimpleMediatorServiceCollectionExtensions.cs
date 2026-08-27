using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DeepCode.SimpleMediator.Abstractions;
using DeepCode.SimpleMediator.Pipeline;

namespace DeepCode.SimpleMediator.DependencyInjection;

/// <summary>
/// Extension methods for registering SimpleMediator services.
/// </summary>
public static class SimpleMediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator services to the specified IServiceCollection.
    /// Scans the calling assembly for handlers and registers them.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services)
    {
        return services.AddSimpleMediator(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Adds SimpleMediator services to the specified IServiceCollection.
    /// Scans the specified assembly for handlers and registers them.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, Assembly assembly)
    {
        return services.AddSimpleMediator(new[] { assembly });
    }

    /// <summary>
    /// Adds SimpleMediator services to the specified IServiceCollection.
    /// Scans the specified assemblies for handlers and registers them.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddSimpleMediator(assemblies.AsEnumerable());
    }

    /// <summary>
    /// Adds SimpleMediator services to the specified IServiceCollection.
    /// Scans the specified assemblies for handlers and registers them.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        // Register IMediator as transient (uses current scope's IServiceProvider)
        services.TryAddTransient<IMediator, Mediator>();

        var assembliesArray = assemblies.ToArray();

        // Register request handlers
        var handlerTypes = assembliesArray
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            services.AddTransient(interfaceType, handlerType);
        }

        // Register notification handlers
        var notificationHandlerTypes = assembliesArray
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)));

        foreach (var handlerType in notificationHandlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

            services.AddTransient(interfaceType, handlerType);
        }

        // Register pipeline behaviors
        var behaviorTypes = assembliesArray
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)));

        foreach (var behaviorType in behaviorTypes)
        {
            var interfaceType = behaviorType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            services.AddTransient(interfaceType, behaviorType);
        }

        // Register pre-processors
        var preProcessorTypes = assembliesArray
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPreProcessor<>)));

        foreach (var preProcessorType in preProcessorTypes)
        {
            var interfaceType = preProcessorType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPreProcessor<>));

            services.AddTransient(interfaceType, preProcessorType);
        }

        // Register post-processors
        var postProcessorTypes = assembliesArray
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPostProcessor<,>)));

        foreach (var postProcessorType in postProcessorTypes)
        {
            var interfaceType = postProcessorType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPostProcessor<,>));

            services.AddTransient(interfaceType, postProcessorType);
        }

        return services;
    }
}
