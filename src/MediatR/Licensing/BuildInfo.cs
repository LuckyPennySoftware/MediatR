using System;
using System.Linq;
using System.Reflection;

namespace MediatR.Licensing;

internal static class BuildInfo
{
    public static DateTimeOffset? BuildDate { get; } = GetBuildDate();

    private static DateTimeOffset? GetBuildDate()
    {
        var assembly = typeof(BuildInfo).Assembly;

        var buildDateAttribute = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDateUtc");

        if (buildDateAttribute?.Value != null &&
            DateTimeOffset.TryParse(buildDateAttribute.Value, out var buildDate))
        {
            return buildDate;
        }

        return null;
    }
}
