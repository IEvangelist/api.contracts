// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Assembly-level configuration extracted from attributes and MSBuild properties.
/// </summary>
internal sealed class AssemblyConfig
{
    public string OutputFolder { get; set; } = "ai-skills/apis";
    public bool EmitStandard { get; set; } = true;
    public bool EmitVendor { get; set; }
    public string? VendorFolder { get; set; }
    public bool Sign { get; set; }
    public string? SigningKeyId { get; set; }
    public bool IncludeInternals { get; set; }
}
