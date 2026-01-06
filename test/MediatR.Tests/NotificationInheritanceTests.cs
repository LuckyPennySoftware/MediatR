using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediatR.Tests;

public class NotificationInheritanceTests
{
    // Base notification
    public class BaseNotification : INotification
    {
        public string Message { get; set; } = "";
    }

    // Derived notification
    public class DerivedNotification : BaseNotification
    {
        public string AdditionalInfo { get; set; } = "";
    }

    // Handler for base notification
    public class BaseNotificationHandler : INotificationHandler<BaseNotification>
    {
        public static int CallCount { get; set; }
        public static List<string> ReceivedTypes { get; set; } = new();

        public Task Handle(BaseNotification notification, CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedTypes.Add(notification.GetType().Name);
            return Task.CompletedTask;
        }
    }

    // Handler for derived notification
    public class DerivedNotificationHandler : INotificationHandler<DerivedNotification>
    {
        public static int CallCount { get; set; }
        public static List<string> ReceivedTypes { get; set; } = new();

        public Task Handle(DerivedNotification notification, CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedTypes.Add(notification.GetType().Name);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_Call_BaseHandler_Once_For_BaseNotification()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NotificationInheritanceTests).Assembly));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        BaseNotificationHandler.CallCount = 0;
        BaseNotificationHandler.ReceivedTypes.Clear();
        DerivedNotificationHandler.CallCount = 0;
        DerivedNotificationHandler.ReceivedTypes.Clear();

        // Act
        await mediator.Publish(new BaseNotification { Message = "Test" });

        // Assert
        BaseNotificationHandler.CallCount.ShouldBe(1);
        DerivedNotificationHandler.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Call_BaseHandler_Once_And_DerivedHandler_Once_For_DerivedNotification()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NotificationInheritanceTests).Assembly));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        BaseNotificationHandler.CallCount = 0;
        BaseNotificationHandler.ReceivedTypes.Clear();
        DerivedNotificationHandler.CallCount = 0;
        DerivedNotificationHandler.ReceivedTypes.Clear();

        // Act
        await mediator.Publish(new DerivedNotification { Message = "Test", AdditionalInfo = "Extra" });

        // Assert
        // Base handler should be called once due to contravariance (this is expected behavior)
        BaseNotificationHandler.CallCount.ShouldBe(1);
        // Derived handler should be called once
        DerivedNotificationHandler.CallCount.ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Register_Handler_Twice_For_Same_Notification_Type()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NotificationInheritanceTests).Assembly));

        // Act
        var baseHandlers = services.Where(sd => 
            sd.ServiceType == typeof(INotificationHandler<BaseNotification>) &&
            sd.ImplementationType == typeof(BaseNotificationHandler)).ToList();

        var derivedHandlers = services.Where(sd => 
            sd.ServiceType == typeof(INotificationHandler<DerivedNotification>) &&
            sd.ImplementationType == typeof(DerivedNotificationHandler)).ToList();

        var baseHandlerForDerived = services.Where(sd => 
            sd.ServiceType == typeof(INotificationHandler<DerivedNotification>) &&
            sd.ImplementationType == typeof(BaseNotificationHandler)).ToList();

        // Assert
        // BaseNotificationHandler should only be registered for INotificationHandler<BaseNotification>
        baseHandlers.Count.ShouldBe(1);
        
        // DerivedNotificationHandler should only be registered for INotificationHandler<DerivedNotification>
        derivedHandlers.Count.ShouldBe(1);
        
        // BaseNotificationHandler should NOT be registered for INotificationHandler<DerivedNotification>
        // This is the bug we're fixing
        baseHandlerForDerived.Count.ShouldBe(0, 
            "BaseNotificationHandler should not be registered for INotificationHandler<DerivedNotification>");
    }
}
