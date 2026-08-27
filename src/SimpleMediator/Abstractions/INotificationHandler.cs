namespace SimpleMediator.Abstractions;

/// <summary>
/// Handles a notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
