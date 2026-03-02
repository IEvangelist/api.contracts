// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ApiContracts.Verification;

/// <summary>
/// Verifies API schema signatures and computes canonical hashes.
/// </summary>
public static class SchemaVerifier
{
    private static readonly JsonWriterOptions s_canonicalWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

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
    /// Validates the structural integrity of a schema document.
    /// Checks for required fields, valid hash format, and proper structure.
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

            // Validate hash format: sha256:<64 hex chars>
            if (!declaredHash!.StartsWith("sha256:") || declaredHash.Length != 71)
            {
                return ValidationResult.Failure(
                    $"Invalid apiHash format: expected 'sha256:<64-hex-chars>', got '{declaredHash}'");
            }

            if (!root.TryGetProperty("schemaVersion", out var versionElement))
            {
                return ValidationResult.Failure("Missing schemaVersion property");
            }

            var schemaVersion = versionElement.GetString();
            if (string.IsNullOrEmpty(schemaVersion))
            {
                return ValidationResult.Failure("Empty schemaVersion value");
            }

            if (!root.TryGetProperty("package", out var packageElement) ||
                packageElement.ValueKind != JsonValueKind.Object)
            {
                return ValidationResult.Failure("Missing or invalid package object");
            }

            if (!packageElement.TryGetProperty("name", out _) ||
                !packageElement.TryGetProperty("version", out _) ||
                !packageElement.TryGetProperty("targetFramework", out _))
            {
                return ValidationResult.Failure("Package object missing required fields (name, version, targetFramework)");
            }

            if (!root.TryGetProperty("types", out var typesElement) ||
                typesElement.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Failure("Missing or invalid types array");
            }

            // Validate signature envelope if present
            if (root.TryGetProperty("signature", out var sigElement))
            {
                if (sigElement.ValueKind != JsonValueKind.Object)
                {
                    return ValidationResult.Failure("Invalid signature: expected object");
                }

                if (!sigElement.TryGetProperty("algorithm", out _) ||
                    !sigElement.TryGetProperty("publicKeyId", out _) ||
                    !sigElement.TryGetProperty("value", out _))
                {
                    return ValidationResult.Failure("Signature object missing required fields (algorithm, publicKeyId, value)");
                }
            }

            return ValidationResult.Success(declaredHash, schemaVersion!);
        }
        catch (JsonException ex)
        {
            return ValidationResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies the <c>apiHash</c> in a schema document by re-canonicalizing the
    /// types array and recomputing the SHA-256 hash.
    /// </summary>
    /// <param name="schemaJson">The full assembly schema JSON.</param>
    /// <returns>A <see cref="HashVerificationResult"/> indicating whether the hash matches.</returns>
    /// <remarks>
    /// This method reproduces the canonical serialization format used by the generator:
    /// properties sorted alphabetically, no whitespace, boolean flags always present,
    /// docs included (excluding examples and seeAlso).
    /// </remarks>
    public static HashVerificationResult VerifyApiHash(string schemaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("apiHash", out var hashElement))
            {
                return HashVerificationResult.Error("Missing apiHash property");
            }

            var declaredHash = hashElement.GetString();
            if (string.IsNullOrEmpty(declaredHash))
            {
                return HashVerificationResult.Error("Empty apiHash value");
            }

            if (!root.TryGetProperty("types", out var typesElement) ||
                typesElement.ValueKind != JsonValueKind.Array)
            {
                return HashVerificationResult.Error("Missing or invalid types array");
            }

            var canonicalJson = RecanonicalizeTypes(typesElement);
            var computedHash = ComputeApiHash(canonicalJson);

            return new HashVerificationResult
            {
                IsMatch = declaredHash == computedHash,
                DeclaredHash = declaredHash!,
                ComputedHash = computedHash,
            };
        }
        catch (JsonException ex)
        {
            return HashVerificationResult.Error($"Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Re-canonicalizes the types array from an emitted schema JSON to produce
    /// the deterministic canonical JSON used for hash computation.
    /// </summary>
    internal static string RecanonicalizeTypes(JsonElement typesArray)
    {
        // Sort types by namespace then name (matching CanonicalSerializer)
        var types = new List<JsonElement>();
        foreach (var t in typesArray.EnumerateArray())
        {
            types.Add(t);
        }

        types.Sort((a, b) =>
        {
            var nsA = a.GetProperty("namespace").GetString() ?? "";
            var nsB = b.GetProperty("namespace").GetString() ?? "";
            var cmp = string.Compare(nsA, nsB, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            var nameA = a.GetProperty("name").GetString() ?? "";
            var nameB = b.GetProperty("name").GetString() ?? "";
            return string.Compare(nameA, nameB, StringComparison.Ordinal);
        });

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, s_canonicalWriterOptions))
        {
            writer.WriteStartArray();
            foreach (var type in types)
            {
                WriteCanonicalType(writer, type);
            }
            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonicalType(Utf8JsonWriter writer, JsonElement type)
    {
        writer.WriteStartObject();

        // Properties in alphabetical order (matching CanonicalSerializer)
        writer.WriteString("accessibility",
            type.TryGetProperty("accessibility", out var acc)
                ? acc.GetString() ?? "public" : "public");

        WriteCanonicalAttributes(writer, type);

        if (type.TryGetProperty("baseType", out var baseType) &&
            baseType.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("baseType", baseType.GetString());
        }

        if (type.TryGetProperty("enumMembers", out var enumMembers) &&
            enumMembers.ValueKind == JsonValueKind.Array &&
            enumMembers.GetArrayLength() > 0)
        {
            writer.WriteStartArray("enumMembers");
            foreach (var em in enumMembers.EnumerateArray())
            {
                writer.WriteStartObject();
                writer.WriteString("name", em.GetProperty("name").GetString());
                writer.WriteNumber("value", em.GetProperty("value").GetInt64());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteString("fullName", type.GetProperty("fullName").GetString());

        if (type.TryGetProperty("genericParameters", out var genParams) &&
            genParams.ValueKind == JsonValueKind.Array &&
            genParams.GetArrayLength() > 0)
        {
            writer.WriteStartArray("genericParameters");
            foreach (var gp in genParams.EnumerateArray())
            {
                writer.WriteStartObject();
                if (gp.TryGetProperty("constraints", out var constraints) &&
                    constraints.ValueKind == JsonValueKind.Array &&
                    constraints.GetArrayLength() > 0)
                {
                    writer.WriteStartArray("constraints");
                    foreach (var c in constraints.EnumerateArray())
                    {
                        writer.WriteStringValue(c.GetString());
                    }
                    writer.WriteEndArray();
                }
                writer.WriteString("name", gp.GetProperty("name").GetString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (type.TryGetProperty("interfaces", out var interfaces) &&
            interfaces.ValueKind == JsonValueKind.Array &&
            interfaces.GetArrayLength() > 0)
        {
            writer.WriteStartArray("interfaces");
            foreach (var iface in interfaces.EnumerateArray())
            {
                writer.WriteStringValue(iface.GetString());
            }
            writer.WriteEndArray();
        }

        // Boolean flags — always written in canonical form
        writer.WriteBoolean("isAbstract", GetBool(type, "isAbstract"));
        writer.WriteBoolean("isGeneric", GetBool(type, "isGeneric"));
        writer.WriteBoolean("isSealed", GetBool(type, "isSealed"));
        writer.WriteBoolean("isStatic", GetBool(type, "isStatic"));

        writer.WriteString("kind", type.GetProperty("kind").GetString());

        // Members — sorted by kind, name, signature
        writer.WriteStartArray("members");
        var members = new List<JsonElement>();
        if (type.TryGetProperty("members", out var membersElement))
        {
            foreach (var m in membersElement.EnumerateArray())
            {
                members.Add(m);
            }
        }

        members.Sort((a, b) =>
        {
            var kindA = a.GetProperty("kind").GetString() ?? "";
            var kindB = b.GetProperty("kind").GetString() ?? "";
            var cmp = string.Compare(kindA, kindB, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            var nameA = a.GetProperty("name").GetString() ?? "";
            var nameB = b.GetProperty("name").GetString() ?? "";
            cmp = string.Compare(nameA, nameB, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            var sigA = a.TryGetProperty("signature", out var sa) ? sa.GetString() ?? "" : "";
            var sigB = b.TryGetProperty("signature", out var sb) ? sb.GetString() ?? "" : "";
            return string.Compare(sigA, sigB, StringComparison.Ordinal);
        });

        foreach (var member in members)
        {
            WriteCanonicalMember(writer, member);
        }
        writer.WriteEndArray();

        writer.WriteString("name", type.GetProperty("name").GetString());
        writer.WriteString("namespace", type.GetProperty("namespace").GetString());

        // Docs at end of type (matching CanonicalSerializer placement)
        WriteCanonicalDocs(writer, type);

        writer.WriteEndObject();
    }

    private static void WriteCanonicalMember(Utf8JsonWriter writer, JsonElement member)
    {
        writer.WriteStartObject();

        // Properties in alphabetical order
        writer.WriteString("accessibility",
            member.TryGetProperty("accessibility", out var acc)
                ? acc.GetString() ?? "public" : "public");

        WriteCanonicalAttributes(writer, member);
        WriteCanonicalDocs(writer, member);

        if (member.TryGetProperty("genericParameters", out var genParams) &&
            genParams.ValueKind == JsonValueKind.Array &&
            genParams.GetArrayLength() > 0)
        {
            writer.WriteStartArray("genericParameters");
            foreach (var gp in genParams.EnumerateArray())
            {
                writer.WriteStartObject();
                writer.WriteString("name", gp.GetProperty("name").GetString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteBoolean("isAbstract", GetBool(member, "isAbstract"));
        writer.WriteBoolean("isAsync", GetBool(member, "isAsync"));
        writer.WriteBoolean("isOverride", GetBool(member, "isOverride"));
        writer.WriteBoolean("isReturnNullable", GetBool(member, "isReturnNullable"));
        writer.WriteBoolean("isStatic", GetBool(member, "isStatic"));
        writer.WriteBoolean("isVirtual", GetBool(member, "isVirtual"));

        writer.WriteString("kind", member.GetProperty("kind").GetString());
        writer.WriteString("name", member.GetProperty("name").GetString());

        if (member.TryGetProperty("parameters", out var parameters) &&
            parameters.ValueKind == JsonValueKind.Array &&
            parameters.GetArrayLength() > 0)
        {
            writer.WriteStartArray("parameters");
            foreach (var param in parameters.EnumerateArray())
            {
                WriteCanonicalParameter(writer, param);
            }
            writer.WriteEndArray();
        }

        if (member.TryGetProperty("returnType", out var returnType) &&
            returnType.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("returnType", returnType.GetString());
        }

        writer.WriteString("signature",
            member.TryGetProperty("signature", out var sig) ? sig.GetString() : "");

        writer.WriteEndObject();
    }

    private static void WriteCanonicalParameter(Utf8JsonWriter writer, JsonElement param)
    {
        writer.WriteStartObject();

        if (param.TryGetProperty("defaultValue", out var defaultValue) &&
            defaultValue.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("defaultValue", defaultValue.GetString());
        }

        writer.WriteBoolean("isNullable", GetBool(param, "isNullable"));
        writer.WriteBoolean("isOptional", GetBool(param, "isOptional"));

        if (param.TryGetProperty("modifier", out var modifier) &&
            modifier.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("modifier", modifier.GetString());
        }

        writer.WriteString("name", param.GetProperty("name").GetString());
        writer.WriteString("type", param.GetProperty("type").GetString());
        writer.WriteEndObject();
    }

    private static void WriteCanonicalDocs(Utf8JsonWriter writer, JsonElement element)
    {
        if (!element.TryGetProperty("docs", out var docs) ||
            docs.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        writer.WritePropertyName("docs");
        writer.WriteStartObject();

        // Parameters (alphabetically sorted keys)
        if (docs.TryGetProperty("parameters", out var parameters) &&
            parameters.ValueKind == JsonValueKind.Object)
        {
            var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in parameters.EnumerateObject())
            {
                sortedParams[prop.Name] = prop.Value.GetString() ?? "";
            }

            if (sortedParams.Count > 0)
            {
                writer.WriteStartObject("parameters");
                foreach (var kvp in sortedParams)
                {
                    writer.WriteString(kvp.Key, kvp.Value);
                }
                writer.WriteEndObject();
            }
        }

        if (docs.TryGetProperty("remarks", out var remarks) &&
            remarks.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("remarks", remarks.GetString());
        }

        if (docs.TryGetProperty("returns", out var returns) &&
            returns.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("returns", returns.GetString());
        }

        if (docs.TryGetProperty("summary", out var summary) &&
            summary.ValueKind == JsonValueKind.String)
        {
            writer.WriteString("summary", summary.GetString());
        }

        // examples and seeAlso are excluded from hash per spec

        writer.WriteEndObject();
    }

    private static void WriteCanonicalAttributes(Utf8JsonWriter writer, JsonElement element)
    {
        if (!element.TryGetProperty("attributes", out var attributes) ||
            attributes.ValueKind != JsonValueKind.Array ||
            attributes.GetArrayLength() == 0)
        {
            return;
        }

        writer.WriteStartArray("attributes");
        foreach (var attr in attributes.EnumerateArray())
        {
            writer.WriteStartObject();
            if (attr.TryGetProperty("arguments", out var args) &&
                args.ValueKind == JsonValueKind.Object)
            {
                var sortedArgs = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var prop in args.EnumerateObject())
                {
                    sortedArgs[prop.Name] = prop.Value.GetString() ?? "";
                }

                if (sortedArgs.Count > 0)
                {
                    writer.WriteStartObject("arguments");
                    foreach (var kvp in sortedArgs)
                    {
                        writer.WriteString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                }
            }
            writer.WriteString("name", attr.GetProperty("name").GetString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static bool GetBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) &&
        prop.ValueKind == JsonValueKind.True;
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

/// <summary>
/// Result of an API hash verification operation.
/// </summary>
public sealed class HashVerificationResult
{
    /// <summary>Whether the declared hash matches the computed hash.</summary>
    public bool IsMatch { get; init; }

    /// <summary>The hash declared in the schema.</summary>
    public string? DeclaredHash { get; init; }

    /// <summary>The hash recomputed from the types array.</summary>
    public string? ComputedHash { get; init; }

    /// <summary>Error message if verification could not be performed.</summary>
    public string? ErrorMessage { get; init; }

    internal static HashVerificationResult Error(string message) => new()
    {
        IsMatch = false,
        ErrorMessage = message,
    };
}
