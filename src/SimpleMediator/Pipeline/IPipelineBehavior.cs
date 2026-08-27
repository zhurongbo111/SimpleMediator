using SimpleMediator.Abstractions;

namespace SimpleMediator.Pipeline;

/// <summary>
/// Pipeline behavior that wraps request handling.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// The execution order of this behavior. Lower values execute first.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Handles the request and invokes the next behavior or handler in the pipeline.
    /// </summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
