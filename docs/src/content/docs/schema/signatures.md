---
title: Signatures
description: RSA-SHA256 cryptographic signatures for schema integrity.
---

API Contracts supports optional RSA-SHA256 signatures.

## Signature Envelope

```json
{
  "signature": {
    "algorithm": "RSA-SHA256",
    "publicKeyId": "pine-2026",
    "value": "<base64>"
  }
}
```

## Enabling

```xml
<PropertyGroup>
  <AISchemaSign>true</AISchemaSign>
  <AISchemaSigningPrivateKey>keys/signing.pem</AISchemaSigningPrivateKey>
</PropertyGroup>
```

## Key Management

```bash
# Generate key pair
openssl genrsa -out signing.pem 2048
openssl rsa -in signing.pem -pubout -out signing.pub.pem
```

## Verification

```csharp
using ApiContracts.Verification;

bool valid = SchemaVerifier.VerifySignature(data, signatureBase64, publicKeyPem);
string sig = SchemaVerifier.SignData(data, privateKeyPem);
```
