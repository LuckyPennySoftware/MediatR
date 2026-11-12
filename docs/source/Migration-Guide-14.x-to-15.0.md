# Migration guide: 14.x to 15.0

[Release notes](https://github.com/LuckyPennySoftware/MediatR/releases/tag/v15.0.0).

## MediatR now targets .NET 10, .NET 11, .NET Standard 2.0 and .NET Framework 4.6.2

The `net8.0` and `net9.0` builds have been removed, because both versions of .NET reach end of support in November 2026. `MediatR.Contracts` still targets `netstandard2.0` only.

Apps on .NET 8 or .NET 9 can still install this package, and they get the `netstandard2.0` build, which has the same public API. We recommend moving to .NET 10 or later.

## Supported versions

Security fixes are provided for the current and previous major versions. With this release, those are 15.x and 14.x, and 13.x is no longer supported.
