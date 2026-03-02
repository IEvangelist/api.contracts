namespace EmissionTarget;

/// <summary>
/// A customer entity for testing schema generation.
/// </summary>
public class Customer
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Full name of the customer.</summary>
    public string Name { get; set; } = "";

    /// <summary>Customer email address.</summary>
    public string? Email { get; set; }

    /// <summary>Whether the customer is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents an order placed by a customer.
/// </summary>
public class Order
{
    /// <summary>Order identifier.</summary>
    public int Id { get; set; }

    /// <summary>Customer who placed the order.</summary>
    public int CustomerId { get; set; }

    /// <summary>Total amount.</summary>
    public decimal Total { get; set; }

    /// <summary>Current status.</summary>
    public OrderStatus Status { get; set; }
}

/// <summary>
/// Status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order is pending.</summary>
    Pending = 0,

    /// <summary>Order is confirmed.</summary>
    Confirmed = 1,

    /// <summary>Order has been shipped.</summary>
    Shipped = 2,

    /// <summary>Order was delivered.</summary>
    Delivered = 3,

    /// <summary>Order was cancelled.</summary>
    Cancelled = 4
}

/// <summary>
/// Service contract for customer operations.
/// </summary>
public interface ICustomerService
{
    /// <summary>Gets a customer by ID.</summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer if found.</returns>
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new customer.</summary>
    /// <param name="customer">The customer to create.</param>
    /// <returns>The created customer with assigned ID.</returns>
    Task<Customer> CreateAsync(Customer customer);

    /// <summary>Searches for customers by name.</summary>
    /// <param name="query">The search query.</param>
    /// <returns>Matching customers.</returns>
    Task<IReadOnlyList<Customer>> SearchAsync(string query);
}

/// <summary>
/// Generic repository for data access.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Gets an entity by ID.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Gets all entities.</summary>
    Task<IReadOnlyList<T>> GetAllAsync();

    /// <summary>Adds a new entity.</summary>
    Task AddAsync(T entity);

    /// <summary>Deletes an entity by ID.</summary>
    Task<bool> DeleteAsync(int id);
}
