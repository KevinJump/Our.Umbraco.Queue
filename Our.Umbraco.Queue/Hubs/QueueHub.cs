using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Our.Umbraco.Queue.Services;

namespace Our.Umbraco.Queue.Hubs
{
    public class QueueHub : Hub
    {
        public QueueHub()
        {
            QueueService.QueueUpdate += QueueService_QueueUpdate;
        }

        private void QueueService_QueueUpdate(QueueEventArgs e)
        {
            this.Clients.All.Progress(new QueueStatus()
            {
                IsProcessing = e.Processing,
                QueueSize = e.QueueSize,
                Remaining = e.Remaining,
                Processed = e.Processed,
            });
        }

        public string GetTime()
            => DateTime.Now.ToString();
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class QueueProgress
    {
        public int Count { get; set; }
        public int Total { get; set; }
        
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
