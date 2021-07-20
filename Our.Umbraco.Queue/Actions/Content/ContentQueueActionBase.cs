using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Queue.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Our.Umbraco.Queue.Actions.Content
{
    public abstract class ContentQueueActionBase
    {
        public int Priority => Queue.DefaultPriority;

        protected readonly IContentService contentService;

        public ContentQueueActionBase(IContentService contentService)
        {
            this.contentService = contentService;
        }

        public Attempt<QueuedItem> Process(QueuedItem item)
        {
            if (item.Udi is GuidUdi guidUdi)
            {
           
                var contentItem = contentService.GetById(guidUdi.Guid);
                if (contentItem == null)
                {
                    return Attempt.Fail(item, new KeyNotFoundException("Content not found"));
                }

                return ProcessItem(contentItem, item);
            }

            return Attempt.Fail(item, new ArgumentException("Invalid Udi for item"));
        }

        protected abstract Attempt<QueuedItem> ProcessItem(IContent item, QueuedItem queueItem);



    }
}
