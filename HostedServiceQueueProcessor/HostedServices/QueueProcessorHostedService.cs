using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using HostedServiceQueueProcessor.Classes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HostedServiceQueueProcessor.HostedServices {

    public class QueueProcessorHostedService :IHostedService {
        private Task dispatchTask;
        private CancellationTokenSource queueProcessorSource = new CancellationTokenSource();
        private ILogger<QueueProcessorHostedService> logger;
        private MemoryQueue queue;
        private IServiceProvider serviceProvider;
        private CancellationToken startupToken;

        public QueueProcessorHostedService(ILogger<QueueProcessorHostedService> logger, MemoryQueue queue, IServiceProvider serviceProvider) {
            this.logger = logger;
            this.queue = queue;
            this.serviceProvider = serviceProvider;
        }

        public virtual Task StartAsync(CancellationToken cancellationToken) {
            startupToken = cancellationToken;

            logger.LogInformation("Service Starting.");

            dispatchTask = new Task(ProcessQueue);

            dispatchTask.Start();

            logger.LogInformation("Service Started.");
            return Task.CompletedTask;
        }

        private void ProcessQueue() {
            var maxConcurrentItems = Math.Max(System.Environment.ProcessorCount / 2, 2);

            var opts = new ExecutionDataflowBlockOptions() {
                MaxDegreeOfParallelism = maxConcurrentItems,
                BoundedCapacity = maxConcurrentItems + 1
            };

            var actionBlock = new ActionBlock<QueueItem>(ProcessQueueItem, opts);

            while(!queueProcessorSource.Token.IsCancellationRequested) {
                try {
                    if(!SpinWait.SpinUntil(() => actionBlock.InputCount == 0, 250)) continue;

                    var workItem = queue.Dequeue(queueProcessorSource.Token);

                    if(workItem == null) continue;

                    if(!actionBlock.Post(workItem)) queue.Enqueue(workItem);
                } catch(OperationCanceledException ex) {
                    logger.LogWarning(ex, "Cancellation occurred while attempting to signal work");
                } catch(Exception ex) {
                    logger.LogError(ex, "Error occurred while attempting to signal work.");
                }
            }
        }

        private void ProcessQueueItem(QueueItem item) {
            using(var processScope = serviceProvider.CreateScope()) {
                try {
                    logger.LogDebug($"Processing queue item {item}");
                    ProcessItem(processScope.ServiceProvider, item).Wait();
                } catch(Exception ex) {
                    logger.LogError(ex, "Error occurred while processing queue item");
                }
            }
        }

        private Task ProcessItem(IServiceProvider serviceProvider, QueueItem item) {
            this.logger.LogInformation($"Executing Queue Item {item.SomeData}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public virtual async Task StopAsync(CancellationToken cancellationToken) {
            queueProcessorSource.Cancel();
            logger.LogInformation("Stopping Service.");
            await dispatchTask;
            logger.LogInformation("Service Stopped.");
        }
    }
}
