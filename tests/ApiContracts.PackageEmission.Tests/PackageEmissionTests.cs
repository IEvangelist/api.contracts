// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using ApiContracts.Verification;
using Xunit;

namespace ApiContracts.PackageEmission.Tests;

/// <summary>
/// Integration tests that build a test fixture project with the source generator
/// and verify the emitted schema content is correct.
/// </summary>
public class PackageEmissionTests : IDisposable
{
    private readonly string _fixtureProjectDir;
    private readonly string _fixtureProjectPath;
    private string? _generatedJson;
    private bool _built;

    public PackageEmissionTests()
    {
        // Walk up from the test output directory to find the repo root (contains api.contracts.slnx)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "api.contracts.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        _fixtureProjectDir = Path.Combine(dir!.FullName, "tests", "ApiContracts.PackageEmission.Tests", "TestFixtures", "EmissionTarget");
        _fixtureProjectPath = Path.Combine(_fixtureProjectDir, "EmissionTarget.csproj");
    }

    public void Dispose()
    {
        // Clean up build artifacts
        var binDir = Path.Combine(_fixtureProjectDir, "bin");
        var objDir = Path.Combine(_fixtureProjectDir, "obj");
        if (Directory.Exists(binDir)) Directory.Delete(binDir, recursive: true);
        if (Directory.Exists(objDir)) Directory.Delete(objDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Build_Succeeds()
    {
        EnsureBuilt();
    }

    [Fact]
    public void Build_GeneratesSchemaEmbedderSource()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();
        Assert.NotNull(json);
        Assert.NotEmpty(json);
    }

    [Fact]
    public void Schema_HasValidJsonStructure()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("$schema", out _));
        Assert.True(root.TryGetProperty("schemaVersion", out _));
        Assert.True(root.TryGetProperty("package", out _));
        Assert.True(root.TryGetProperty("apiHash", out _));
        Assert.True(root.TryGetProperty("types", out var types));
        Assert.Equal(JsonValueKind.Array, types.ValueKind);
    }

    [Fact]
    public void Schema_PassesVerification()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        var result = SchemaVerifier.ValidateSchema(json);
        Assert.True(result.IsValid, $"Schema validation failed: {result.Error}");
    }

    [Fact]
    public void Schema_ContainsPackageInfo()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var package = doc.RootElement.GetProperty("package");

        Assert.Equal("EmissionTarget", package.GetProperty("name").GetString());
        Assert.True(package.TryGetProperty("version", out _));
        Assert.Equal("net10.0", package.GetProperty("targetFramework").GetString());
    }

    [Fact]
    public void Schema_ContainsExpectedTypes()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var typeNames = types.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Customer", typeNames);
        Assert.Contains("Order", typeNames);
        Assert.Contains("OrderStatus", typeNames);
        Assert.Contains("ICustomerService", typeNames);
        Assert.Contains("IRepository", typeNames);
    }

    [Fact]
    public void Schema_CustomerType_HasExpectedMembers()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var customer = types.EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == "Customer");

        Assert.Equal("class", customer.GetProperty("kind").GetString());
        Assert.Equal("EmissionTarget.Customer", customer.GetProperty("fullName").GetString());

        var members = customer.GetProperty("members");
        var memberNames = members.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Id", memberNames);
        Assert.Contains("Name", memberNames);
        Assert.Contains("Email", memberNames);
        Assert.Contains("IsActive", memberNames);
    }

    [Fact]
    public void Schema_EnumType_HasExpectedValues()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var status = types.EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == "OrderStatus");

        Assert.Equal("enum", status.GetProperty("kind").GetString());

        var enumMembers = status.GetProperty("enumMembers");
        var memberNames = enumMembers.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Pending", memberNames);
        Assert.Contains("Confirmed", memberNames);
        Assert.Contains("Shipped", memberNames);
        Assert.Contains("Delivered", memberNames);
        Assert.Contains("Cancelled", memberNames);
    }

    [Fact]
    public void Schema_InterfaceType_HasMethods()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var svc = types.EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == "ICustomerService");

        Assert.Equal("interface", svc.GetProperty("kind").GetString());

        var members = svc.GetProperty("members");
        var memberNames = members.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("GetByIdAsync", memberNames);
        Assert.Contains("CreateAsync", memberNames);
        Assert.Contains("SearchAsync", memberNames);
    }

    [Fact]
    public void Schema_GenericType_HasTypeParameters()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var repo = types.EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == "IRepository");

        Assert.Equal("interface", repo.GetProperty("kind").GetString());
        Assert.True(repo.GetProperty("isGeneric").GetBoolean());
        Assert.True(repo.TryGetProperty("genericParameters", out var gps));

        var gp = gps.EnumerateArray().First();
        Assert.Equal("T", gp.GetProperty("name").GetString());
    }

    [Fact]
    public void Schema_HasDocumentation()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var customer = types.EnumerateArray()
            .First(t => t.GetProperty("name").GetString() == "Customer");

        Assert.True(customer.TryGetProperty("docs", out var docs));
        // summary is now an array of doc nodes
        var summaryArr = docs.GetProperty("summary");
        Assert.Equal(JsonValueKind.Array, summaryArr.ValueKind);
        var firstNode = summaryArr.EnumerateArray().First();
        Assert.Equal("A customer entity for testing schema generation.", firstNode.GetProperty("text").GetString());
    }

    [Fact]
    public void Schema_ApiHash_IsDeterministic()
    {
        EnsureBuilt();
        var json1 = GetGeneratedJson();

        // Reset and rebuild
        _built = false;
        _generatedJson = null;
        Dispose();
        EnsureBuilt();
        var json2 = GetGeneratedJson();

        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);

        var hash1 = doc1.RootElement.GetProperty("apiHash").GetString();
        var hash2 = doc2.RootElement.GetProperty("apiHash").GetString();

        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1);
    }

    [Fact]
    public void Schema_TypesSortedByNamespaceThenName()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        using var doc = JsonDocument.Parse(json);
        var types = doc.RootElement.GetProperty("types");
        var fullNames = types.EnumerateArray()
            .Select(t => t.GetProperty("fullName").GetString()!)
            .Where(n => n.StartsWith("EmissionTarget."))
            .ToList();

        var sorted = fullNames.OrderBy(n => n).ToList();
        Assert.Equal(sorted, fullNames);
    }

    [Fact]
    public void Schema_NoSentinelValues()
    {
        EnsureBuilt();
        var json = GetGeneratedJson();

        Assert.DoesNotContain("\"_\":", json);
        Assert.DoesNotContain("\"generatedAt\":", json);
    }

    #region Build Helpers

    private void EnsureBuilt()
    {
        if (_built) return;

        Assert.True(File.Exists(_fixtureProjectPath),
            $"Fixture project not found at: {_fixtureProjectPath}");

        var result = RunDotnetBuild(_fixtureProjectPath);
        Assert.True(result.ExitCode == 0,
            $"dotnet build failed (exit code {result.ExitCode}):\n{result.Output}\n{result.Error}");

        _built = true;
    }

    private string GetGeneratedJson()
    {
        if (_generatedJson is not null) return _generatedJson;

        // Source generator output is in obj/GeneratedFiles/ when EmitCompilerGeneratedFiles=true
        var generatedDir = Path.Combine(_fixtureProjectDir, "obj", "GeneratedFiles");
        var generatedFiles = Directory.GetFiles(generatedDir, "*.api-schema.json.g.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(generatedFiles);

        var embedderSource = File.ReadAllText(generatedFiles[0]);

        // Extract JSON from the verbatim string literal: @"..."
        var startMarker = "=> @\"";
        var startIdx = embedderSource.IndexOf(startMarker);
        Assert.True(startIdx >= 0, "Could not find schema property in generated source");
        startIdx += startMarker.Length;

        var endIdx = embedderSource.LastIndexOf("\";");
        Assert.True(endIdx > startIdx, "Could not find end of schema string");

        var escaped = embedderSource[startIdx..endIdx];
        _generatedJson = escaped.Replace("\"\"", "\"");

        return _generatedJson;
    }

    private static (int ExitCode, string Output, string Error) RunDotnetBuild(string projectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -v quiet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit(120_000);

        return (process.ExitCode, output, error);
    }

    #endregion
}
