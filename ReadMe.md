# Dotnet Core WebApp / MicroService Queue Processor

This is a basic example of executing queue items in Dotnet Core Hosted services using `System.Threading.Tasks.Dataflow` Specifically `ActionBlock`

the basic idea is to have a thread blocking queue for example `BlockingCollection` from `System.Collections.Concurrent` this will allow us to create a blocking queue in order dequeue until we receive an item in the queue.

we can then combine that with the `ActionBlock`

```csharp
var actionBlock = new ActionBlock<QueueItem>(ProcessQueueItem, opts);
```

we can then dequeue from the queue and post into the action block creating a signal process to execute the callback 

```csharp
while(!queueProcessorSource.Token.IsCancellationRequested) {
    try {
       //...
        var workItem = queue.Dequeue(queueProcessorSource.Token);

        if(workItem == null) continue;

        if(!actionBlock.Post(workItem)) queue.Enqueue(workItem);
    } catch(OperationCanceledException ex) {
        logger.LogWarning(ex, "Cancellation occurred - Service is shutting down");
    } catch(Exception ex) {
        logger.LogError(ex, "Error occurred need to do something here.");
    }
}
```

the callback method will look something like this

```csharp
private void ProcessQueueItem(QueueItem item) {
    using(var processScope = serviceProvider.CreateScope()) {
        try {
            logger.LogDebug($"Processing queue item {item}");
            // Do more stuffs here
        } catch(Exception ex) {
            logger.LogError(ex, "handle error here");
        }
    }
}
```
