// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Extracts configuration from assembly-level <c>ApiContractConfigAttribute</c>.
/// </summary>
internal static class ConfigExtractor
{
    private const string ConfigAttributeName = "ApiContractConfigAttribute";
    private const string ConfigAttributeFullName = "ApiContracts.ApiContractConfigAttribute";

    public static AssemblyConfig ExtractConfig(IAssemblySymbol assembly)
    {
        var config = new AssemblyConfig();

        var attr = assembly.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass?.Name == ConfigAttributeName ||
                a.AttributeClass?.ToDisplayString() == ConfigAttributeFullName);

        if (attr is null)
        {
            return config;
        }

        foreach (var named in attr.NamedArguments)
        {
            switch (named.Key)
            {
                case nameof(AssemblyConfig.OutputFolder) when named.Value.Value is string outputFolder:
                    config.OutputFolder = outputFolder;
                    break;
                case nameof(AssemblyConfig.EmitStandard) when named.Value.Value is bool emitStd:
                    config.EmitStandard = emitStd;
                    break;
                case nameof(AssemblyConfig.EmitVendor) when named.Value.Value is bool emitVendor:
                    config.EmitVendor = emitVendor;
                    break;
                case nameof(AssemblyConfig.VendorFolder) when named.Value.Value is string vendorFolder:
                    config.VendorFolder = vendorFolder;
                    break;
                case nameof(AssemblyConfig.Sign) when named.Value.Value is bool sign:
                    config.Sign = sign;
                    break;
                case nameof(AssemblyConfig.SigningKeyId) when named.Value.Value is string keyId:
                    config.SigningKeyId = keyId;
                    break;
                case nameof(AssemblyConfig.IncludeInternals) when named.Value.Value is bool includeInternals:
                    config.IncludeInternals = includeInternals;
                    break;
            }
        }

        return config;
    }
}
