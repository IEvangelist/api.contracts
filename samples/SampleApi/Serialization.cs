// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using ApiContracts;

namespace SampleApi;

/// <summary>
/// A data transfer object for product information, annotated with
/// <c>System.Text.Json</c> serialization attributes.
/// </summary>
/// <remarks>
/// This DTO demonstrates common JSON customisation patterns:
/// <list type="bullet">
///   <item><description><see cref="JsonPropertyNameAttribute"/> to control the serialised property names.</description></item>
///   <item><description><see cref="JsonIgnoreAttribute"/> to exclude properties from serialisation.</description></item>
///   <item><description><see cref="JsonRequiredAttribute"/> to enforce presence during deserialisation.</description></item>
///   <item><description><see cref="JsonConverterAttribute"/> to apply a custom converter.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ApiResponse{T}"/>
[ApiContract(
    Name = "ProductDto",
    Description = "Product data transfer object with System.Text.Json serialization attributes.",
    Category = "DTOs",
    Role = "dto",
    Tags = "product,dto,serialization,json")]
public class ProductDto
{
    /// <summary>
    /// Gets or sets the unique product identifier.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonRequired]
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the stock keeping unit code.
    /// </summary>
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the product tags for search and filtering.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the product is currently available for purchase.
    /// </summary>
    [JsonPropertyName("available")]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// An internal tracking code that should not be exposed through the API.
    /// </summary>
    [JsonIgnore]
    public string? InternalTrackingCode { get; set; }

    /// <summary>
    /// Gets or sets the date the product was added to the catalogue.
    /// </summary>
    [JsonPropertyName("added_at")]
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A generic envelope for API responses containing a data payload and metadata.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
/// <remarks>
/// Wrapping all API responses in <see cref="ApiResponse{T}"/> gives clients a
/// consistent structure to deserialise, regardless of the underlying resource type.
/// </remarks>
/// <seealso cref="ProductDto"/>
/// <seealso cref="ProblemDetails"/>
[ApiContract(
    Name = "ApiResponse",
    Description = "A generic API response wrapper containing data, a message, and a timestamp.",
    Category = "DTOs",
    Role = "response",
    Tags = "response,generic,dto,serialization,envelope")]
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets the response data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the response was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
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
