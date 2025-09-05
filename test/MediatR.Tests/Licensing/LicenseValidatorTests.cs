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