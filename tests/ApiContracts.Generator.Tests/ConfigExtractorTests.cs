// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class ConfigExtractorTests
{
    [Fact]
    public void ExtractConfig_NoAttribute_ReturnsDefaults()
    {
        // When there's no ApiContractConfigAttribute, defaults should be used
        var config = new AssemblyConfig();

        Assert.Equal("ai-skills/apis", config.OutputFolder);
        Assert.True(config.EmitStandard);
        Assert.False(config.EmitVendor);
        Assert.Null(config.VendorFolder);
        Assert.False(config.Sign);
        Assert.Null(config.SigningKeyId);
        Assert.False(config.IncludeInternals);
    }
}
