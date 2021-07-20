using System;

using Our.Umbraco.Queue.Models;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Our.Umbraco.Queue.Actions.Content
{
    public class QueueActionDelete : ContentQueueActionBase, IQueueAction
    {
        public QueueActionDelete(IContentService contentService)
            : base(contentService) { }

        public string Name => "ContentDelete";

        protected override Attempt<QueuedItem> ProcessItem(IContent item, QueuedItem queuedItem)
        {
            var result = contentService.Delete(item);
            if (result.Success)
                return Attempt.Succeed(queuedItem);
            else
                return Attempt.Fail(queuedItem, new InvalidOperationException("Unable to delete item"));
        }
    }
}
