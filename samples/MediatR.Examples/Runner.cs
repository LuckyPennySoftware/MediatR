using System;
using System.Linq;
using System.Text;
using Deluxe.MediatR;
using MediatR.Examples.Streams;

namespace MediatR.Examples;

using MediatR.Examples.ExceptionHandler;
using System.IO;
using System.Threading.Tasks;

public static class Runner
{
    public static async Task Run(IMediator mediator, WrappingWriter writer, string projectName, bool testStreams = false)
    {
        await writer.WriteLineAsync("===============");
        await writer.WriteLineAsync(projectName);
        await writer.WriteLineAsync("===============");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("Sending Ping...");
        var pong = await mediator.Send(new Ping { Message = "Ping" });
        await writer.WriteLineAsync("Received: " + pong.Message);
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("Publishing Pinged...");
        await mediator.Publish(new Pinged());
        await writer.WriteLineAsync();

        await writer.WriteLineAsync("Publishing Ponged...");
        var failedPong = false;
        try
        {
            await mediator.Publish(new Ponged());
        }
        catch (Exception e)
        {
            failedPong = true;
            await writer.WriteLineAsync(e.ToString());
        }
        await writer.WriteLineAsync();

        var failedJing = false;
        await writer.WriteLineAsync("Sending Jing...");
        try
        {
            await mediator.Send(new Jing { Message = "Jing" });
        }
        catch (Exception e)
        {
            failedJing = true;
            await writer.WriteLineAsync(e.ToString());
        }
        await writer.WriteLineAsync();

        bool failedSing = false;
        if (testStreams)
        {
            await writer.WriteLineAsync("Sending Sing...");
            try
            {
                int i = 0;
                await foreach (Song s in mediator.CreateStream(new Sing { Message = "Sing" }))
                {
                    if (i == 0) {
                        failedSing = !(s.Message.Contains("Singing do"));
                    }
                    else if (i == 1)
                    {
                        failedSing = !(s.Message.Contains("Singing re"));
                    }
                    else if (i == 2)
                    {
                        failedSing = !(s.Message.Contains("Singing mi"));
                    }
                    else if (i == 3)
                    {
                        failedSing = !(s.Message.Contains("Singing fa"));
                    }
                    else if (i == 4)
                    {
                        failedSing = !(s.Message.Contains("Singing so"));
                    }
                    else if (i == 5)
                    {
                        failedSing = !(s.Message.Contains("Singing la"));
                    }
                    else if (i == 6)
                    {
                        failedSing = !(s.Message.Contains("Singing ti"));
                    }
                    else if (i == 7)
                    {
                        failedSing = !(s.Message.Contains("Singing do"));
                    }

                    failedSing = failedSing || (++i) > 10;
                }
            }
            catch (Exception e)
            {
                failedSing = true;
                await writer.WriteLineAsync(e.ToString());
            }
            await writer.WriteLineAsync();
        }

        var isHandlerForSameExceptionWorks = await IsHandlerForSameExceptionWorks(mediator, writer).ConfigureAwait(false);
        var isHandlerForBaseExceptionWorks = await IsHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);
        var isHandlerForLessSpecificExceptionWorks = await IsHandlerForLessSpecificExceptionWorks(mediator, writer).ConfigureAwait(false);
        var isPreferredHandlerForBaseExceptionWorks = await IsPreferredHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);
        var isOverriddenHandlerForBaseExceptionWorks = await IsOverriddenHandlerForBaseExceptionWorks(mediator, writer).ConfigureAwait(false);

        await writer.WriteLineAsync("---------------");
        var contents = writer.Contents;
        var order = new[] {
            contents.IndexOf("- Starting Up", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Handling Request", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("--- Handled Ping", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Finished Request", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("- All Done", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("- All Done with Ping", StringComparison.OrdinalIgnoreCase),
        };

        var streamOrder = new[] {
            contents.IndexOf("-- Handling StreamRequest", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("--- Handled Sing: Sing, Song", StringComparison.OrdinalIgnoreCase),
            contents.IndexOf("-- Finished StreamRequest", StringComparison.OrdinalIgnoreCase),
        };

        var results = new RunResults
        {
            RequestHandlers = contents.Contains("--- Handled Ping:"),
            VoidRequestsHandlers = contents.Contains("--- Handled Jing:"),
            PipelineBehaviors = contents.Contains("-- Handling Request"),
            RequestPreProcessors = contents.Contains("- Starting Up"),
            RequestPostProcessors = contents.Contains("- All Done"),
            ConstrainedGenericBehaviors = contents.Contains("- All Done with Ping") && !failedJing,
            OrderedPipelineBehaviors = order.SequenceEqual(order.OrderBy(i => i)),
            NotificationHandler = contents.Contains("Got pinged async"),
            MultipleNotificationHandlers = contents.Contains("Got pinged async") && contents.Contains("Got pinged also async"),
            ConstrainedGenericNotificationHandler = contents.Contains("Got pinged constrained async") && !failedPong,
            CovariantNotificationHandler = contents.Contains("Got notified"),
            HandlerForSameException = isHandlerForSameExceptionWorks,
            HandlerForBaseException = isHandlerForBaseExceptionWorks,
            HandlerForLessSpecificException = isHandlerForLessSpecificExceptionWorks,
            PreferredHandlerForBaseException = isPreferredHandlerForBaseExceptionWorks,
            OverriddenHandlerForBaseException = isOverriddenHandlerForBaseExceptionWorks,

            // Streams
            StreamRequestHandlers = contents.Contains("--- Handled Sing: Sing, Song") && !failedSing,
            StreamPipelineBehaviors = contents.Contains("-- Handling StreamRequest"),
            StreamOrderedPipelineBehaviors = streamOrder.SequenceEqual(streamOrder.OrderBy(i => i))
        };

        await writer.WriteLineAsync($"Request Handler....................................................{(results.RequestHandlers ? "Y" : "N")}");
        await writer.WriteLineAsync($"Void Request Handler...............................................{(results.VoidRequestsHandlers ? "Y" : "N")}");
        await writer.WriteLineAsync($"Pipeline Behavior..................................................{(results.PipelineBehaviors ? "Y" : "N")}");
        await writer.WriteLineAsync($"Pre-Processor......................................................{(results.RequestPreProcessors ? "Y" : "N")}");
        await writer.WriteLineAsync($"Post-Processor.....................................................{(results.RequestPostProcessors ? "Y" : "N")}");
        await writer.WriteLineAsync($"Constrained Post-Processor.........................................{(results.ConstrainedGenericBehaviors ? "Y" : "N")}");
        await writer.WriteLineAsync($"Ordered Behaviors..................................................{(results.OrderedPipelineBehaviors ? "Y" : "N")}");
        await writer.WriteLineAsync($"Notification Handler...............................................{(results.NotificationHandler ? "Y" : "N")}");
        await writer.WriteLineAsync($"Notification Handlers..............................................{(results.MultipleNotificationHandlers ? "Y" : "N")}");
        await writer.WriteLineAsync($"Constrained Notification Handler...................................{(results.ConstrainedGenericNotificationHandler ? "Y" : "N")}");
        await writer.WriteLineAsync($"Covariant Notification Handler.....................................{(results.CovariantNotificationHandler ? "Y" : "N")}");
        await writer.WriteLineAsync($"Handler for inherited request with same exception used.............{(results.HandlerForSameException ? "Y" : "N")}");
        await writer.WriteLineAsync($"Handler for inherited request with base exception used.............{(results.HandlerForBaseException ? "Y" : "N")}");
        await writer.WriteLineAsync($"Handler for request with less specific exception used by priority..{(results.HandlerForLessSpecificException ? "Y" : "N")}");
        await writer.WriteLineAsync($"Preferred handler for inherited request with base exception used...{(results.PreferredHandlerForBaseException ? "Y" : "N")}");
        await writer.WriteLineAsync($"Overridden handler for inherited request with same exception used..{(results.OverriddenHandlerForBaseException ? "Y" : "N")}");

        if (testStreams)
        {
            await writer.WriteLineAsync($"Stream Request Handler.............................................{(results.StreamRequestHandlers ? "Y" : "N")}");
            await writer.WriteLineAsync($"Stream Pipeline Behavior...........................................{(results.StreamPipelineBehaviors ? "Y" : "N")}");
            await writer.WriteLineAsync($"Stream Ordered Behaviors...........................................{(results.StreamOrderedPipelineBehaviors ? "Y" : "N")}");
        }

        await writer.WriteLineAsync();
    }

    private static async Task<bool> IsHandlerForSameExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        var isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking handler to catch exact exception...");
        try
        {
            await mediator.Send(new PingProtectedResource { Message = "Ping to protected resource" });
            isHandledCorrectly = IsExceptionHandledBy<ForbiddenException, AccessDeniedExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        var isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking shared handler to catch exception by base type...");
        try
        {
            await mediator.Send(new PingResource { Message = "Ping to missed resource" });
            isHandledCorrectly = IsExceptionHandledBy<ResourceNotFoundException, ConnectionExceptionHandler>(writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }
        
    private static async Task<bool> IsHandlerForLessSpecificExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        var isHandledCorrectly = false;

        await writer.WriteLineAsync("Checking base handler to catch any exception...");
        try
        {
            await mediator.Send(new PingResourceTimeout { Message = "Ping to ISS resource" });
            isHandledCorrectly = IsExceptionHandledBy<TaskCanceledException, CommonExceptionHandler> (writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsPreferredHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        var isHandledCorrectly = false;

        await writer.WriteLineAsync("Selecting preferred handler to handle exception...");

        try
        {
            await mediator.Send(new ExceptionHandler.Overrides.PingResourceTimeout { Message = "Ping to ISS resource (preferred)" });
            isHandledCorrectly = IsExceptionHandledBy<TaskCanceledException, ExceptionHandler.Overrides.CommonExceptionHandler> (writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static async Task<bool> IsOverriddenHandlerForBaseExceptionWorks(IMediator mediator, WrappingWriter writer)
    {
        var isHandledCorrectly = false;

        await writer.WriteLineAsync("Selecting new handler to handle exception...");

        try
        {
            await mediator.Send(new PingNewResource { Message = "Ping to ISS resource (override)" });
            isHandledCorrectly = IsExceptionHandledBy<ServerException, ExceptionHandler.Overrides.ServerExceptionHandler> (writer);
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
        await writer.WriteLineAsync();

        return isHandledCorrectly;
    }

    private static bool IsExceptionHandledBy<TException, THandler>(WrappingWriter writer)
        where TException : Exception
    {
        var messages = writer.Contents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        if (messages.Count - 3 < 0)
            return false;

        // Note: For this handler type to be found in messages, it must be written in messages by LogExceptionAction
        return messages[messages.Count - 2].Contains(typeof(THandler).FullName)
            // Note: For this exception type to be found in messages, exception must be written in all tested exception handlers
               && messages[messages.Count - 3].Contains(typeof(TException).FullName);
    }
}

public class RunResults
{
    public bool RequestHandlers { get; set; }
    public bool VoidRequestsHandlers { get; set; }
    public bool PipelineBehaviors { get; set; }
    public bool RequestPreProcessors { get; set; }
    public bool RequestPostProcessors { get; set; }
    public bool OrderedPipelineBehaviors { get; set; }
    public bool ConstrainedGenericBehaviors { get; set; }
    public bool NotificationHandler { get; set; }
    public bool MultipleNotificationHandlers { get; set; }
    public bool CovariantNotificationHandler { get; set; }
    public bool ConstrainedGenericNotificationHandler { get; set; }
    public bool HandlerForSameException { get; set; }
    public bool HandlerForBaseException { get; set; }
    public bool HandlerForLessSpecificException { get; set; }
    public bool PreferredHandlerForBaseException { get; set; }
    public bool OverriddenHandlerForBaseException { get; set; }

    // Stream results
    public bool StreamRequestHandlers { get; set; }
    public bool StreamPipelineBehaviors { get; set; }
    public bool StreamOrderedPipelineBehaviors { get; set; }
}

public class WrappingWriter : TextWriter
{
    private readonly TextWriter _innerWriter;
    private readonly StringBuilder _stringWriter = new StringBuilder();

    public WrappingWriter(TextWriter innerWriter)
    {
        _innerWriter = innerWriter;
    }

    public override void Write(char value)
    {
        _stringWriter.Append(value);
        _innerWriter.Write(value);
    }

    public override Task WriteLineAsync(string value)
    {
        _stringWriter.AppendLine(value);
        return _innerWriter.WriteLineAsync(value);
    }

    public override Encoding Encoding => _innerWriter.Encoding;

    public string Contents => _stringWriter.ToString();
}
