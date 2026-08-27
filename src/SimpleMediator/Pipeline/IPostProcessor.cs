namespace DeepCode.SimpleMediator.Pipeline;

/// <summary>
/// Post-processor that runs after the request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public interface IPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// The execution order of this post-processor. Lower values execute first.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Processes the response after the handler executes.
    /// </summary>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
