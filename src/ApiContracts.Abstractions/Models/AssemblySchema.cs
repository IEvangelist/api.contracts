// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Models;

/// <summary>
/// Assembly-level schema snapshot. Corresponds to
/// <c>{AssemblyName}.api-schema.json</c> in the NuGet package content.
/// </summary>
public sealed class AssemblySchema
{
    /// <summary>Schema format version matching the root schema.</summary>
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>Reference to the root schema path.</summary>
    public string RootSchema { get; set; } = "../../schema.json";

    /// <summary>Package metadata.</summary>
    public PackageInfo Package { get; set; } = new();

    /// <summary>All public types in this assembly.</summary>
    public List<TypeModel> Types { get; set; } = [];

    /// <summary>Deterministic hash of the canonical API model.</summary>
    public string ApiHash { get; set; } = "";

    /// <summary>Optional cryptographic signature envelope.</summary>
    public SignatureEnvelope? Signature { get; set; }
}

/// <summary>
/// Package/assembly metadata.
/// </summary>
public sealed class PackageInfo
{
    /// <summary>Assembly name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Assembly/package version.</summary>
    public string Version { get; set; } = "";

    /// <summary>Target framework.</summary>
    public string TargetFramework { get; set; } = "";

    /// <summary>Assembly description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Cryptographic signature envelope.
/// </summary>
public sealed class SignatureEnvelope
{
    /// <summary>Signing algorithm (e.g., "RSA-SHA256").</summary>
    public string Algorithm { get; set; } = "RSA-SHA256";

    /// <summary>Public key identifier.</summary>
    public string PublicKeyId { get; set; } = "";

    /// <summary>Base64-encoded signature value.</summary>
    public string Value { get; set; } = "";
}
