// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts;

/// <summary>
/// Marks an assembly for API schema generation. All public types are included
/// implicitly unless a type or member is annotated with <c>Ignore = true</c>.
/// Apply at the type or member level only when you need to exclude an element.
/// </summary>
[AttributeUsage(
    AttributeTargets.Assembly |
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Event,
    AllowMultiple = false,
    Inherited = true)]
public sealed class ApiContractAttribute : Attribute
{
    /// <summary>
    /// When <see langword="true"/>, excludes this element from the generated schema.
    /// </summary>
    public bool Ignore { get; set; }
}
