using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Queue.Models;
using Umbraco.Core;

namespace Our.Umbraco.Queue.Actions
{
    public interface IQueueAction
    {
        string Name { get; }

        int Priority { get; }

        Attempt<QueuedItem> Process(QueuedItem item);
    }
}
