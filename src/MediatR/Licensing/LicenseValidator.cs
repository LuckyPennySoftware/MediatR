using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MediatR.Licensing;

internal class LicenseValidator
{
    // Grace period for keys issued without a term_end claim, measured from their exp (their term end).
    internal const int DefaultGracePeriodDays = 30;

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

        var today = DateTime.UtcNow.Date;
        // Keys without a term_end claim predate the grace period: their exp is the term end, so the grace
        // runs from there. Keys with one carry the grace in exp itself (term end + grace).
        var termEnd = (license.TermEndDate ?? license.ExpirationDate!.Value).Date;
        var graceEnd = license.TermEndDate.HasValue
            ? license.ExpirationDate!.Value.Date
            : termEnd.AddDays(DefaultGracePeriodDays);
        string? warning = null;

        // Past the end of the term: within the grace period, a warning (the renewed key may not be
        // deployed yet); past it, an error.
        void Expired(double expiredDaysAgo)
        {
            if (today <= graceEnd)
            {
                warning = $"Your license term for the Lucky Penny software MediatR ended {expiredDaysAgo} days ago. " +
                          $"Update your license key by {graceEnd:yyyy-MM-dd} ({graceEnd.Subtract(today).TotalDays} days remaining) to avoid license errors. " +
                          "If you have renewed, your renewed license key is available at https://luckypennysoftware.com/account.";
            }
            else
            {
                errors.Add($"Your license for the Lucky Penny software MediatR expired {expiredDaysAgo} days ago.");
            }
        }

        var diff = today.Subtract(termEnd).TotalDays;
        if (diff > 0)
        {
            // If perpetual, check if build date is before the end of the term
            if (license.IsPerpetual && _buildDate.HasValue)
            {
                var buildDateDiff = _buildDate.Value.Date.Subtract(termEnd).TotalDays;
                if (buildDateDiff <= 0)
                {
                    _logger.LogInformation(
                        "Your license for the Lucky Penny software MediatR expired {expiredDaysAgo} days ago, but perpetual licensing is active because the build date ({buildDate:O}) is before the license expiration date ({licenseExpiration:O}).",
                        diff, _buildDate, termEnd);
                    // Don't add to errors - perpetual fallback applies
                }
                else
                {
                    Expired(diff);
                }
            }
            else
            {
                if (license.IsPerpetual)
                {
                    _logger.LogWarning(
                        "Your license for the Lucky Penny software MediatR has perpetual licensing enabled, but the build date could not be determined. Perpetual licensing cannot be applied. Please ensure the assembly metadata is correctly embedded at build time.");
                }
                Expired(diff);
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
        else if (warning != null)
        {
            _logger.LogWarning(warning);
        }
        else
        {
            _logger.LogInformation("You have a valid license key for the Lucky Penny software {type} {edition} edition. The license expires on {licenseExpiration}.",
                license.ProductType,
                license.Edition,
                termEnd);
        }
    }
}