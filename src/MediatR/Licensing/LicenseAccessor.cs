using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Convert = System.Convert;

namespace MediatR.Licensing;

internal class LicenseAccessor
{
    internal const string MediatRLicenseKeyEnvVariable = "MEDIATR_LICENSE_KEY";
    internal const string SharedLicenseKeyEnvVariable = "LUCKYPENNY_LICENSE_KEY";

    private readonly MediatRServiceConfiguration? _configuration;
    private readonly ILogger _logger;

    public LicenseAccessor(MediatRServiceConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger("LuckyPennySoftware.MediatR.License");
    }   
    
    public LicenseAccessor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("LuckyPennySoftware.MediatR.License");
    }
    
    private License? _license;
    private readonly object _lock = new();

    public License Current => _license ??= Initialize();

    private License Initialize()
    {
        lock (_lock)
        {
            if (_license != null)
            {
                return _license;
            }

            var key = ResolveLicenseKey(_configuration?.LicenseKey, Mediator.LicenseKey);

            if (string.IsNullOrWhiteSpace(key))
            {
                return new License();
            }

            var licenseClaims = ValidateKey(key!);
            return licenseClaims.Any() 
                ? new License(new ClaimsPrincipal(new ClaimsIdentity(licenseClaims))) 
                : new License();
        }
    }
    
    /// <summary>
    /// Resolves the license key using the first non-blank value, in order of precedence: the
    /// explicitly configured keys (the <see cref="MediatRServiceConfiguration.LicenseKey"/> then the
    /// static <see cref="Mediator.LicenseKey"/>), the product-specific <c>MEDIATR_LICENSE_KEY</c>
    /// environment variable, then the shared <c>LUCKYPENNY_LICENSE_KEY</c> environment variable
    /// (usable across Lucky Penny products). Whitespace is removed from each value, and empty values
    /// are treated as unset.
    /// </summary>
    internal static string? ResolveLicenseKey(params string?[] explicitKeys) =>
        explicitKeys.Select(NormalizeLicenseKey).FirstOrDefault(key => key != null)
        ?? NormalizeLicenseKey(Environment.GetEnvironmentVariable(MediatRLicenseKeyEnvVariable))
        ?? NormalizeLicenseKey(Environment.GetEnvironmentVariable(SharedLicenseKeyEnvVariable));

    // A license key is a compact JWS (base64url and '.'), so whitespace is never meaningful. Keys copied
    // from PDFs, email, or secret files often pick up line breaks and trailing newlines.
    private static string? NormalizeLicenseKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : new string(key!.Where(c => !char.IsWhiteSpace(c)).ToArray());

    // RS256 signature over the 2048-bit Lucky Penny key: 256 bytes, base64url-encoded without padding.
    private const int SignatureLength = 342;

    private static readonly RSAParameters LicenseKeyParameters = new()
    {
        Exponent = Convert.FromBase64String("AQAB"),
        Modulus = Convert.FromBase64String(
            "2LTtdJV2b0mYoRqChRCfcqnbpKvsiCcDYwJ+qPtvQXWXozOhGo02/V0SWMFBdbZHUzpEytIiEcojo7Vbq5mQmt4lg92auyPKsWq6qSmCVZCUuL/kpYqLCit4yUC0YqZfw4H9zLf1yAIOgyXQf1x6g+kscDo1pWAniSl9a9l/LXRVEnGz+OfeUrN/5gzpracGUY6phx6T09UCRuzi4YqqO4VJzL877W0jCW2Q7jMzHxOK04VSjNc22CADuCd34mrFs23R0vVm1DVLYtPGD76/rGOcxO6vmRc7ydBAvt1IoUsrY0vQ2rahp51YPxqqhKPd8nNOomHWblCCA7YUeV3C1Q==")
    };

    internal Claim[] ValidateKey(string licenseKey)
    {
        if (!IsWellFormed(licenseKey))
        {
            // Never log the key itself.
            _logger.LogError(
                "The Lucky Penny software license key appears to be incomplete or corrupted " +
                "(it may have been truncated when copied). " +
                "Copy the full key again from https://luckypennysoftware.com/account.");
            return [];
        }

        try
        {
            // JsonWebTokenHandler swallows a PlatformNotSupportedException from the RSA provider and
            // reports it as an IDX10511 signature failure, with the original exception only as (PII-hidden)
            // message text. Probe RSA directly so client platforms such as Blazor WASM are detected.
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(LicenseKeyParameters);
            }

            var handler = new JsonWebTokenHandler();

            var key = new RsaSecurityKey(LicenseKeyParameters)
            {
                KeyId = "LuckyPennySoftwareLicenseKey/bbb13acb59904d89b4cb1c85f088ccf9"
            };

            var parms = new TokenValidationParameters
            {
                ValidIssuer = "https://luckypennysoftware.com",
                ValidAudience = "LuckyPennySoftware",
                IssuerSigningKey = key,
                ValidateLifetime = false
            };

            // Runs on the dedicated background thread started during Mediator construction
            // (see MediatRServiceCollectionExtensions.CheckLicense / AutoMapper #4640), so there is
            // no SynchronizationContext to deadlock on; local JWT validation completes synchronously,
            // so this does not depend on the thread pool.
            var validateResult = handler.ValidateTokenAsync(licenseKey, parms).GetAwaiter().GetResult();
            if (!validateResult.IsValid)
            {
                LogValidationFailure(validateResult.Exception);
            }

            return validateResult.ClaimsIdentity?.Claims.ToArray() ?? [];
        }
        catch (PlatformNotSupportedException)
        {
            LogPlatformNotSupported();
            return [];
        }
    }

    /// <summary>
    /// Cheap structural check of a compact JWS: three dot-separated base64url segments, with a
    /// signature segment of the expected length. Catches keys truncated or mangled by copy/paste.
    /// </summary>
    internal static bool IsWellFormed(string licenseKey)
    {
        var segments = licenseKey.Split('.');
        return segments.Length == 3
            && segments.All(segment => segment.Length > 0 && segment.All(IsBase64UrlChar))
            && segments[2].Length == SignatureLength;

        static bool IsBase64UrlChar(char c) =>
            c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_';
    }

    internal void LogValidationFailure(Exception? exception)
    {
        if (ContainsPlatformNotSupported(exception))
        {
            LogPlatformNotSupported();
            return;
        }

        // IdentityModel's IDX messages aren't actionable for customers (and hide most details unless PII
        // logging is on), so log a plain message and keep the exception at debug level for support.
        _logger.LogCritical(exception switch
        {
            SecurityTokenInvalidSignatureException =>
                "The Lucky Penny software license key is not valid: its signature does not match. " +
                "If the key was copied from a PDF, email, or file, it may have been corrupted; " +
                "copy the full key again from https://luckypennysoftware.com/account.",
            _ when ContainsAssemblyMismatch(exception) =>
                "The Lucky Penny software license key could not be validated because the Microsoft.IdentityModel " +
                "packages in this application are different versions. Reference the same version of all " +
                "Microsoft.IdentityModel.* packages (for example, by referencing Microsoft.IdentityModel.JsonWebTokens directly).",
            SecurityTokenMalformedException or ArgumentException =>
                "The Lucky Penny software license key appears to be incomplete or corrupted. " +
                "Copy the full key again from https://luckypennysoftware.com/account.",
            SecurityTokenInvalidIssuerException or SecurityTokenInvalidAudienceException =>
                "The Lucky Penny software license key is not valid: it is not a Lucky Penny Software license key. " +
                "Copy your license key from https://luckypennysoftware.com/account.",
            _ =>
                "Error validating the Lucky Penny software license key. " +
                "Copy the full key again from https://luckypennysoftware.com/account. " +
                "If the problem persists, enable debug logging for this category and contact support."
        });
        _logger.LogDebug(exception, "License key validation failure details");
    }

    internal static bool ContainsPlatformNotSupported(Exception? exception) =>
        exception switch
        {
            null => false,
            PlatformNotSupportedException => true,
            AggregateException aggregate => aggregate.InnerExceptions.Any(ContainsPlatformNotSupported),
            _ => ContainsPlatformNotSupported(exception.InnerException)
        };

    // Mixed Microsoft.IdentityModel.* package versions surface as IDX14102 wrapping a MissingMethodException.
    internal static bool ContainsAssemblyMismatch(Exception? exception) =>
        exception switch
        {
            null => false,
            MissingMemberException or TypeLoadException or FileLoadException => true,
            AggregateException aggregate => aggregate.InnerExceptions.Any(ContainsAssemblyMismatch),
            _ => ContainsAssemblyMismatch(exception.InnerException)
        };

    private void LogPlatformNotSupported() =>
        _logger.LogInformation(
            "RSA cryptography is not supported on this platform. " +
            "For client redistribution scenarios such as Blazor WASM, see: " +
            "https://github.com/LuckyPennySoftware/MediatR/wiki/License-Key-Configuration#client-redistribution-scenarios");
}
