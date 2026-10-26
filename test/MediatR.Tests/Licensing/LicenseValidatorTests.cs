using System;
using System.Collections.Generic;
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
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeSeconds().ToString()),
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
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-40).ToUnixTimeSeconds().ToString()),
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

        var licenseValidator = new LicenseValidator(factory, (DateTimeOffset?)null);
        var license = new License(
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("customer_id", Guid.NewGuid().ToString()),
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddDays(-31).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(Edition.Community)),
            new Claim("type", nameof(ProductType.Bundle)),
            new Claim("perpetual", "true"));

        license.IsConfigured.ShouldBeTrue();
        license.IsPerpetual.ShouldBeTrue();

        // Validator was created with null buildDate - perpetual licensing cannot be applied.
        licenseValidator.Validate(license);

        var logMessages = provider.Collector.GetSnapshot();
        logMessages.ShouldContain(log => log.Level == LogLevel.Warning && log.Message.Contains("perpetual"));
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

    private static License TermLicense(int termEndDaysFromNow, int? expDaysFromNow, bool perpetual = false)
    {
        var claims = new List<Claim>
        {
            new("account_id", Guid.NewGuid().ToString()),
            new("customer_id", Guid.NewGuid().ToString()),
            new("sub_id", Guid.NewGuid().ToString()),
            new("iat", DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds().ToString()),
            new("edition", nameof(Edition.Professional)),
            new("type", nameof(ProductType.MediatR)),
        };
        if (expDaysFromNow is { } exp)
        {
            // A key issued with a grace period: exp is the term end + grace, term_end the real term end.
            claims.Add(new("exp", DateTimeOffset.UtcNow.AddDays(exp).ToUnixTimeSeconds().ToString()));
            claims.Add(new("term_end", DateTimeOffset.UtcNow.AddDays(termEndDaysFromNow).ToUnixTimeSeconds().ToString()));
        }
        else
        {
            // A key issued before the grace period: exp is the term end, no term_end claim.
            claims.Add(new("exp", DateTimeOffset.UtcNow.AddDays(termEndDaysFromNow).ToUnixTimeSeconds().ToString()));
        }
        if (perpetual)
            claims.Add(new("perpetual", "true"));
        return new License(claims.ToArray());
    }

    private static IReadOnlyList<FakeLogRecord> Validate(License license, DateTimeOffset? buildDate = null)
    {
        var factory = new LoggerFactory();
        var provider = new FakeLoggerProvider();
        factory.AddProvider(provider);

        license.IsConfigured.ShouldBeTrue();
        new LicenseValidator(factory, buildDate).Validate(license);

        return provider.Collector.GetSnapshot();
    }

    [Fact]
    public void Should_warn_within_grace_period_after_term_end()
    {
        var logMessages = Validate(TermLicense(termEndDaysFromNow: -5, expDaysFromNow: 25));

        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error);
        logMessages.ShouldContain(log => log.Level == LogLevel.Warning && log.Message.Contains("ended 5 days ago"));
    }

    [Fact]
    public void Should_error_after_grace_period()
    {
        var logMessages = Validate(TermLicense(termEndDaysFromNow: -31, expDaysFromNow: -1));

        logMessages.ShouldContain(log => log.Level == LogLevel.Error && log.Message.Contains("expired 31 days ago"));
    }

    [Fact]
    public void Should_be_valid_before_term_end()
    {
        var logMessages = Validate(TermLicense(termEndDaysFromNow: 10, expDaysFromNow: 40));

        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error || log.Level == LogLevel.Warning);
        logMessages.ShouldContain(log => log.Level == LogLevel.Information);
    }

    [Fact]
    public void Should_warn_within_default_grace_period_for_keys_without_term_end()
    {
        var logMessages = Validate(TermLicense(termEndDaysFromNow: -10, expDaysFromNow: null));

        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error);
        logMessages.ShouldContain(log => log.Level == LogLevel.Warning && log.Message.Contains("ended 10 days ago"));
    }

    [Fact]
    public void Should_use_term_end_for_perpetual_cutoff()
    {
        // Built during the grace period: after the term end, so perpetual fallback does not apply.
        var logMessages = Validate(
            TermLicense(termEndDaysFromNow: -40, expDaysFromNow: -10, perpetual: true),
            buildDate: DateTimeOffset.UtcNow.AddDays(-20));

        logMessages.ShouldContain(log => log.Level == LogLevel.Error && log.Message.Contains("expired"));
        logMessages.ShouldNotContain(log => log.Message.Contains("perpetual licensing is active"));
    }

    [Fact]
    public void Should_allow_perpetual_when_built_before_term_end()
    {
        var logMessages = Validate(
            TermLicense(termEndDaysFromNow: -40, expDaysFromNow: -10, perpetual: true),
            buildDate: DateTimeOffset.UtcNow.AddDays(-50));

        logMessages.ShouldNotContain(log => log.Level == LogLevel.Error || log.Level == LogLevel.Warning);
        logMessages.ShouldContain(log => log.Level == LogLevel.Information && log.Message.Contains("perpetual"));
    }
}
