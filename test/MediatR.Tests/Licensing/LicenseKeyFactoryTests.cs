using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediatR.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;

namespace MediatR.Tests.Licensing;

// Shares the non-parallel ServiceFactory collection because it mutates the static
// MediatRServiceCollectionExtensions.LicenseChecked flag.
[Collection(nameof(ServiceFactoryCollectionBehavior))]
public class LicenseKeyFactoryTests
{
    // Malformed keys log an Error, and well-formed keys with a bad signature log a Critical,
    // which shows which key was used without needing a genuine license.
    private const string MalformedKey = "not-a-real-license-key";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public interface ILicenseKeyProvider
    {
        string? GetMediatRLicense();
    }

    private sealed class LicenseKeyProvider(string? key) : ILicenseKeyProvider
    {
        public string? GetMediatRLicense() => key;
    }

    [Fact]
    public void Should_resolve_the_key_from_a_service_in_the_container()
    {
        var logs = ReadLicense(new MediatRServiceConfiguration
        {
            LicenseKeyFactory = sp => sp.GetRequiredService<ILicenseKeyProvider>().GetMediatRLicense()
        }, MalformedKey);

        logs.ShouldHaveSingleItem().Message.ShouldContain("incomplete or corrupted");
    }

    [Fact]
    public void Should_take_precedence_over_the_configured_key()
    {
        var logs = ReadLicense(new MediatRServiceConfiguration
        {
            LicenseKey = LicenseAccessorTests.WellFormedKey,
            LicenseKeyFactory = _ => MalformedKey
        });

        var log = logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Error);
        log.Message.ShouldContain("incomplete or corrupted");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n")]
    public void Should_fall_back_to_the_configured_key_when_the_result_is_blank(string? factoryKey)
    {
        var factoryCalls = 0;

        var logs = ReadLicense(new MediatRServiceConfiguration
        {
            LicenseKey = MalformedKey,
            LicenseKeyFactory = _ =>
            {
                factoryCalls++;
                return factoryKey;
            }
        });

        factoryCalls.ShouldBe(1);
        logs.ShouldHaveSingleItem().Message.ShouldContain("incomplete or corrupted");
    }

    [Fact]
    public void Should_strip_whitespace_from_the_result()
    {
        var logs = ReadLicense(new MediatRServiceConfiguration
        {
            LicenseKeyFactory = _ => LicenseAccessorTests.WellFormedKey + "\r\n"
        });

        // Reaching signature validation means the key was still well formed after normalization.
        logs.ShouldContain(log => log.Level == LogLevel.Critical && log.Message.Contains("signature does not match"));
    }

    // The factory may read a secret store. It must not run during registration, or on the Mediator
    // construction path, where a lazily-built DI singleton holds the container's build lock
    // (AutoMapper #4640). Blocking the factory until construction returns proves it doesn't.
    [Fact]
    public void Should_run_off_the_construction_thread_without_blocking_it()
    {
        using var factoryStarted = new ManualResetEventSlim(false);
        using var constructionReturned = new ManualResetEventSlim(false);
        using var factoryFinished = new ManualResetEventSlim(false);
        var factoryThreadId = 0;
        var constructionReturnedFirst = false;
        var factoryCalled = false;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Mediator).Assembly);
            cfg.LicenseKeyFactory = _ =>
            {
                factoryCalled = true;
                factoryThreadId = Environment.CurrentManagedThreadId;
                factoryStarted.Set();
                constructionReturnedFirst = constructionReturned.Wait(Timeout);
                factoryFinished.Set();
                return null;
            };
        });

        using var container = services.BuildServiceProvider();

        factoryCalled.ShouldBeFalse();

        MediatRServiceCollectionExtensions.LicenseChecked = false;
        _ = container.GetRequiredService<IMediator>();
        constructionReturned.Set();

        factoryStarted.Wait(Timeout).ShouldBeTrue();
        factoryFinished.Wait(Timeout).ShouldBeTrue();
        constructionReturnedFirst.ShouldBeTrue();
        factoryThreadId.ShouldNotBe(Environment.CurrentManagedThreadId);
    }

    [Fact]
    public void Should_log_an_exception_from_the_factory_and_still_create_the_mediator()
    {
        var loggerProvider = new SignalingLoggerProvider();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerProvider>(loggerProvider);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Mediator).Assembly);
            cfg.LicenseKeyFactory = _ => throw new InvalidOperationException("secret store unavailable");
        });

        using var container = services.BuildServiceProvider();

        MediatRServiceCollectionExtensions.LicenseChecked = false;
        container.GetRequiredService<IMediator>().ShouldNotBeNull();

        loggerProvider.LicenseLogged.Wait(Timeout).ShouldBeTrue();
        loggerProvider.Level.ShouldBe(LogLevel.Error);
        loggerProvider.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    private static IReadOnlyList<FakeLogRecord> ReadLicense(MediatRServiceConfiguration configuration, string? providerKey = null)
    {
        var loggerProvider = new FakeLoggerProvider();
        using var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(loggerProvider);

        using var container = new ServiceCollection()
            .AddSingleton<ILicenseKeyProvider>(new LicenseKeyProvider(providerKey))
            .BuildServiceProvider();

        _ = new LicenseAccessor(configuration, container, loggerFactory).Current;

        return loggerProvider.Collector.GetSnapshot()
            .Where(log => log.Level > LogLevel.Debug)
            .ToArray();
    }

    private sealed class SignalingLoggerProvider : ILoggerProvider, ILogger
    {
        public readonly ManualResetEventSlim LicenseLogged = new(false);
        public LogLevel Level;
        public Exception? Exception;

        public ILogger CreateLogger(string categoryName) =>
            categoryName == "LuckyPennySoftware.MediatR.License" ? this : NullLogger.Instance;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Exception = exception;
            LicenseLogged.Set();
        }

        public void Dispose() => LicenseLogged.Dispose();
    }
}
