// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Service for managing customers.
/// </summary>
/// <remarks>
/// Provides CRUD operations and search capabilities for customer entities.
/// </remarks>
public interface ICustomerService
{
    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="customer">The customer to create.</param>
    /// <returns>The created customer with assigned identifier.</returns>
    Task<Customer> CreateAsync(Customer customer);

    /// <summary>
    /// Retrieves a customer by identifier.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <returns>The customer if found; otherwise, <see langword="null"/>.</returns>
    Task<Customer?> GetByIdAsync(Guid id);

    /// <summary>
    /// Searches for customers matching the given criteria.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="skip">Number of results to skip for pagination.</param>
    /// <param name="take">Number of results to return.</param>
    /// <returns>A list of matching customers.</returns>
    Task<IReadOnlyList<Customer>> SearchAsync(string query, int skip = 0, int take = 25);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="customer">The customer with updated values.</param>
    /// <returns>The updated customer.</returns>
    Task<Customer> UpdateAsync(Customer customer);

    /// <summary>
    /// Deactivates a customer by identifier.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <returns><see langword="true"/> if the customer was deactivated.</returns>
    Task<bool> DeactivateAsync(Guid id);
}
