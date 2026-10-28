using System;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Xunit;

namespace MediatR.Tests.Licensing;

[CollectionDefinition(nameof(LicenseCryptoProviderIsolationCollection), DisableParallelization = true)]
public class LicenseCryptoProviderIsolationCollection {}

// Replaces the process-global CryptoProviderFactory.Default, so it must not run alongside
// other tests that validate tokens. Disable parallelization via the collection above.
[Collection(nameof(LicenseCryptoProviderIsolationCollection))]
public class LicenseCryptoProviderIsolationTests
{
    [Fact]
    public void Validation_DoesNotUseHostAppCryptoProviderFactory()
    {
        // A failure inside the host app's crypto provider is reported by IdentityModel as an IDX10511
        // signature mismatch, indistinguishable from a corrupted key, so validation must not use it.
        var hostProvider = new RecordingCryptoProvider();
        var original = CryptoProviderFactory.Default;
        try
        {
            CryptoProviderFactory.Default = new CryptoProviderFactory { CustomCryptoProvider = hostProvider };

            LicenseAccessorTests.Validate(LicenseAccessorTests.WellFormedKey);
        }
        finally
        {
            CryptoProviderFactory.Default = original;
        }

        hostProvider.WasConsulted.ShouldBeFalse();
    }

    private sealed class RecordingCryptoProvider : ICryptoProvider
    {
        public bool WasConsulted { get; private set; }

        public bool IsSupportedAlgorithm(string algorithm, params object[] args)
        {
            WasConsulted = true;
            return false;
        }

        public object Create(string algorithm, params object[] args) => throw new NotSupportedException();

        public void Release(object cryptoInstance) { }
    }
}
