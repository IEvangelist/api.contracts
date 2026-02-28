// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Computes deterministic SHA-256 hashes from canonical JSON.
/// </summary>
internal static class HashComputer
{
    /// <summary>
    /// Computes a SHA-256 hash of the canonical JSON string.
    /// Returns <c>sha256:&lt;hex&gt;</c>.
    /// </summary>
    public static string ComputeSha256(string canonicalJson)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalJson);

#if NETSTANDARD2_0
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
#else
        var hash = SHA256.HashData(bytes);
#endif

        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return $"sha256:{hex}";
    }
}
