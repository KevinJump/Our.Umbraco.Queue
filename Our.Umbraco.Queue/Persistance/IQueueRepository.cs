using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Queue.Models;
using Umbraco.Core.Models;

namespace Our.Umbraco.Queue.Persistance
{
    public interface IQueueRepository
    {
        /// <summary>
        ///  empty the queue
        /// </summary>
        void Clear();

        /// <summary>
        ///  number of items in the queue
        /// </summary>
        int Count();

        QueuedItem Dequeue();
        QueuedItem Enqueue(QueuedItem item);

        IEnumerable<QueuedItem> List();

        PagedResult<QueuedItem> List(int page, int pageSize);
    }
}
