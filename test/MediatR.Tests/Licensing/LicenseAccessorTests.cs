using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediatR.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

namespace MediatR.Tests.Licensing;

public class LicenseAccessorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Should_return_unconfigured_license_for_missing_or_blank_key(string? key)
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var config = new MediatRServiceConfiguration { LicenseKey = key };
        var accessor = new LicenseAccessor(config, factory);

        var license = accessor.Current;

        license.IsConfigured.ShouldBeFalse();

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldNotContain(log => log.Level == LogLevel.Critical);
    }

    [Fact]
    public void Should_return_unconfigured_license_when_static_key_is_blank()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var original = Mediator.LicenseKey;
        try
        {
            Mediator.LicenseKey = "   ";
            var accessor = new LicenseAccessor(factory);

            var license = accessor.Current;

            license.IsConfigured.ShouldBeFalse();

            var logMessages = provider.Collector.GetSnapshot();
            logMessages.ShouldNotContain(log => log.Level == LogLevel.Critical);
        }
        finally
        {
            Mediator.LicenseKey = original;
        }
    }

    // Structurally valid compact JWS (correct issuer, audience and kid, 342-character RS256 signature)
    // whose signature is not genuine, so it reaches signature validation and fails there.
    internal static readonly string WellFormedKey = string.Join(".",
        Base64Url("""{"alg":"RS256","kid":"LuckyPennySoftwareLicenseKey/bbb13acb59904d89b4cb1c85f088ccf9","typ":"JWT"}"""),
        Base64Url("""{"iss":"https://luckypennysoftware.com","aud":"LuckyPennySoftware"}"""),
        new string('A', 342));

    [Fact]
    public void WellFormedKey_PassesStructuralCheck() =>
        LicenseAccessor.IsWellFormed(WellFormedKey).ShouldBeTrue();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void TruncatedKey_LogsIncompleteMessage(int missingCharacters)
    {
        var truncatedKey = WellFormedKey.Substring(0, WellFormedKey.Length - missingCharacters);

        var logs = Validate(truncatedKey);

        ShouldLogIncompleteKey(logs, truncatedKey);
    }

    [Theory]
    [InlineData("header.payload")]
    [InlineData("header.payload.signature.extra")]
    [InlineData("not-a-real-license-key")]
    public void WrongSegmentCount_LogsIncompleteMessage(string key)
    {
        var logs = Validate(key);

        ShouldLogIncompleteKey(logs, key);
    }

    [Fact]
    public void NonBase64UrlCharacters_LogIncompleteMessage()
    {
        var key = WellFormedKey.Substring(0, WellFormedKey.Length - 1) + "=";

        var logs = Validate(key);

        ShouldLogIncompleteKey(logs, key);
    }

    [Fact]
    public void InvalidSignature_LogsCopyPasteHint_WithoutExceptionDetails()
    {
        var logs = Validate(WellFormedKey);

        ShouldLogPlainCritical(logs, "copy the full key again");
        ShouldLogDetailsAtDebug<SecurityTokenInvalidSignatureException>(logs);
    }

    [Fact]
    public void UnparseableToken_LogsIncompleteMessage_WithoutExceptionDetails()
    {
        var key = string.Join(".", "bm90LWpzb24", "bm90LWpzb24", new string('A', 342));

        var logs = Validate(key);

        ShouldLogPlainCritical(logs, "incomplete or corrupted");
        // IdentityModel reports an undecodable header as IDX14102, an ArgumentException.
        ShouldLogDetailsAtDebug<ArgumentException>(logs);
    }

    [Fact]
    public void NestedPlatformNotSupportedException_LogsClientRedistributionInfo()
    {
        var exception = new SecurityTokenInvalidSignatureException("IDX10511",
            new InvalidOperationException("wrapper", new PlatformNotSupportedException()));

        var logs = WithAccessor(accessor =>
        {
            accessor.LogValidationFailure(exception);
            accessor.IsValidationUnsupported.ShouldBeTrue();
        });

        ShouldLogClientRedistribution(logs);
    }

    [Fact]
    public void AggregatedPlatformNotSupportedException_LogsClientRedistributionInfo()
    {
        var exception = new SecurityTokenInvalidSignatureException("IDX10511",
            new AggregateException(new InvalidOperationException(), new PlatformNotSupportedException()));

        var logs = LogValidationFailure(exception);

        ShouldLogClientRedistribution(logs);
    }

    [Fact]
    public void MismatchedIdentityModelVersions_LogsPackageVersionMessage_WithoutExceptionDetails()
    {
        // The shape seen in support: IDX14102 wrapping a MissingMethodException from mixed package versions.
        var exception = new ArgumentException("IDX14102",
            new MissingMethodException("Method not found: 'Void Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode'"));

        var logs = LogValidationFailure(exception);

        ShouldLogPlainCritical(logs, "Microsoft.IdentityModel");
        ShouldLogDetailsAtDebug<ArgumentException>(logs);
    }

    [Fact]
    public void WrongIssuer_LogsNotALuckyPennyKey_WithoutExceptionDetails()
    {
        var logs = LogValidationFailure(new SecurityTokenInvalidIssuerException("IDX10205"));

        ShouldLogPlainCritical(logs, "not a Lucky Penny Software license key");
        ShouldLogDetailsAtDebug<SecurityTokenInvalidIssuerException>(logs);
    }

    [Fact]
    public void WrongAudience_LogsNotALuckyPennyKey_WithoutExceptionDetails()
    {
        var logs = LogValidationFailure(new SecurityTokenInvalidAudienceException("IDX10214"));

        ShouldLogPlainCritical(logs, "not a Lucky Penny Software license key");
        ShouldLogDetailsAtDebug<SecurityTokenInvalidAudienceException>(logs);
    }

    [Fact]
    public void OtherValidationFailure_LogsGenericMessage_WithoutExceptionDetails()
    {
        var logs = LogValidationFailure(new SecurityTokenException("IDX10000"));

        ShouldLogPlainCritical(logs, "contact support");
        ShouldLogDetailsAtDebug<SecurityTokenException>(logs);
    }

    private static void ShouldLogPlainCritical(IReadOnlyList<FakeLogRecord> logs, string expected)
    {
        var critical = logs.Where(log => log.Level == LogLevel.Critical).ShouldHaveSingleItem();
        critical.Exception.ShouldBeNull();
        var details = logs.Where(log => log.Level == LogLevel.Debug).Select(log => log.Exception?.ToString()).FirstOrDefault();
        critical.Message.ShouldContain(expected, customMessage: "Validation failure details: " + details);
        critical.Message.ShouldNotContain("IDX");
        logs.ShouldAllBe(log => log.Level == LogLevel.Critical || log.Level == LogLevel.Debug);
    }

    private static void ShouldLogDetailsAtDebug<TException>(IReadOnlyList<FakeLogRecord> logs) where TException : Exception =>
        logs.Where(log => log.Level == LogLevel.Debug).ShouldHaveSingleItem().Exception.ShouldBeOfType<TException>();

    internal static IReadOnlyList<FakeLogRecord> Validate(string key) =>
        WithAccessor(accessor => accessor.ValidateKey(key).ShouldBeEmpty());

    private static IReadOnlyList<FakeLogRecord> LogValidationFailure(Exception exception) =>
        WithAccessor(accessor => accessor.LogValidationFailure(exception));

    private static IReadOnlyList<FakeLogRecord> WithAccessor(Action<LicenseAccessor> act)
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        act(new LicenseAccessor(factory));

        return provider.Collector.GetSnapshot();
    }

    private static void ShouldLogIncompleteKey(IReadOnlyList<FakeLogRecord> logs, string key)
    {
        var log = logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Error);
        log.Exception.ShouldBeNull();
        log.Message.ShouldContain("incomplete or corrupted");
        log.Message.ShouldNotContain(key);
    }

    private static void ShouldLogClientRedistribution(IReadOnlyList<FakeLogRecord> logs)
    {
        var log = logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Information);
        log.Message.ShouldContain(LicenseAccessor.ClientPlatformsLink);
    }

    private static string Base64Url(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
