using Xunit;
using ApiContracts.Generator.Helpers;
using ApiContracts.Verification;

namespace ApiContracts.Generator.Tests;

public class HashComputerTests
{
    [Fact]
    public void ComputeSha256_MatchesVerifierOutput()
    {
        var json = """[{"name":"Test","namespace":"TestNs"}]""";

        var generatorHash = HashComputer.ComputeSha256(json);
        var verifierHash = SchemaVerifier.ComputeApiHash(json);

        Assert.Equal(generatorHash, verifierHash);
    }

    [Fact]
    public void ComputeSha256_EmptyInput_ProducesKnownHash()
    {
        // SHA-256 of empty string is well-known
        var hash = HashComputer.ComputeSha256("");
        Assert.Equal("sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", hash);
    }

    [Fact]
    public void ComputeSha256_IsDeterministic()
    {
        var input = """[{"fullName":"A.B","kind":"class","members":[],"name":"B","namespace":"A"}]""";

        var hash1 = HashComputer.ComputeSha256(input);
        var hash2 = HashComputer.ComputeSha256(input);

        Assert.Equal(hash1, hash2);
    }
}
