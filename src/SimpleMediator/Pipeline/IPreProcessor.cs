namespace SimpleMediator.Pipeline;

/// <summary>
/// Pre-processor that runs before the request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
public interface IPreProcessor<in TRequest>
{
    /// <summary>
    /// Processes the request before the handler executes.
    /// </summary>
    Task Process(TRequest request, CancellationToken cancellationToken);
}
