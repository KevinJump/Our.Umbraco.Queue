using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Migrations;

namespace Our.Umbraco.Queue.Persistance.Migrations
{
    public class QueueMigrationPlan : MigrationPlan
    {
        public QueueMigrationPlan() : base("TheQueue")
        {
            From(string.Empty)
                .To<CreateQueueTablesMigration>("CreateTables")
                .To<AddSendToQueueToAdmin>("DefaultPermissions");
        }
    }
}
