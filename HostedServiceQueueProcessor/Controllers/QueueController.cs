using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using HostedServiceQueueProcessor.Classes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HostedServiceQueueProcessor.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class QueueController :ControllerBase {
        private readonly ILogger<QueueController> _logger;
        private readonly MemoryQueue memoryQueue;

        public QueueController(ILogger<QueueController> logger, MemoryQueue memoryQueue) {
            _logger = logger;
            this.memoryQueue = memoryQueue;
        }

        [HttpPost]
        public void Get() {
            memoryQueue.Enqueue(new QueueItem { SomeData = Guid.NewGuid().ToString() });
        }
    }
}
