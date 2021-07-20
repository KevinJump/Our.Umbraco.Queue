using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Umbraco.Core;

namespace Our.Umbraco.Queue.Models
{
    /// <summary>
    ///  An item in the Queue.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class QueuedItem
    {
        public int Id { get; set; }
       
        public Udi Udi { get; set; }

        public string Name { get; set; }

        public int UserId { get; set; }

        public DateTime Submitted { get; set; } = DateTime.Now;

        /// <summary>
        ///  Number of times this item has been requeued, 
        /// </summary>
        public int Attempt { get; set; }

        /// <summary>
        ///  alias of the action needed for this item.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        ///  priority (used to boost things in the queue)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        ///  Scheduled time, (will not process before this time)
        /// </summary>
        public DateTime Schedule { get; set; } = DateTime.Now;

        /// <summary>
        ///  custom data for this action.
        /// </summary>
        public string Data { get; set; }
    }
}
