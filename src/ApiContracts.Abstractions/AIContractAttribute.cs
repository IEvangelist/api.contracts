// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts;

/// <summary>
/// Controls how a public API element appears in the emitted AI schema.
/// Apply to types, methods, properties, or parameters to override defaults.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Event,
    AllowMultiple = false,
    Inherited = true)]
public sealed class AIContractAttribute : Attribute
{
    /// <summary>
    /// Overrides the display name in the schema.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Provides a human-readable description for AI consumers.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Assigns a category for grouping in documentation and tooling.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Assigns a semantic role (e.g., "request", "response", "configuration").
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Comma-separated tags for search and filtering.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// When <see langword="true"/>, excludes this element from the schema.
    /// </summary>
    public bool Exclude { get; set; }
}
