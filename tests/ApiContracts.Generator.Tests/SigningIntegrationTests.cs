using Xunit;
using ApiContracts.Generator.Helpers;
using ApiContracts.Verification;

namespace ApiContracts.Generator.Tests;

public class SigningIntegrationTests
{
    [Fact]
    public void EmitAssemblySchema_WithSignature_IncludesSignatureEnvelope()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "TestType",
                FullName = "TestNs.TestType",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
            }
        };

        var config = new AssemblyConfig
        {
            Sign = true,
            SigningKeyId = "test-key-2025"
        };

        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc",
            config, signatureValue: "dGVzdC1zaWduYXR1cmU=");

        Assert.Contains("\"signature\":", json);
        Assert.Contains("\"algorithm\": \"RSA-SHA256\"", json);
        Assert.Contains("\"publicKeyId\": \"test-key-2025\"", json);
        Assert.Contains("\"value\": \"dGVzdC1zaWduYXR1cmU=\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_WithoutSignature_OmitsSignatureEnvelope()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "TestType",
                FullName = "TestNs.TestType",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
            }
        };

        var config = new AssemblyConfig();

        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.DoesNotContain("\"signature\":", json);
    }

    [Fact]
    public void FullSigningPipeline_HashSignVerify_Succeeds()
    {
        // Build canonical model
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Customer",
                FullName = "Models.Customer",
                Namespace = "Models",
                Kind = "class",
                Accessibility = "public",
                Members =
                [
                    new CanonicalMember
                    {
                        Name = "Name",
                        Kind = "property",
                        Accessibility = "public",
                        Signature = "string Name { get; set; }",
                        ReturnType = "string",
                    },
                    new CanonicalMember
                    {
                        Name = "Id",
                        Kind = "property",
                        Accessibility = "public",
                        Signature = "int Id { get; set; }",
                        ReturnType = "int",
                    }
                ],
            }
        };

        // Step 1: Serialize canonically
        var canonicalJson = CanonicalSerializer.SerializeForHashing(types);

        // Step 2: Hash
        var apiHash = HashComputer.ComputeSha256(canonicalJson);
        Assert.StartsWith("sha256:", apiHash);

        // Step 3: Sign
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();

        var signature = SchemaVerifier.SignData(canonicalJson, privateKey);
        Assert.NotEmpty(signature);

        // Step 4: Verify
        var isValid = SchemaVerifier.VerifySignature(canonicalJson, signature, publicKey);
        Assert.True(isValid);

        // Step 5: Emit schema with signature
        var config = new AssemblyConfig { Sign = true, SigningKeyId = "test-2025" };
        var schemaJson = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, apiHash, config, signature);

        Assert.Contains($"\"apiHash\": \"{apiHash}\"", schemaJson);
        Assert.Contains("\"algorithm\": \"RSA-SHA256\"", schemaJson);
        Assert.Contains("\"publicKeyId\": \"test-2025\"", schemaJson);

        // Step 6: Validate schema structure
        var result = SchemaVerifier.ValidateSchema(schemaJson);
        Assert.True(result.IsValid);
        Assert.Equal(apiHash, result.ApiHash);
    }

    [Fact]
    public void EmitAssemblySchema_ProducesValidJson()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Test",
                FullName = "Ns.Test",
                Namespace = "Ns",
                Kind = "class",
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"$schema\":", json);
        Assert.Contains("\"schemaVersion\":", json);
        Assert.Contains("\"types\":", json);
        Assert.DoesNotContain("\"generatedAt\":", json);
    }

    [Fact]
    public void VerifySignature_WithWrongKey_Fails()
    {
        using var rsa1 = System.Security.Cryptography.RSA.Create(2048);
        using var rsa2 = System.Security.Cryptography.RSA.Create(2048);
        var data = "test data for signing";

        var signature = SchemaVerifier.SignData(data, rsa1.ExportRSAPrivateKeyPem());
        var isValid = SchemaVerifier.VerifySignature(data, signature, rsa2.ExportRSAPublicKeyPem());

        Assert.False(isValid);
    }
}
