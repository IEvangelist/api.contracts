// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
namespace SampleApi;

/// <summary>
/// A data transfer object for product information, annotated with
/// <c>System.Text.Json</c> serialization attributes to control wire format.
/// </summary>
/// <remarks>
/// <see cref="ProductDto"/> demonstrates the most common JSON customization patterns
/// supported by <c>System.Text.Json</c>:
/// <list type="bullet">
///   <item><description><see cref="JsonPropertyNameAttribute"/> to map CLR property names to snake_case or camelCase JSON keys.</description></item>
///   <item><description><see cref="JsonIgnoreAttribute"/> to hide internal-only properties from serialized output.</description></item>
///   <item><description><see cref="JsonRequiredAttribute"/> to enforce that a property must be present during deserialization.</description></item>
/// </list>
///
/// Properties marked with <see cref="JsonRequiredAttribute"/> will cause
/// <see cref="System.Text.Json.JsonSerializer"/> to throw a
/// <see cref="System.Text.Json.JsonException"/> if the corresponding key is missing
/// from the input JSON.
/// </remarks>
/// <example>
/// Serializing a product to JSON:
/// <code language="csharp">
/// var product = new ProductDto
/// {
///     Id = Guid.NewGuid(),
///     Name = "Wireless Mouse",
///     Description = "Ergonomic wireless mouse with USB-C receiver",
///     Price = 29.99m,
///     Sku = "WM-1001",
///     Category = "Peripherals",
///     Tags = ["wireless", "ergonomic", "usb-c"],
///     IsAvailable = true
/// };
///
/// string json = JsonSerializer.Serialize(product);
/// // {"id":"...","name":"Wireless Mouse","description":"...","price":29.99,...}
/// </code>
/// </example>
/// <example>
/// Deserializing from JSON (required properties enforced):
/// <code language="csharp">
/// string json = """
///     {
///         "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///         "name": "Keyboard",
///         "price": 59.99
///     }
///     """;
/// var product = JsonSerializer.Deserialize&lt;ProductDto&gt;(json);
/// </code>
/// </example>
/// <seealso cref="ApiResponse{T}"/>
public class ProductDto
{
    /// <summary>
    /// Gets or sets the unique product identifier. This is required during
    /// deserialization and maps to the JSON key <c>"id"</c>.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonRequired]
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable product name displayed in catalogues
    /// and search results. Maps to the JSON key <c>"name"</c>.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the long-form product description, supporting plain text
    /// only. Maps to the JSON key <c>"description"</c>.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the product has no description and UIs
    /// should display a placeholder or omit the section entirely.
    /// </remarks>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unit price in the store's base currency.
    /// Maps to the JSON key <c>"price"</c>.
    /// </summary>
    /// <remarks>
    /// Prices are represented as <see cref="decimal"/> to preserve precision
    /// in financial calculations. Negative values are not permitted.
    /// </remarks>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the stock keeping unit code for inventory tracking.
    /// Maps to the JSON key <c>"sku"</c>.
    /// </summary>
    /// <remarks>
    /// SKU codes follow the format <c>XX-####</c> where XX is a two-letter
    /// category prefix and #### is a numeric sequence.
    /// </remarks>
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the product category used for catalogue organization.
    /// Maps to the JSON key <c>"category"</c>.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the list of searchable tags associated with this product.
    /// Maps to the JSON key <c>"tags"</c>.
    /// </summary>
    /// <remarks>
    /// Tags enable faceted search and filtering. They should be lowercase,
    /// hyphen-separated strings (e.g., "usb-c", "wireless").
    /// </remarks>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the product is currently available for purchase.
    /// Maps to the JSON key <c>"available"</c>.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, the product should not appear in storefront
    /// listings but may still be visible in the admin catalogue.
    /// </remarks>
    [JsonPropertyName("available")]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// An internal tracking code used by the warehouse management system.
    /// This property is excluded from JSON serialization via
    /// <see cref="JsonIgnoreAttribute"/> and is never exposed through the API.
    /// </summary>
    [JsonIgnore]
    public string? InternalTrackingCode { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the product was first added to
    /// the catalogue. Maps to the JSON key <c>"added_at"</c>.
    /// </summary>
    [JsonPropertyName("added_at")]
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A generic envelope for API responses, providing a consistent wire format that
/// wraps the data payload alongside success/failure status and metadata.
/// </summary>
/// <typeparam name="T">The type of the data payload contained in the response.</typeparam>
/// <remarks>
/// All API endpoints should return <see cref="ApiResponse{T}"/> to give clients
/// a uniform structure for deserialization. The <see cref="Success"/> flag allows
/// clients to branch on success or failure without inspecting HTTP status codes,
/// while <see cref="Timestamp"/> provides server-side timing for debugging and
/// cache-invalidation purposes.
///
/// For error responses, <see cref="Data"/> will be <see langword="null"/> and
/// <see cref="Message"/> will contain a human-readable error description.
/// For richer error bodies, consider returning <see cref="ProblemDetails"/>
/// as the <typeparamref name="T"/> payload.
/// </remarks>
/// <example>
/// Returning a successful product response:
/// <code language="csharp">
/// var product = new ProductDto { Id = Guid.NewGuid(), Name = "Widget" };
/// var response = ApiResponse&lt;ProductDto&gt;.Ok(product, "Product retrieved");
/// // { "data": { ... }, "message": "Product retrieved", "success": true, ... }
/// </code>
/// </example>
/// <example>
/// Returning an error response:
/// <code language="csharp">
/// var response = ApiResponse&lt;ProductDto&gt;.Fail("Product not found");
/// // { "data": null, "message": "Product not found", "success": false, ... }
/// </code>
/// </example>
/// <seealso cref="ProductDto"/>
/// <seealso cref="ProblemDetails"/>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets the response data payload.
    /// </summary>
    /// <remarks>
    /// Contains the requested resource on success, or <see langword="null"/> on failure.
    /// </remarks>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable message providing additional context
    /// about the response, such as a confirmation or an error description.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when the server generated
    /// this response. Useful for debugging latency and cache invalidation.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the request completed successfully.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, <see cref="Message"/> contains the error
    /// description and <see cref="Data"/> is <see langword="null"/>.
    /// </remarks>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// Creates a successful response containing the specified data.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <param name="message">An optional message.</param>
    /// <returns>A new <see cref="ApiResponse{T}"/> marked as successful.</returns>
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Data = data, Message = message, Success = true };

    /// <summary>
    /// Creates an error response with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="ApiResponse{T}"/> marked as unsuccessful.</returns>
    public static ApiResponse<T> Fail(string message) =>
        new() { Message = message, Success = false };
}
