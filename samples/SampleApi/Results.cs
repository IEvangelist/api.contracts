// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Encapsulates the outcome of a domain operation as either a success carrying a
/// value of type <typeparamref name="T"/>, or a failure carrying an error message.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
/// <remarks>
/// <see cref="Result{T}"/> implements the discriminated-result pattern:
/// callers inspect <see cref="IsSuccess"/> (or <see cref="IsFailure"/>) before
/// accessing <see cref="Value"/> or <see cref="Error"/>. Accessing <see cref="Value"/>
/// on a failed result returns <see langword="default"/> for <typeparamref name="T"/>.
///
/// Prefer the static factory methods <see cref="Ok"/> and <see cref="Fail"/> over
/// direct construction to ensure the success/error invariants are satisfied.
/// This type can be used as a method return value in service layers to avoid
/// throwing exceptions for expected failure conditions.
/// </remarks>
/// <example>
/// Using Result to handle success and failure paths:
/// <code language="csharp">
/// Result&lt;Customer&gt; result = await customerService.TryFindAsync(id);
///
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Found: {result.Value!.FullName}");
/// }
/// else
/// {
///     Console.WriteLine($"Error: {result.Error}");
/// }
/// </code>
/// </example>
/// <example>
/// Returning a Result from a service method:
/// <code language="csharp">
/// public Result&lt;Customer&gt; Validate(Customer customer)
/// {
///     if (string.IsNullOrWhiteSpace(customer.FullName))
///         return Result&lt;Customer&gt;.Fail("Full name is required.");
///
///     return Result&lt;Customer&gt;.Ok(customer);
/// }
/// </code>
/// </example>
/// <seealso cref="ValidationError"/>
/// <seealso cref="ProblemDetails"/>
public class Result<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// Use the static factory methods instead of calling this constructor directly.
    /// </summary>
    protected Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value produced by a successful operation.
    /// </summary>
    /// <value>
    /// The result value, or the default of <typeparamref name="T"/> when the operation failed.
    /// </value>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message describing why the operation failed.
    /// </summary>
    /// <value>
    /// The error message, or <see langword="null"/> when the operation succeeded.
    /// </value>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result containing the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A <see cref="Result{T}"/> representing a successful outcome.</returns>
    public static Result<T> Ok(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">A message describing the failure.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed outcome.</returns>
    public static Result<T> Fail(string error) => new(false, default, error);
}

/// <summary>
/// Describes a single validation error tied to a specific input field,
/// including a human-readable message and an optional machine-readable error code.
/// </summary>
/// <param name="Field">
/// The name of the input field that failed validation, using the same casing
/// as the JSON property name (e.g., <c>"email"</c>, <c>"fullName"</c>).
/// </param>
/// <param name="Message">
/// A human-readable, locale-sensitive message describing why the field value
/// is invalid (e.g., <c>"Email address is not in a valid format."</c>).
/// </param>
/// <param name="Code">
/// An application-specific error code for programmatic handling, such as
/// <c>"INVALID_FORMAT"</c> or <c>"FIELD_REQUIRED"</c>. When <see langword="null"/>,
/// the client should rely on the <paramref name="Message"/> for context.
/// </param>
/// <remarks>
/// Validation errors are typically collected during request processing and
/// surfaced to the client as an array within a <see cref="ProblemDetails"/>
/// response. Use consistent error codes across the API to allow clients to
/// build locale-aware error displays.
/// </remarks>
/// <example>
/// Creating a list of validation errors:
/// <code language="csharp">
/// var errors = new List&lt;ValidationError&gt;
/// {
///     new("email", "Email address is required.", "FIELD_REQUIRED"),
///     new("fullName", "Full name must be between 1 and 200 characters.", "INVALID_LENGTH")
/// };
/// </code>
/// </example>
/// <seealso cref="ProblemDetails"/>
/// <seealso cref="Result{T}"/>
public record class ValidationError(
    string Field,
    string Message,
    string? Code = null);

/// <summary>
/// A machine-readable error response body following the RFC 9457 (Problem Details
/// for HTTP APIs) specification, providing a standardized structure that clients
/// can parse without knowledge of the specific API.
/// </summary>
/// <remarks>
/// Every field in <see cref="ProblemDetails"/> is optional. At minimum, set
/// <see cref="Status"/> and <see cref="Title"/>. Use <see cref="Type"/> to point
/// to a documentation page describing the error category. The
/// <see cref="Extensions"/> dictionary allows attaching arbitrary additional
/// context—common patterns include:
/// <list type="bullet">
///   <item><description>A <c>"traceId"</c> for correlating with server logs.</description></item>
///   <item><description>A <c>"errors"</c> array of <see cref="ValidationError"/> for 400 responses.</description></item>
///   <item><description>A <c>"retryAfter"</c> value for 429 / 503 responses.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Building a 404 problem details response:
/// <code language="csharp">
/// var problem = new ProblemDetails
/// {
///     Type = "https://api.example.com/errors/not-found",
///     Title = "Customer Not Found",
///     Status = 404,
///     Detail = $"No customer with ID '{id}' exists.",
///     Instance = $"/api/v1/customers/{id}"
/// };
/// problem.Extensions["traceId"] = Activity.Current?.Id;
/// </code>
/// </example>
/// <example>
/// Building a 400 validation problem details:
/// <code language="csharp">
/// var problem = new ProblemDetails
/// {
///     Type = "https://api.example.com/errors/validation",
///     Title = "Validation Failed",
///     Status = 400,
///     Detail = "One or more fields failed validation."
/// };
/// problem.Extensions["errors"] = new[]
/// {
///     new ValidationError("email", "Invalid email format.", "INVALID_FORMAT")
/// };
/// </code>
/// </example>
/// <seealso cref="ValidationError"/>
/// <seealso cref="Result{T}"/>
public class ProblemDetails
{
    /// <summary>
    /// Gets or sets a URI reference that identifies the problem type.
    /// </summary>
    /// <value>A URI such as "https://example.com/errors/not-found".</value>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a short, human-readable summary of the problem type.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code generated by the origin server for this occurrence.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Gets or sets a human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets a URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Gets the extension members for this problem details instance.
    /// </summary>
    /// <remarks>
    /// Use this dictionary to include additional context such as trace identifiers,
    /// validation errors, or retry-after hints.
    /// </remarks>
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();
}
