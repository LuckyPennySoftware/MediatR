# License key configuration

MediatR is [dual licensed](https://github.com/LuckyPennySoftware/MediatR/blob/main/LICENSE.md). To configure the commercial license, either the license key can be set using `Microsoft.Extensions.DependencyInjection` integration:

```csharp
services.AddMediatR(cfg => {
    cfg.LicenseKey = "License key here";

    // Other configuration
});
```

If the key comes from a service in the container, such as a secret store client, use a factory:

```csharp
services.AddMediatR(cfg => {
    cfg.LicenseKeyFactory = sp => sp.GetRequiredService<ISecretStore>().GetSecret("MediatR-License-Key");

    // Other configuration
});
```

The factory runs on a background thread after the first `Mediator` is created, so slow work won't delay startup or block `Mediator` construction. It receives the root service provider, so it can't resolve scoped services. If it throws, the exception is logged under the license log category. `AddOptions<MediatRServiceConfiguration>()` isn't supported, because MediatR needs its configuration while registering services, before a service provider exists.

Or on non-MS.Ext.DI scenarios, where you're configuring a 3rd-party container directly, you can set the license key with a static property:

```csharp
Mediator.LicenseKey = "License key here";
```

You can obtain a valid license from the [MediatR website](https://mediatr.io).

## License Enforcement

Licensing is enforced via log messages at various levels:

- INFO: Valid license message, or license validation is not supported on this platform (see [Client Redistribution Scenarios](#client-redistribution-scenarios))
- WARNING: Missing license message, or a license term that has ended but is still within its grace period
- ERROR: Invalid/expired license message, or a license key that is incomplete or corrupted
- CRITICAL: The license key could not be validated (see [Troubleshooting license key validation](#troubleshooting-license-key-validation))

There is no other license enforcement besides log messages. No central license server, no outbound HTTP calls, no degrading or disabling of features.

The log messages are logged using standard `Microsoft.Extensions.Logging` loggers under the category name `LuckyPennySoftware.MediatR.License`.

## Client Redistribution Scenarios

In the case where MediatR is used on a client, including:

- Blazor WASM
- WPF/MAUI/Desktop apps
- Redistributed clients

The license key should NOT be set as this would result in secrets transmitted to the client. Instead, omit the license key configuration and mute the license message category name using:

```csharp
builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);
```

This will depend on your logging setup. A missing/invalid license key does not affect runtime behavior in any way.

If a license key is set on a platform that does not support RSA cryptography, such as Blazor WASM, MediatR skips validation and logs an INFO message linking to this section instead of a validation error.

## Auto-discovery via environment variables

If no license key is set in code, MediatR looks for one in environment variables. This is convenient for containerized and cloud environments, and for enterprises that share a single key across many services without code changes:

- `MEDIATR_LICENSE_KEY` – the MediatR-specific license key.
- `LUCKYPENNY_LICENSE_KEY` – a shared key usable across Lucky Penny products (for example, [AutoMapper](https://github.com/LuckyPennySoftware/AutoMapper) reads the same variable). Because it is shared, the key must be for a license that includes MediatR (a `Bundle` or MediatR edition); an AutoMapper-only license will not validate here.

The license key is resolved in the following order of precedence, using the first value found:

1. An explicit value set in code (`cfg.LicenseKeyFactory`, `cfg.LicenseKey`, then `Mediator.LicenseKey`).
2. The `MEDIATR_LICENSE_KEY` environment variable.
3. The `LUCKYPENNY_LICENSE_KEY` environment variable.

No code change is required when using an environment variable—just register MediatR as usual without setting the license key.

You can register for your license key at [MediatR.io](https://mediatr.io)

## Renewal grace period

License keys include a 30-day grace period after the end of their term. When your subscription renews, you receive a new license key, and the previous key keeps logging only a WARNING (not an ERROR) for 30 days after its term ends. This gives you time to deploy the renewed key. Your current license key is always available on your [account page](https://luckypennysoftware.com/account).

## Troubleshooting license key validation

When a license key can't be validated, MediatR logs a message describing the problem. The underlying exception is logged separately at `Debug` level; to see it, enable debug logging for the `LuckyPennySoftware.MediatR.License` category:

```csharp
builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.Debug);
```

### The license key is incomplete or corrupted

The key is missing characters or contains characters that aren't part of a license key. This usually happens when the key is copied from a PDF or an email, where it can be truncated or wrapped across lines.

MediatR removes whitespace (spaces, tabs, and line breaks) from the key before validating it, so line breaks and trailing newlines, such as those in Kubernetes secrets created from files, are not a problem. Missing characters can't be recovered: copy the full key again from your [account page](https://luckypennysoftware.com/account).

### The license key signature does not match

The key is well formed but its contents have been changed, usually by a copy/paste or formatting problem. Copy the full key again from your [account page](https://luckypennysoftware.com/account).

Online JWT decoders such as jwt.io can show a damaged key as valid, because they ignore whitespace and don't check the signature against the Lucky Penny Software key. Use your account page as the source of truth.

### The license key is not a Lucky Penny Software license key

The value is a token, but not one issued by Lucky Penny Software. Check that the value you configured (in code, or in the `MEDIATR_LICENSE_KEY` or `LUCKYPENNY_LICENSE_KEY` environment variables) is the license key from your [account page](https://luckypennysoftware.com/account).

### Mismatched Microsoft.IdentityModel package versions

MediatR validates license keys with the `Microsoft.IdentityModel.JsonWebTokens` package. If your application references different versions of the `Microsoft.IdentityModel.*` packages (for example, through an authentication library that pins an older `Microsoft.IdentityModel.Tokens`), validation fails with an error such as:

```
System.MissingMethodException: Method not found: 'Void Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(...)'
```

To find the versions in use, run:

```
dotnet list package --include-transitive
```

Then reference the same version of every `Microsoft.IdentityModel.*` package, for example by adding direct references at the highest version in use:

```xml
<PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" Version="8.14.0" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.14.0" />
```

If you use [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management), you can enable transitive pinning (`CentralPackageTransitivePinningEnabled`) and pin the versions in `Directory.Packages.props`.
