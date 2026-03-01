// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Defines the contract for managing customer lifecycle operations including
/// creation, retrieval, search, update, and deactivation.
/// </summary>
/// <remarks>
/// Implementations of <see cref="ICustomerService"/> are responsible for enforcing
/// business rules such as email uniqueness, required-field validation, and the
/// invariant that deactivated customers cannot place new orders.
///
/// All methods are asynchronous and return <see cref="Task{TResult}"/> to support
/// non-blocking I/O against the underlying data store. Callers should handle
/// <see langword="null"/> returns from <see cref="GetByIdAsync(Guid)"/> gracefully,
/// as a missing customer is a normal condition rather than an error.
/// </remarks>
/// <example>
/// Registering a new customer and immediately retrieving it:
/// <code language="csharp">
/// ICustomerService service = GetService();
///
/// var customer = new Customer
/// {
///     Id = Guid.NewGuid(),
///     FullName = "Contoso Ltd.",
///     Email = "info@contoso.com"
/// };
///
/// var created = await service.CreateAsync(customer);
/// var fetched = await service.GetByIdAsync(created.Id);
/// </code>
/// </example>
/// <seealso cref="Customer"/>
/// <seealso cref="Order"/>
public interface ICustomerService
{
    /// <summary>
    /// Persists a new <see cref="Customer"/> and returns the created entity with
    /// any server-generated values (e.g., timestamps) populated.
    /// </summary>
    /// <param name="customer">
    /// The customer to create. The <see cref="Customer.Id"/> and
    /// <see cref="Customer.FullName"/> properties are required.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to the newly created
    /// <see cref="Customer"/> with <see cref="Customer.CreatedAt"/> set by the server.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    /// var customer = new Customer
    /// {
    ///     Id = Guid.NewGuid(),
    ///     FullName = "Alice Smith",
    ///     Email = "alice@example.com",
    ///     Tags = ["premium"]
    /// };
    /// var created = await service.CreateAsync(customer);
    /// Console.WriteLine($"Created {created.FullName} at {created.CreatedAt}");
    /// </code>
    /// </example>
    Task<Customer> CreateAsync(Customer customer);

    /// <summary>
    /// Retrieves a single customer by its globally unique identifier.
    /// </summary>
    /// <param name="id">The <see cref="Guid"/> identifier of the customer to retrieve.</param>
    /// <returns>
    /// The <see cref="Customer"/> if a record with the specified <paramref name="id"/>
    /// exists; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method does not throw when the customer is not found. Callers should
    /// check for a <see langword="null"/> result and return an appropriate HTTP 404
    /// or equivalent response.
    /// </remarks>
    Task<Customer?> GetByIdAsync(Guid id);

    /// <summary>
    /// Searches for customers whose <see cref="Customer.FullName"/> or
    /// <see cref="Customer.Email"/> matches the given query string, with support
    /// for offset-based pagination.
    /// </summary>
    /// <param name="query">
    /// A case-insensitive search string. The implementation performs a contains-style
    /// match against <see cref="Customer.FullName"/> and <see cref="Customer.Email"/>.
    /// </param>
    /// <param name="skip">
    /// The number of matching results to skip, enabling offset-based pagination.
    /// Defaults to 0.
    /// </param>
    /// <param name="take">
    /// The maximum number of results to return per page. Defaults to 25.
    /// Values above 100 may be clamped by the implementation.
    /// </param>
    /// <returns>
    /// A read-only list of customers matching the search criteria, ordered by
    /// <see cref="Customer.FullName"/> ascending. Returns an empty list when no
    /// matches are found.
    /// </returns>
    /// <example>
    /// Paginated search for customers containing "contoso":
    /// <code language="csharp">
    /// var page1 = await service.SearchAsync("contoso", skip: 0, take: 10);
    /// var page2 = await service.SearchAsync("contoso", skip: 10, take: 10);
    /// </code>
    /// </example>
    Task<IReadOnlyList<Customer>> SearchAsync(string query, int skip = 0, int take = 25);

    /// <summary>
    /// Replaces the stored customer record with the values from the supplied
    /// <paramref name="customer"/> object.
    /// </summary>
    /// <param name="customer">
    /// The customer entity containing the updated values. The <see cref="Customer.Id"/>
    /// must match an existing record.
    /// </param>
    /// <returns>
    /// The updated <see cref="Customer"/> as persisted, which may include
    /// server-modified fields such as an updated timestamp.
    /// </returns>
    Task<Customer> UpdateAsync(Customer customer);

    /// <summary>
    /// Marks a customer as inactive, preventing them from placing new orders
    /// or appearing in search results.
    /// </summary>
    /// <param name="id">The identifier of the customer to deactivate.</param>
    /// <returns>
    /// <see langword="true"/> if the customer was found and successfully deactivated;
    /// <see langword="false"/> if no customer with the given <paramref name="id"/> exists
    /// or the customer was already inactive.
    /// </returns>
    /// <remarks>
    /// Deactivation is a soft operation\u2014the customer record is retained in the
    /// data store with <see cref="Customer.IsActive"/> set to <see langword="false"/>.
    /// This method is idempotent: calling it on an already-inactive customer
    /// returns <see langword="false"/> without side effects.
    /// </remarks>
    Task<bool> DeactivateAsync(Guid id);
}
