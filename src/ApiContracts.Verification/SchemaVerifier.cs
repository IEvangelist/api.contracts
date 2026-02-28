// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ApiContracts.Verification;

/// <summary>
/// Verifies API schema signatures and computes canonical hashes.
/// </summary>
public static class SchemaVerifier
{
    /// <summary>
    /// Computes the SHA-256 hash of the canonical API model JSON.
    /// </summary>
    /// <param name="canonicalJson">The canonical JSON string (sorted, no whitespace, UTF-8).</param>
    /// <returns>A hash string in the format <c>sha256:&lt;hex&gt;</c>.</returns>
    public static string ComputeApiHash(string canonicalJson)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalJson);
        var hash = SHA256.HashData(bytes);
        var hex = Convert.ToHexStringLower(hash);
        return $"sha256:{hex}";
    }

    /// <summary>
    /// Verifies an RSA-SHA256 signature against the given data.
    /// </summary>
    /// <param name="data">The data that was signed (canonical JSON bytes).</param>
    /// <param name="signatureBase64">The Base64-encoded signature value.</param>
    /// <param name="publicKeyPem">The PEM-encoded RSA public key.</param>
    /// <returns><see langword="true"/> if the signature is valid.</returns>
    public static bool VerifySignature(string data, string signatureBase64, string publicKeyPem)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signatureBytes = Convert.FromBase64String(signatureBase64);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        return rsa.VerifyData(
            dataBytes,
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Signs data using RSA-SHA256 and returns the Base64-encoded signature.
    /// </summary>
    /// <param name="data">The data to sign (canonical JSON).</param>
    /// <param name="privateKeyPem">The PEM-encoded RSA private key.</param>
    /// <returns>Base64-encoded signature.</returns>
    public static string SignData(string data, string privateKeyPem)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var signatureBytes = rsa.SignData(
            dataBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Validates that the <c>apiHash</c> in a schema document matches the computed hash of its types.
    /// </summary>
    /// <param name="schemaJson">The full assembly schema JSON.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating success or failure.</returns>
    public static ValidationResult ValidateSchema(string schemaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("apiHash", out var hashElement))
            {
                return ValidationResult.Failure("Missing apiHash property");
            }

            var declaredHash = hashElement.GetString();
            if (string.IsNullOrEmpty(declaredHash))
            {
                return ValidationResult.Failure("Empty apiHash value");
            }

            if (!root.TryGetProperty("schemaVersion", out var versionElement))
            {
                return ValidationResult.Failure("Missing schemaVersion property");
            }

            if (!root.TryGetProperty("types", out var typesElement) ||
                typesElement.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Failure("Missing or invalid types array");
            }

            return ValidationResult.Success(declaredHash!, versionElement.GetString() ?? "unknown");
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }
}

/// <summary>
/// Result of a schema validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Whether validation succeeded.</summary>
    public bool IsValid { get; private init; }

    /// <summary>Error message if validation failed.</summary>
    public string? Error { get; private init; }

    /// <summary>The API hash from the schema.</summary>
    public string? ApiHash { get; private init; }

    /// <summary>The schema version.</summary>
    public string? SchemaVersion { get; private init; }

    internal static ValidationResult Success(string apiHash, string schemaVersion) => new()
    {
        IsValid = true,
        ApiHash = apiHash,
        SchemaVersion = schemaVersion,
    };

    internal static ValidationResult Failure(string error) => new()
    {
        IsValid = false,
        Error = error,
    };
}
