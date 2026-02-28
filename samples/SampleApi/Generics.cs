// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ApiContracts;

namespace SampleApi;

/// <summary>
/// A generic repository interface for performing CRUD and query operations on entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity managed by the repository. Must be a reference type.</typeparam>
/// <remarks>
/// Implementations should handle persistence concerns such as connection management
/// and transaction scoping. Consumers should depend on this abstraction rather than
/// concrete data-access implementations.
/// </remarks>
/// <seealso cref="PagedResult{T}"/>
[ApiContract(
    Name = "Repository",
    Description = "Generic repository providing CRUD and query operations for any entity type.",
    Category = "DataAccess",
    Role = "repository",
    Tags = "repository,generic,crud,data-access")]
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The entity if found; otherwise, <see langword="null"/>.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged subset of entities.
    /// </summary>
    /// <param name="page">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the requested page of entities.</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The added entity, including any server-generated values.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the repository by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to remove.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> if the entity was removed; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total number of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The total count of entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><see langword="true"/> if an entity with the given identifier exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a paged subset of a larger collection.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="Page">The one-based page number of this result.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <remarks>
/// Use this type to return paginated results from queries. The <see cref="TotalPages"/>
/// property is computed automatically from <see cref="TotalCount"/> and <see cref="PageSize"/>.
/// </remarks>
/// <seealso cref="IRepository{TEntity}"/>
[ApiContract(
    Name = "PagedResult",
    Description = "A paged result containing a subset of items along with pagination metadata.",
    Category = "DataAccess",
    Role = "response",
    Tags = "pagination,generic,response,collection")]
public record class PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Gets the total number of pages available.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a subsequent page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there is a preceding page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
