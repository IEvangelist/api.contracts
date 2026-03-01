// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ApiContracts;

namespace SampleApi;

/// <summary>
/// Provides data for order-related events.
/// </summary>
/// <remarks>
/// This class extends <see cref="EventArgs"/> to carry the <see cref="Order"/>
/// that triggered the event along with a UTC timestamp indicating when the event occurred.
/// </remarks>
/// <seealso cref="IOrderNotificationService"/>
/// <seealso cref="Order"/>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderEventArgs"/> class.
/// </remarks>
/// <param name="order">The order associated with the event.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="order"/> is <see langword="null"/>.</exception>
[ApiContract(
    Name = "OrderEventArgs",
    Description = "Event data carrying an order and the timestamp of the event.",
    Category = "Events",
    Role = "event-args",
    Tags = "event,order,notification")]
public class OrderEventArgs(Order order) : EventArgs
{

    /// <summary>
    /// Gets the order associated with this event.
    /// </summary>
    public Order Order { get; } = order ?? throw new ArgumentNullException(nameof(order));

    /// <summary>
    /// Gets the UTC timestamp indicating when the event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A service that publishes notifications for key order lifecycle events.
/// </summary>
/// <remarks>
/// Consumers subscribe to the events exposed by this interface to react to
/// order state transitions. Implementations are responsible for raising events
/// at the appropriate points in the order processing pipeline.
/// </remarks>
/// <seealso cref="OrderEventArgs"/>
/// <seealso cref="Order"/>
[ApiContract(
    Name = "OrderNotificationService",
    Description = "Publishes events for order lifecycle transitions such as placement, shipment, and cancellation.",
    Category = "Services",
    Role = "service",
    Tags = "event,order,notification,service")]
public interface IOrderNotificationService
{
    /// <summary>
    /// Occurs when a new order has been placed.
    /// </summary>
    event EventHandler<OrderEventArgs> OrderPlaced;

    /// <summary>
    /// Occurs when an order has been shipped.
    /// </summary>
    event EventHandler<OrderEventArgs> OrderShipped;

    /// <summary>
    /// Occurs when an order has been cancelled.
    /// </summary>
    event EventHandler<OrderEventArgs> OrderCancelled;

    /// <summary>
    /// Occurs when an order has been delivered.
    /// </summary>
    event EventHandler<OrderEventArgs> OrderDelivered;

    /// <summary>
    /// Publishes a notification that the specified order was placed.
    /// </summary>
    /// <param name="order">The order that was placed.</param>
    void NotifyOrderPlaced(Order order);

    /// <summary>
    /// Publishes a notification that the specified order was shipped.
    /// </summary>
    /// <param name="order">The order that was shipped.</param>
    void NotifyOrderShipped(Order order);

    /// <summary>
    /// Publishes a notification that the specified order was cancelled.
    /// </summary>
    /// <param name="order">The order that was cancelled.</param>
    void NotifyOrderCancelled(Order order);
}
