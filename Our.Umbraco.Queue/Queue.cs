using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.Queue
{
    public class Queue
    {
        public const string QueueTable = "Queue";

        public const int DefaultPriority = 1000;

        public const string SendQueueAction = "q";
    }
}
