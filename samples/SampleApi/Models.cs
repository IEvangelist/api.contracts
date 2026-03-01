// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Represents a customer in the system.
/// </summary>
/// <remarks>
/// This is a sample domain model demonstrating the API Contracts generator.
/// It shows how types, properties, and methods are captured in the schema.
/// </remarks>
public class Customer
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer's full name.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the date when the customer was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets whether the customer is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the customer's tags for segmentation.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the customer's preferred contact method.
    /// </summary>
    public ContactMethod PreferredContact { get; set; } = ContactMethod.Email;
}

/// <summary>
/// Available contact methods for a customer.
/// </summary>
public enum ContactMethod
{
    /// <summary>Contact via email.</summary>
    Email = 0,

    /// <summary>Contact via phone.</summary>
    Phone = 1,

    /// <summary>Contact via SMS.</summary>
    Sms = 2,

    /// <summary>Contact via postal mail.</summary>
    Mail = 3,
}

/// <summary>
/// Represents an order placed by a customer.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer who placed this order.
    /// </summary>
    public required Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the line items in the order.
    /// </summary>
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// Gets the total price of the order.
    /// </summary>
    public decimal TotalPrice => Items.Sum(i => i.Quantity * i.UnitPrice);

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

/// <summary>
/// Represents a single item within an order.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public required string ProductName { get; set; }

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// The status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order is pending processing.</summary>
    Pending = 0,

    /// <summary>Order has been confirmed.</summary>
    Confirmed = 1,

    /// <summary>Order has been shipped.</summary>
    Shipped = 2,

    /// <summary>Order has been delivered.</summary>
    Delivered = 3,

    /// <summary>Order has been cancelled.</summary>
    Cancelled = 4,
}
