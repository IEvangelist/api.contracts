// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts;

/// <summary>
/// Assembly-level attribute that configures schema generation behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AIContractConfigAttribute : Attribute
{
    /// <summary>
    /// Output folder for generated schemas, relative to the project directory.
    /// Defaults to <c>ai-skills/apis</c>.
    /// </summary>
    public string OutputFolder { get; set; } = "ai-skills/apis";

    /// <summary>
    /// When <see langword="true"/>, emits schema to the standard output location.
    /// </summary>
    public bool EmitStandard { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, emits a vendor-specific mirror of the schema.
    /// </summary>
    public bool EmitVendor { get; set; }

    /// <summary>
    /// Folder name for vendor-specific schema output.
    /// </summary>
    public string? VendorFolder { get; set; }

    /// <summary>
    /// When <see langword="true"/>, signs the emitted schema with the configured private key.
    /// </summary>
    public bool Sign { get; set; }

    /// <summary>
    /// Identifier for the signing key, embedded in the schema signature envelope.
    /// </summary>
    public string? SigningKeyId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, includes internal types and members in the schema.
    /// </summary>
    public bool IncludeInternals { get; set; }
}
