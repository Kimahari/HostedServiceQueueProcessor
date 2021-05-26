using System.Collections.Concurrent;
using System.Threading;

namespace HostedServiceQueueProcessor.Classes {
    public class MemoryQueue {

        #region Private Fields

        private readonly BlockingCollection<QueueItem> work = new();

        #endregion Private Fields

        #region Public Methods

        public QueueItem Dequeue(CancellationToken cancellationToken) {
            return work.Take(cancellationToken);
        }

        public void Enqueue(QueueItem item) {
            work.Add(item);
        }

        #endregion Public Methods
    }
}
