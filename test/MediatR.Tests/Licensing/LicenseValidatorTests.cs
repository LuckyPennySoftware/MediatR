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
    public void Should_allow_perpetual_license_when_build_date_before_expiration()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var buildDate = DateTimeOffset.UtcNow.AddDays(-30);
        var licenseValidator = new LicenseValidator(factory, buildDate);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Professional)),
            new Claim("type", nameof(ProductType.MediatR)),
            new Claim("perpetual", "true"));

        license.IsConfigured.ShouldBeTrue();
        license.IsPerpetual.ShouldBeTrue();

        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error);
        logMessages.ShouldContain(log => log.Level == LogLevel.Information &&
                                         log.Message.Contains("perpetual"));
    }

    [Fact]
    public void Should_reject_perpetual_license_when_build_date_after_expiration()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var buildDate = DateTimeOffset.UtcNow.AddDays(-5); // Build date in past, after expiration
        var licenseValidator = new LicenseValidator(factory, buildDate);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Professional)),
            new Claim("type", nameof(ProductType.MediatR)),
            new Claim("perpetual", "true"));

        license.IsConfigured.ShouldBeTrue();
        license.IsPerpetual.ShouldBeTrue();

        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldContain(log => log.Level == LogLevel.Error &&
                                        log.Message.Contains("expired"));
    }

    [Fact]
    public void Should_handle_missing_perpetual_claim()
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
        license.IsPerpetual.ShouldBeFalse();

        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error
                                            || log.Level == LogLevel.Warning
                                            || log.Level == LogLevel.Critical);
    }

    [Fact]
    public void Should_fall_back_to_expiration_error_when_perpetual_and_build_date_is_null()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var licenseValidator = new LicenseValidator(factory);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Community)),
            new Claim("type", nameof(ProductType.Bundle)),
            new Claim("perpetual", bool.TrueString));

        license.IsConfigured.ShouldBeTrue();
        license.IsPerpetual.ShouldBeTrue();

        // Pass a null buildDate for a perpetual, but expired, license.
        licenseValidator.Validate(license, null);

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldContain(log => log.Level == LogLevel.Error);
    }
    [Fact(Skip = "Needs license")]
    public void Should_return_valid_for_actual_valid_license()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        var config = new MediatRServiceConfiguration
        {
            LicenseKey =
                "<>"
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
    
    [Fact(Skip = "Needs license")]
    public void Should_return_valid_for_actual_valid_license_via_static_property()
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        Mediator.LicenseKey = "<>";
        var licenseAccessor = new LicenseAccessor(factory);

        var licenseValidator = new LicenseValidator(factory);
        var license = licenseAccessor.Current;
        
        license.IsConfigured.ShouldBeTrue();
        
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
     
        logMessages
            .ShouldNotContain(log => log.Level == LogLevel.Error);
    }
}