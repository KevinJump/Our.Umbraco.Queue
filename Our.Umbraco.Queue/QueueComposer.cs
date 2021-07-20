using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Queue.Actions;
using Our.Umbraco.Queue.Persistance;
using Our.Umbraco.Queue.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.Queue
{
    public class QueueComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // the default repo.
            composition.RegisterUnique<IQueueRepository, QueueRepository>();

            // the actions (need to be loaded before the service)
            composition.WithCollectionBuilder<QueueActionCollectionBuilder>()
                .Add(() => composition.TypeLoader.GetTypes<IQueueAction>());

            // the service
            composition.RegisterUnique<QueueService>();

            composition.Components()
                .Append<QueueComponent>();
        }
    }
}
