using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MediatR.Licensing;

internal class LicenseValidator
{
    private readonly ILogger _logger;
    private readonly DateTimeOffset? _buildDate;

    public LicenseValidator(ILoggerFactory loggerFactory) : this(loggerFactory, BuildInfo.BuildDate)
    {
    }

    public LicenseValidator(ILoggerFactory loggerFactory, DateTimeOffset? buildDate)
    {
        _logger = loggerFactory.CreateLogger("LuckyPennySoftware.MediatR.License");
        _buildDate = buildDate;
    }

    public void Validate(License license)
    {
        var errors = new List<string>();

        if (license is not { IsConfigured: true })
        {
            var message = "You do not have a valid license key for the Lucky Penny software MediatR. " +
                          "This is allowed for development and testing scenarios. " +
                          "If you are running in production you are required to have a licensed version. " +
                          "Please visit https://luckypennysoftware.com to obtain a valid license.";

            _logger.LogWarning(message);
            return;
        }

        _logger.LogDebug("The Lucky Penny license key details: {license}", license);

        var diff = DateTime.UtcNow.Date.Subtract(license.ExpirationDate!.Value.Date).TotalDays;
        if (diff > 0)
        {
            // If perpetual, check if build date is before expiration
            if (license.IsPerpetual && _buildDate.HasValue)
            {
                var buildDateDiff = _buildDate.Value.Date.Subtract(license.ExpirationDate.Value.Date).TotalDays;
                if (buildDateDiff <= 0)
                {
                    _logger.LogInformation(
                        "Your license for the Lucky Penny software MediatR expired {expiredDaysAgo} days ago, but perpetual licensing is active because the build date ({buildDate:O}) is before the license expiration date ({licenseExpiration:O}).",
                        diff, _buildDate, license.ExpirationDate);
                    // Don't add to errors - perpetual fallback applies
                }
                else
                {
                    errors.Add($"Your license for the Lucky Penny software MediatR expired {diff} days ago.");
                }
            }
            else
            {
                if (license.IsPerpetual)
                {
                    _logger.LogWarning(
                        "Your license for the Lucky Penny software MediatR has perpetual licensing enabled, but the build date could not be determined. Perpetual licensing cannot be applied. Please ensure the assembly metadata is correctly embedded at build time.");
                }
                errors.Add($"Your license for the Lucky Penny software MediatR expired {diff} days ago.");
            }
        }

        if (license.ProductType!.Value != ProductType.MediatR
            && license.ProductType.Value != ProductType.Bundle)
        {
            errors.Add("Your Lucky Penny software license does not include MediatR.");
        }

        if (errors.Count > 0)
        {
            foreach (var err in errors)
            {
                _logger.LogError(err);
            }

            _logger.LogError(
                "Please visit https://luckypennysoftware.com to obtain a valid license for the Lucky Penny software MediatR.");
        }
        else
        {
            _logger.LogInformation("You have a valid license key for the Lucky Penny software {type} {edition} edition. The license expires on {licenseExpiration}.",
                license.ProductType,
                license.Edition,
                license.ExpirationDate);
        }
    }
}