using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Our.Umbraco.Queue.Services;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Sync;
using Umbraco.Web;
using Umbraco.Web.Scheduling;

namespace Our.Umbraco.Queue.Tasks
{
    public class BackgroundQueueProcessor : RecurringTaskBase
    {
        private readonly IProfilingLogger logger;
        private readonly QueueService queueService;
        private readonly IRuntimeState runtimeState;
        private readonly IUmbracoContextFactory umbracoContextFactory;

        public BackgroundQueueProcessor(
            IBackgroundTaskRunner<RecurringTaskBase> runner, 
            int delayMilliseconds, 
            int periodMilliseconds,
            IProfilingLogger logger,
            IRuntimeState runtimeState,
            IUmbracoContextFactory umbracoContextFactory,
            QueueService queueService) 
            : base(runner, delayMilliseconds, periodMilliseconds)
        {
            this.logger = logger;
            this.queueService = queueService;
            this.runtimeState = runtimeState;
            this.umbracoContextFactory = umbracoContextFactory;
        }

        public override bool IsAsync => false;
        public override bool RunsOnShutdown => false;

        public override bool PerformRun()
        {
            try
            {
                if (RunOnThisServer() && BackgroundJobOn())
                {
                    if (queueService.IsProcessing())
                    {
                        logger.Info<BackgroundQueueProcessor>("Queue is already processing");
                        return true;
                    }

                    using (var contextReference = umbracoContextFactory.EnsureUmbracoContext())
                    {
                        try
                        {
                            var sw = Stopwatch.StartNew();
                            var count = queueService.ProcessQueue(200);
                            sw.Stop();
                            logger.Info<BackgroundQueueProcessor>("Processed {count} items in queue in {time}ms", count, sw.ElapsedMilliseconds);
                        }
                        finally
                        {
                            // if running on a temp context, we have to flush the messenger
                            if (contextReference.IsRoot && 
                                Current.ServerMessenger is BatchedDatabaseServerMessenger m)
                            {
                                m.FlushBatch();
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Error<BackgroundQueueProcessor>(ex, "Error processing queue in background");
            }

            return true; // repeat.
        }


        private bool RunOnThisServer()
        {
            if (!runtimeState.IsMainDom)
            {
                logger.Debug<BackgroundQueueProcessor>("Does not run if not MainDom");
                return false;
            }

            if (runtimeState.Level != RuntimeLevel.Run)
            {
                logger.Debug<BackgroundQueueProcessor>("Does not run if run level is not run");
                return false;
            }

            switch (runtimeState.ServerRole)
            {
                case ServerRole.Replica:
                    logger.Debug<BackgroundQueueProcessor>("Doesn't run on Replica");
                    return false;
                case ServerRole.Unknown:
                    logger.Debug<BackgroundQueueProcessor>("Doesn't run on Unknown");
                    return false;
                default:
                    return true;
            }
        }

        private bool BackgroundJobOn()
            => true; // turn it off for now, we have context issues :( 

    }


}
