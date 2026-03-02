using Xunit;
using ApiContracts.Verification;

namespace ApiContracts.Generator.Tests;

public class SchemaVerifierTests
{
    [Fact]
    public void ComputeApiHash_ProducesDeterministicHash()
    {
        // Arrange
        var json = """[{"name":"Test","namespace":"TestNs"}]""";

        // Act
        var hash1 = SchemaVerifier.ComputeApiHash(json);
        var hash2 = SchemaVerifier.ComputeApiHash(json);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1);
        Assert.Equal(71, hash1.Length); // "sha256:" + 64 hex chars
    }

    [Fact]
    public void ComputeApiHash_DifferentInputProducesDifferentHash()
    {
        var hash1 = SchemaVerifier.ComputeApiHash("""[{"name":"A"}]""");
        var hash2 = SchemaVerifier.ComputeApiHash("""[{"name":"B"}]""");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void SignAndVerify_RoundTrips()
    {
        // Arrange
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();
        var data = """{"types":[],"apiHash":"sha256:abc"}""";

        // Act
        var signature = SchemaVerifier.SignData(data, privateKey);
        var isValid = SchemaVerifier.VerifySignature(data, signature, publicKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_FailsWithTamperedData()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();
        var data = """{"types":[],"apiHash":"sha256:abc"}""";

        var signature = SchemaVerifier.SignData(data, privateKey);
        var isValid = SchemaVerifier.VerifySignature(data + "tampered", signature, publicKey);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateSchema_ValidSchema_ReturnsSuccess()
    {
        var schema = """
        {
            "schemaVersion": "1.0.0",
            "package": { "name": "TestLib", "version": "1.0.0", "targetFramework": "net10.0" },
            "types": [],
            "apiHash": "sha256:abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd"
        }
        """;

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.True(result.IsValid);
        Assert.Equal("sha256:abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd", result.ApiHash);
        Assert.Equal("1.0.0", result.SchemaVersion);
    }

    [Fact]
    public void ValidateSchema_InvalidHashFormat_ReturnsFailure()
    {
        var schema = """
        {
            "schemaVersion": "1.0.0",
            "package": { "name": "TestLib", "version": "1.0.0", "targetFramework": "net10.0" },
            "types": [],
            "apiHash": "sha256:tooshort"
        }
        """;

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.False(result.IsValid);
        Assert.Contains("Invalid apiHash format", result.Error);
    }

    [Fact]
    public void ValidateSchema_MissingPackage_ReturnsFailure()
    {
        var schema = """
        {
            "schemaVersion": "1.0.0",
            "types": [],
            "apiHash": "sha256:abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd"
        }
        """;

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.False(result.IsValid);
        Assert.Contains("package", result.Error);
    }

    [Fact]
    public void ValidateSchema_InvalidSignatureEnvelope_ReturnsFailure()
    {
        var schema = """
        {
            "schemaVersion": "1.0.0",
            "package": { "name": "TestLib", "version": "1.0.0", "targetFramework": "net10.0" },
            "types": [],
            "apiHash": "sha256:abc123abc123abc123abc123abc123abc123abc123abc123abc123abc123abcd",
            "signature": { "algorithm": "RSA-SHA256" }
        }
        """;

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.False(result.IsValid);
        Assert.Contains("Signature", result.Error);
    }

    [Fact]
    public void ValidateSchema_MissingHash_ReturnsFailure()
    {
        var schema = """{"schemaVersion": "1.0.0", "types": []}""";

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.False(result.IsValid);
        Assert.Contains("apiHash", result.Error);
    }

    [Fact]
    public void ValidateSchema_InvalidJson_ReturnsFailure()
    {
        var result = SchemaVerifier.ValidateSchema("not valid json");

        Assert.False(result.IsValid);
        Assert.Contains("Invalid JSON", result.Error);
    }

    [Fact]
    public void VerifyApiHash_MatchesCanonicalHash()
    {
        // Build a schema with known canonical form and compute the expected hash
        var canonicalJson = """[{"accessibility":"public","fullName":"TestNs.A","isAbstract":false,"isGeneric":false,"isSealed":false,"isStatic":false,"kind":"class","members":[],"name":"A","namespace":"TestNs"}]""";
        var expectedHash = SchemaVerifier.ComputeApiHash(canonicalJson);

        var schema = $$"""
        {
            "schemaVersion": "1.0.0",
            "package": { "name": "TestLib", "version": "1.0.0", "targetFramework": "net10.0" },
            "apiHash": "{{expectedHash}}",
            "types": [
                {
                    "name": "A",
                    "fullName": "TestNs.A",
                    "namespace": "TestNs",
                    "kind": "class",
                    "accessibility": "public",
                    "members": []
                }
            ]
        }
        """;

        var result = SchemaVerifier.VerifyApiHash(schema);

        Assert.True(result.IsMatch, $"Hash mismatch: declared={result.DeclaredHash}, computed={result.ComputedHash}");
        Assert.Equal(expectedHash, result.ComputedHash);
    }

    [Fact]
    public void VerifyApiHash_DetectsTampering()
    {
        // Use a hash from a different type name
        var wrongHash = SchemaVerifier.ComputeApiHash("""[{"name":"Original"}]""");

        var schema = $$"""
        {
            "schemaVersion": "1.0.0",
            "package": { "name": "TestLib", "version": "1.0.0", "targetFramework": "net10.0" },
            "apiHash": "{{wrongHash}}",
            "types": [
                {
                    "name": "Tampered",
                    "fullName": "TestNs.Tampered",
                    "namespace": "TestNs",
                    "kind": "class",
                    "accessibility": "public",
                    "members": []
                }
            ]
        }
        """;

        var result = SchemaVerifier.VerifyApiHash(schema);

        Assert.False(result.IsMatch);
        Assert.NotEqual(result.DeclaredHash, result.ComputedHash);
    }

    [Fact]
    public void VerifyApiHash_MissingTypes_ReturnsError()
    {
        var schema = """{"apiHash": "sha256:abc"}""";
        var result = SchemaVerifier.VerifyApiHash(schema);

        Assert.False(result.IsMatch);
        Assert.Contains("types", result.ErrorMessage);
    }
}
