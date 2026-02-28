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
            "types": [],
            "apiHash": "sha256:abc123"
        }
        """;

        var result = SchemaVerifier.ValidateSchema(schema);

        Assert.True(result.IsValid);
        Assert.Equal("sha256:abc123", result.ApiHash);
        Assert.Equal("1.0.0", result.SchemaVersion);
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
}
