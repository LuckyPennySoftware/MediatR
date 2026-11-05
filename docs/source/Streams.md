# Streams and AsyncEnumerables

To create a stream from a request, first implement the stream request and its response:

- `IStreamRequest<TResponse>`

    ```cs
    public sealed class CounterStreamRequest : IStreamRequest<int> { }
    ```

Stream request handlers are separate from the normal `IRequestHandler` and require implementing:

- `IStreamRequestHandler<TRequest, TResponse>`

    ```cs
    public sealed class CounterStreamHandler : IStreamRequestHandler<CounterStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(CounterStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            int count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500, cancellationToken);
                yield return count;
                count++;
            }
        }
    }
    ```

Unlike normal request handlers that return a single `TResponse`, a stream handler returns an `IAsyncEnumerable<TResponse>`:

```csharp
IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
```

To create a stream request handler, create a class that implements `IStreamRequestHandler<TRequest, TResponse>` and implement the above Handle method.

Finally, send a stream message through the mediator:

```cs
    CancellationTokenSource cts = new();
    int count = 10;
    await foreach (var item in mediator.CreateStream<int>(new CounterStreamRequest(), cts.Token))
    {
        count--;
        if (count == 0)
        {
            cts.Cancel();
        }
        Debug.WriteLine(item);
    }
```
