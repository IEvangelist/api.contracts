// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ApiContracts;

namespace SampleApi;

/// <summary>
/// Provides well-known route path constants for the sample API.
/// </summary>
/// <remarks>
/// Centralising route paths in a static class ensures that controllers, clients,
/// and tests reference the same values, reducing the risk of routing mismatches.
/// </remarks>
[ApiContract(
    Name = "ApiEndpoints",
    Description = "Static class containing route path constants for every API endpoint.",
    Category = "Configuration",
    Role = "configuration",
    Tags = "routes,constants,configuration,static")]
public static class ApiEndpoints
{
    /// <summary>
    /// Base path for customer endpoints.
    /// </summary>
    public const string Customers = "/api/v1/customers";

    /// <summary>
    /// Route template for a single customer resource.
    /// </summary>
    public const string CustomerById = "/api/v1/customers/{id}";

    /// <summary>
    /// Base path for order endpoints.
    /// </summary>
    public const string Orders = "/api/v1/orders";

    /// <summary>
    /// Route template for a single order resource.
    /// </summary>
    public const string OrderById = "/api/v1/orders/{id}";

    /// <summary>
    /// Path for the product catalogue.
    /// </summary>
    public const string Products = "/api/v1/products";

    /// <summary>
    /// Route template for a single product resource.
    /// </summary>
    public const string ProductById = "/api/v1/products/{id}";

    /// <summary>
    /// Path for the health-check endpoint.
    /// </summary>
    public const string Health = "/health";

    /// <summary>
    /// Path for the readiness probe endpoint.
    /// </summary>
    public const string Ready = "/ready";
}

/// <summary>
/// Configuration options for API rate limiting.
/// </summary>
/// <remarks>
/// Bind an instance of <see cref="RateLimitOptions"/> from the application's configuration
/// section (e.g., "RateLimiting") to control how many requests a client may make
/// within a sliding time window.
/// </remarks>
[ApiContract(
    Name = "RateLimitOptions",
    Description = "Options that control the API rate-limiting behaviour.",
    Category = "Configuration",
    Role = "configuration",
    Tags = "rate-limit,configuration,options,throttling")]
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    /// <value>Defaults to 100 requests.</value>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets the duration of the sliding window in seconds.
    /// </summary>
    /// <value>Defaults to 60 seconds.</value>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the number of seconds a client should wait before retrying
    /// after being rate-limited.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public int RetryAfterSeconds { get; set; } = 30;
}

/// <summary>
/// Feature flags that can be independently enabled for the application.
/// </summary>
/// <remarks>
/// This enum uses the <see cref="FlagsAttribute"/> to allow bitwise combination of values.
/// Use <see cref="AllFeatures"/> as a convenience mask to enable every flag at once.
/// </remarks>
[Flags]
[ApiContract(
    Name = "FeatureFlags",
    Description = "Bitwise flags representing independently toggleable application features.",
    Category = "Configuration",
    Role = "enum",
    Tags = "feature-flags,configuration,enum,flags")]
public enum FeatureFlags
{
    /// <summary>No features enabled.</summary>
    None = 0,

    /// <summary>Enables access to beta features.</summary>
    BetaFeatures = 1,

    /// <summary>Enables the dark mode user interface.</summary>
    DarkMode = 2,

    /// <summary>Enables analytics and telemetry collection.</summary>
    Analytics = 4,

    /// <summary>Enables push notifications.</summary>
    Notifications = 8,

    /// <summary>Convenience flag that enables all features.</summary>
    AllFeatures = BetaFeatures | DarkMode | Analytics | Notifications,
}
