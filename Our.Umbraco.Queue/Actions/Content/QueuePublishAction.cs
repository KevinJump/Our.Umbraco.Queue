using System;

using Our.Umbraco.Queue.Models;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Our.Umbraco.Queue.Actions.Content
{
    public class QueuePublishAction : ContentQueueActionBase, IQueueAction
    {
        public QueuePublishAction(IContentService contentService)
            : base(contentService) { }


        public string Name => "ContentPublish";

        protected override Attempt<QueuedItem> ProcessItem(IContent item, QueuedItem queueItem)
        {
            var publishAttempt = contentService.SaveAndPublish(item, userId: queueItem.UserId);
            if (publishAttempt.Success)
                return Attempt.Succeed(queueItem);
            else
                return Attempt.Fail(queueItem, new InvalidOperationException("Publish failed"));
        }
    }
}
