using System;
using System.Security.Claims;
using MediatR.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;
using License = MediatR.Licensing.License;

namespace MediatR.Tests.Licensing;

public class LicenseValidatorTests
{
    [Fact]
    public void Should_return_invalid_when_no_claims()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var licenseValidator = new LicenseValidator(factory);
        var license = new License();
        
        license.IsConfigured.ShouldBeFalse();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages
            .ShouldContain(log => log.Level == LogLevel.Warning);
    }   
    
        
    [Fact]
    public void Should_return_valid_when_community()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var licenseValidator = new LicenseValidator(factory);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString()), 
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Community)),
            new Claim("type", nameof(ProductType.Bundle)));
        
        license.IsConfigured.ShouldBeTrue();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error 
                                            || log.Level == LogLevel.Warning
                                            || log.Level == LogLevel.Critical);
    }
    
    [Fact]
    public void Should_return_invalid_when_not_correct_type()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var licenseValidator = new LicenseValidator(factory);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString()), 
            new Claim("exp", DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Professional)),
            new Claim("type", nameof(ProductType.AutoMapper)));
        
        license.IsConfigured.ShouldBeTrue();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages
            .ShouldContain(log => log.Level == LogLevel.Error);
    }
    
    [Fact]
    public void Should_return_invalid_when_expired()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var licenseValidator = new LicenseValidator(factory);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds().ToString()), 
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Professional)),
            new Claim("type", nameof(ProductType.MediatR)));
        
        license.IsConfigured.ShouldBeTrue();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages
            .ShouldContain(log => log.Level == LogLevel.Error);
    }
    
    [Fact]
    public void Should_return_valid_for_actual_valid_license()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var config = new MediatRServiceConfiguration
        {
            LicenseKey =
                "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzgxNTg2MDAwIiwiaWF0IjoiMTc1MDEwNDUyMiIsImFjY291bnRfaWQiOiJmMzQ4N2NhOWE5MDE0NWRlYmE4NGY4NDkwNDgxNWQ3NiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxanhhcTVkcHNleHFmZmF0eDhkd3Ntd3Y2IiwiY29tcGFueSI6Ik15IFRlc3QgQ29tcGFueSIsInN1Yl9pZCI6InN1Yl8wMWp4eDVxZ3BnbTF0NDBhdDh2cGQzbm0zaCIsImVkaXRpb24iOiIyIiwidHlwZSI6IjEifQ.W-0ScVg5GxZ6R2ZcZfz8z5nnVAhEcMggnFLvyifm15ox9gei6xm6W4Wo1_RC75XqLzWyDqGp2lvgxucJqCDy3EpasDLADjyfRpqt14nZ81BnbjYgufERbfBRlX8i8O4ZfGg0BNb_nFNIP0XKuww4GGJ854HZOJds0CI31CH4JaghQkUSTaDaxGcrqb7K9RiWR90OhdkiUPBHk1p-EO2nogVFNothozEWKgCgVocvi9MguQBlJDC_e5Rg7c9XZdzTCkzwJAXAVdjoXaOvkPxTVSH09eOALuUXhi-FtKRzGVvVbqFVdEmiUDSPSs2ULeWz8GlfC1V33Wz2f3y69Lr9KA"
        };
        var licenseAccessor = new LicenseAccessor(config, factory);

        var licenseValidator = new LicenseValidator(factory);
        var license = licenseAccessor.Current;
        
        license.IsConfigured.ShouldBeTrue();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages
            .ShouldNotContain(log => log.Level == LogLevel.Error);
    }
}