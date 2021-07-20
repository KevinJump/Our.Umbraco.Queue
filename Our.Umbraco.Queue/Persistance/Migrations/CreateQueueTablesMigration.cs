using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Migrations;

namespace Our.Umbraco.Queue.Persistance.Migrations
{
    public class CreateQueueTablesMigration : MigrationBase
    {
        public CreateQueueTablesMigration(IMigrationContext context) 
            : base(context) { }

        public override void Migrate()
        {
            if (!TableExists(Queue.QueueTable))
                Create.Table<QueuedItemDto>().Do();
        }
    }
}
