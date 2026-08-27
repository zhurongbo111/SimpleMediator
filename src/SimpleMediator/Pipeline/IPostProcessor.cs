namespace SimpleMediator.Pipeline;

/// <summary>
/// Post-processor that runs after the request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public interface IPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Processes the response after the handler executes.
    /// </summary>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
