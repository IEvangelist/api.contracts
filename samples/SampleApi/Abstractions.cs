// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SampleApi;

/// <summary>
/// Base class for entities that require audit tracking.
/// </summary>
/// <remarks>
/// All entities inheriting from <see cref="AuditableEntity"/> automatically receive
/// a unique identifier and timestamp fields for creation and last modification.
/// The <see cref="CreatedBy"/> and <see cref="UpdatedBy"/> properties record the
/// identity of the user who performed the respective action.
/// </remarks>
/// <seealso cref="SoftDeletableEntity"/>
public abstract class AuditableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the UTC date and time when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC date and time when the entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base class for entities that support soft deletion in addition to audit tracking.
/// </summary>
/// <remarks>
/// Entities derived from <see cref="SoftDeletableEntity"/> are never physically removed
/// from the data store. Instead, the <see cref="IsDeleted"/> flag is set to
/// <see langword="true"/> and <see cref="DeletedAt"/> records the time of deletion.
/// Query logic should filter out soft-deleted entities by default.
/// </remarks>
/// <seealso cref="AuditableEntity"/>
public abstract class SoftDeletableEntity : AuditableEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the entity was soft-deleted.
    /// </summary>
    /// <value>
    /// The deletion timestamp, or <see langword="null"/> if the entity has not been deleted.
    /// </value>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Marks the entity as deleted by setting <see cref="IsDeleted"/> to <see langword="true"/>
    /// and recording the current UTC time and the identity of the user performing the deletion.
    /// </summary>
    /// <param name="deletedBy">The identifier of the user performing the deletion.</param>
    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a previously soft-deleted entity, clearing deletion metadata.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
