namespace DeepCode.SimpleMediator.Abstractions;

/// <summary>
/// Marker interface for a request that returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<out TResponse> { }
