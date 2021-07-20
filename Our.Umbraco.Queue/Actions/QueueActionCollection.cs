using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.Queue.Actions
{
    public class QueueActionCollection
        : BuilderCollectionBase<IQueueAction>
    {
        public QueueActionCollection(IEnumerable<IQueueAction> items)
            : base(items) { }

        public IEnumerable<IQueueAction> GetActions(string name)
            => this.Where(x => x.Name.InvariantEquals(name))
                    .OrderBy(x => x.Priority);

    }

    public class QueueActionCollectionBuilder
        : LazyCollectionBuilderBase<QueueActionCollectionBuilder, QueueActionCollection, IQueueAction>
    {
        protected override QueueActionCollectionBuilder This => this;
    }
}
