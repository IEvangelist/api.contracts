// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Defines a generic repository contract for performing CRUD and query
/// operations against a persistent store of <typeparamref name="TEntity"/> objects.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type managed by this repository. Must be a reference type so that
/// <see langword="null"/> can be returned from <see cref="GetByIdAsync"/> when an
/// entity is not found.
/// </typeparam>
/// <remarks>
/// <see cref="IRepository{TEntity}"/> abstracts the data-access layer, allowing
/// service classes to operate against an in-memory fake, an EF Core DbContext,
/// or a Dapper-based implementation without code changes.
///
/// Implementations are responsible for:
/// <list type="bullet">
///   <item><description>Opening and closing database connections or scoping units of work.</description></item>
///   <item><description>Mapping between CLR entities and the underlying storage representation.</description></item>
///   <item><description>Propagating <see cref="CancellationToken"/> to all I/O calls.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Using the repository to page through all customers:
/// <code language="csharp">
/// IRepository&lt;Customer&gt; repo = GetRepository();
///
/// var page = await repo.GetPagedAsync(page: 1, pageSize: 20);
/// Console.WriteLine($"Showing {page.Items.Count} of {page.TotalCount}");
///
/// while (page.HasNextPage)
/// {
///     page = await repo.GetPagedAsync(page.Page + 1, pageSize: 20);
/// }
/// </code>
/// </example>
/// <seealso cref="PagedResult{T}"/>
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
/// Represents a single page of results from a paginated query, including
/// the items on the current page and metadata for navigating the full result set.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The read-only list of items on the current page.</param>
/// <param name="TotalCount">The total number of items across all pages in the underlying query.</param>
/// <param name="Page">The one-based page number represented by this result.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <remarks>
/// <see cref="PagedResult{T}"/> is returned by <see cref="IRepository{TEntity}.GetPagedAsync"/>
/// and other paged query methods. The <see cref="TotalPages"/>, <see cref="HasNextPage"/>,
/// and <see cref="HasPreviousPage"/> properties are computed automatically to simplify
/// building pagination controls in UIs and hypermedia links in API responses.
///
/// When <see cref="PageSize"/> is zero, <see cref="TotalPages"/> returns zero to
/// avoid division-by-zero errors.
/// </remarks>
/// <example>
/// Building pagination links from a paged result:
/// <code language="csharp">
/// var page = await repo.GetPagedAsync(page: 2, pageSize: 10);
///
/// Console.WriteLine($"Page {page.Page} of {page.TotalPages}");
/// if (page.HasPreviousPage) Console.WriteLine("  ← Previous");
/// if (page.HasNextPage) Console.WriteLine("  Next →");
/// </code>
/// </example>
/// <seealso cref="IRepository{TEntity}"/>
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
