namespace DeepCode.SimpleMediator.Pipeline;

/// <summary>
/// Delegate that represents the next step in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The response from the next step in the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
