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
    public class QueueUnpublishAction : ContentQueueActionBase, IQueueAction
    {
        public QueueUnpublishAction(IContentService contentService) 
            : base(contentService) { }

        public string Name => "ContentUnpublish";

        protected override Attempt<QueuedItem> ProcessItem(IContent item, QueuedItem queuedItem)
        {
            var result = contentService.Unpublish(item, userId: queuedItem.UserId);
            if (result.Success)
                return Attempt.Succeed(queuedItem);
            else
                return Attempt.Fail(queuedItem, new InvalidOperationException("Failed to unpublish"));
        }
    }
}
