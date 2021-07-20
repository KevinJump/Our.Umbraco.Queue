using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Web;
using Umbraco.Web.Actions;

namespace Our.Umbraco.Queue
{
    public class SendQueueAction : IAction
    {
        public char Letter => Queue.SendQueueAction[0];

        public bool ShowInNotifier => false;

        public bool CanBePermissionAssigned => true;

        public string Icon => "icon-indent";

        public string Alias => "queue";

        public string Category => "content";
    }
}
