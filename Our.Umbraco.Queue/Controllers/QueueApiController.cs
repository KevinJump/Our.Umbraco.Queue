using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Our.Umbraco.Queue.Hubs;
using Our.Umbraco.Queue.Models;
using Our.Umbraco.Queue.Services;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.WebApi;

namespace Our.Umbraco.Queue.Controllers
{
    public class QueueApiController : UmbracoAuthorizedApiController
    {
        private readonly QueueService queueService;

        public QueueApiController(QueueService queueService)
        {
            this.queueService = queueService;
        }

        [HttpGet]
        // used to get the enpoint for JS Variables
        public bool GetServiceApi() => true;

        [HttpGet]
        public PagedResult<QueuedItem> GetItems(int page)
            => queueService.List(page, 25);

        [HttpGet]
        public QueueStatus GetStatus()
            => queueService.GetStatus();

        [HttpPost]
        public int ProcessQueue(int throttle = 500)
            => queueService.ProcessQueue(throttle);

        [HttpDelete]
        public int ClearQueue()
        {
            var size = queueService.Count();
            queueService.Clear();
            return size;
        }


        [HttpPost]
        public int QueueForPublish(int id, QueueOptions options)
        {
            var item = Services.ContentService.GetById(id);
            if (item == null)
                throw new KeyNotFoundException("Cannot find content");

            var count = 0;

            queueService.Enqueue(PrepQueuedItem(item, "contentPublish", options));
            count++;

            if (options.IncludeChildren)
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<QueueHub>();

                const int pageSize = 1000;
                var page = 0;
                var total = long.MaxValue;
                while (page * pageSize < total)
                {
                    var descendants = Services.ContentService.GetPagedDescendants(id, page, pageSize, out total);

                    foreach (var descendant in descendants)
                    {
                        if (options.IncludeUnpublished || descendant.Published)
                        {
                            queueService.Enqueue(PrepQueuedItem(descendant, "contentPublish", options));
                            count++;

                            hubContext.Clients.All.Add(new QueueProgress()
                            {
                                Count = count,
                                Total = (int)total+1,
                                Title = "Queuing",
                                Message = descendant.Name
                            });
                        }
                    }

                    page++;
                }

                hubContext.Clients.All.Add(new QueueProgress()
                {
                    Count = count,
                    Total = (int)total + 1,
                    Title = "Complete", 
                    Message = $"Queued {count} items"
                });

                queueService.Refresh();

            }


            return count;
        }
            
        private QueuedItem PrepQueuedItem(IContent item, string action, QueueOptions options)
        {
            var queuedItem = new QueuedItem()
            {
                Name = item.Name,
                Udi = item.GetUdi(),
                Action = action,
                UserId = Security.CurrentUser.Id
            };

            return queuedItem;
        }

    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class QueueOptions 
    {  
        public bool IncludeChildren { get; set; }   

        public bool IncludeUnpublished { get; set; }
    
    }
}
