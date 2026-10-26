using System;
using MediatR.Licensing;
using Shouldly;
using Xunit;

namespace MediatR.Tests.Licensing;

[CollectionDefinition(nameof(LicenseKeyEnvironmentVariableCollection), DisableParallelization = true)]
public class LicenseKeyEnvironmentVariableCollection {}

// Mutates process-global environment variables, so it must not run alongside
// other tests that read them. Disable parallelization via the collection above.
[Collection(nameof(LicenseKeyEnvironmentVariableCollection))]
public class LicenseKeyEnvironmentVariableTests
{
    private const string MediatREnvVar = LicenseAccessor.MediatRLicenseKeyEnvVariable;
    private const string SharedEnvVar = LicenseAccessor.SharedLicenseKeyEnvVariable;
    private const string CleanKey = "header.pay-load.sign_ature";

    public static TheoryData<string> KeysWithWhitespace => new()
    {
        CleanKey + "\n",
        CleanKey + "\r\n",
        CleanKey + " ",
        CleanKey + "\t",
        " \t" + CleanKey,
        "header.\r\npay-\nload.sign_\tature",
        "\u00a0header.\u2003pay-load.sign_ature\u2028"
    };

    [Theory]
    [MemberData(nameof(KeysWithWhitespace))]
    public void ConfigurationKey_StripsWhitespace_AndTakesPrecedence(string key)
    {
        WithEnvironment(mediatR: "env-mediatr-key", shared: "env-shared-key", () =>
            LicenseAccessor.ResolveLicenseKey(key, "static-license-key").ShouldBe(CleanKey));
    }

    [Theory]
    [MemberData(nameof(KeysWithWhitespace))]
    public void StaticKey_StripsWhitespace(string key)
    {
        WithEnvironment(mediatR: "env-mediatr-key", shared: "env-shared-key", () =>
            LicenseAccessor.ResolveLicenseKey(null, key).ShouldBe(CleanKey));
    }

    [Theory]
    [MemberData(nameof(KeysWithWhitespace))]
    public void MediatREnvironmentVariable_StripsWhitespace_AndTakesPrecedence(string key)
    {
        WithEnvironment(mediatR: key, shared: "env-shared-key", () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBe(CleanKey));
    }

    [Theory]
    [MemberData(nameof(KeysWithWhitespace))]
    public void SharedEnvironmentVariable_StripsWhitespace(string key)
    {
        WithEnvironment(mediatR: null, shared: key, () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBe(CleanKey));
    }

    [Theory]
    [InlineData("\t\r\n")]
    [InlineData("\u00a0\u2003\u2028")]
    public void BlankKeys_FallBackToSharedEnvironmentVariable(string key)
    {
        WithEnvironment(mediatR: key, shared: " env-shared-key\n", () =>
            LicenseAccessor.ResolveLicenseKey(key, key).ShouldBe("env-shared-key"));
    }

    [Theory]
    [InlineData("\t\r\n")]
    [InlineData("\u00a0\u2003\u2028")]
    public void ReturnsNull_WhenAllKeysAreBlank(string key)
    {
        WithEnvironment(mediatR: key, shared: key, () =>
            LicenseAccessor.ResolveLicenseKey(key, key).ShouldBeNull());
    }

    [Fact]
    public void ExplicitKey_TakesPrecedence_OverBothEnvironmentVariables()
    {
        const string explicitKey = "explicit-license-key";
        WithEnvironment(mediatR: "env-mediatr-key", shared: "env-shared-key", () =>
            LicenseAccessor.ResolveLicenseKey(explicitKey).ShouldBe(explicitKey));
    }

    [Fact]
    public void ConfigurationKey_TakesPrecedence_OverStaticKey()
    {
        const string configKey = "config-license-key";
        WithEnvironment(mediatR: null, shared: null, () =>
            LicenseAccessor.ResolveLicenseKey(configKey, "static-license-key").ShouldBe(configKey));
    }

    [Fact]
    public void StaticKey_Used_WhenConfigurationKeyIsBlank()
    {
        const string staticKey = "static-license-key";
        WithEnvironment(mediatR: null, shared: null, () =>
            LicenseAccessor.ResolveLicenseKey("   ", staticKey).ShouldBe(staticKey));
    }

    [Fact]
    public void MediatREnvironmentVariable_Used_WhenNoExplicitKey()
    {
        const string mediatRKey = "env-mediatr-key";
        WithEnvironment(mediatR: mediatRKey, shared: null, () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBe(mediatRKey));
    }

    [Fact]
    public void SharedEnvironmentVariable_Used_WhenOnlyItIsSet()
    {
        const string sharedKey = "env-shared-key";
        WithEnvironment(mediatR: null, shared: sharedKey, () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBe(sharedKey));
    }

    [Fact]
    public void MediatREnvironmentVariable_TakesPrecedence_OverSharedEnvironmentVariable()
    {
        const string mediatRKey = "env-mediatr-key";
        WithEnvironment(mediatR: mediatRKey, shared: "env-shared-key", () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBe(mediatRKey));
    }

    [Fact]
    public void EnvironmentVariable_Used_WhenExplicitKeysAreBlank()
    {
        const string mediatRKey = "env-mediatr-key";
        WithEnvironment(mediatR: mediatRKey, shared: null, () =>
            LicenseAccessor.ResolveLicenseKey("", "   ").ShouldBe(mediatRKey));
    }

    [Fact]
    public void ReturnsNull_WhenNothingIsSet()
    {
        WithEnvironment(mediatR: null, shared: null, () =>
            LicenseAccessor.ResolveLicenseKey(null, null).ShouldBeNull());
    }

    private static void WithEnvironment(string? mediatR, string? shared, Action assert)
    {
        var originalMediatR = Environment.GetEnvironmentVariable(MediatREnvVar);
        var originalShared = Environment.GetEnvironmentVariable(SharedEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MediatREnvVar, mediatR);
            Environment.SetEnvironmentVariable(SharedEnvVar, shared);
            assert();
        }
        finally
        {
            Environment.SetEnvironmentVariable(MediatREnvVar, originalMediatR);
            Environment.SetEnvironmentVariable(SharedEnvVar, originalShared);
        }
    }
}
