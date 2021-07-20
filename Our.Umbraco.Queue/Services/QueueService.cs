using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Our.Umbraco.Queue.Actions;
using Our.Umbraco.Queue.Models;
using Our.Umbraco.Queue.Persistance;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;

namespace Our.Umbraco.Queue.Services
{
    public delegate void QueueServiceEventHandler(QueueEventArgs e);

    public class QueueService
    {
        public static event QueueServiceEventHandler QueueUpdate;

        private readonly IScopeProvider scopeProvider;

        private readonly IProfilingLogger logger;

        private readonly IQueueRepository queueRepository;
        private readonly QueueActionCollection queueActions;

        private bool processing;

        public QueueService(
            IScopeProvider scopeProvider,
            IQueueRepository queueRepository,
            QueueActionCollection queueActions,
            IProfilingLogger logger)
        {
            this.scopeProvider = scopeProvider;

            this.queueRepository = queueRepository;
            this.queueActions = queueActions;

            this.logger = logger;

            this.processing = false;
        }


        /// <summary>
        ///  Is the queue currently being processed by the service?
        /// </summary>
        public bool IsProcessing()
            => processing;


        /// <summary>
        ///  how many items are in the queue.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return queueRepository.Count();
            }
        }

        public QueuedItem Dequeue()
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return queueRepository.Dequeue();
            }
        }


        public QueuedItem Enqueue(QueuedItem item)
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                var queued = queueRepository.Enqueue(item);

                QueueUpdate?.Invoke(new QueueEventArgs()
                {
                    Processing = this.processing,
                    QueueSize = this.Count()
                });

                return queued;
            }
        }


        public QueuedItem Enqueue(Udi udi, string name, string actiom, int userId = -1)
        {
            var item = new QueuedItem()
            {
                Name = name,
                Udi = udi,
                Action = actiom,
                UserId = userId,
                Submitted = DateTime.Now
            };

            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return Enqueue(item);
            }
        }

        public IEnumerable<QueuedItem> List()
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return queueRepository.List();
            }
        }

        public PagedResult<QueuedItem> List(int page, int pageSize)
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return queueRepository.List(page, pageSize);
            }
        }

        public void Clear()
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                queueRepository.Clear();
            }
        }

        public QueueStatus GetStatus()
        {
            using (scopeProvider.CreateScope(autoComplete: true))
            {
                return new QueueStatus()
                {
                    QueueSize = this.Count(),
                    IsProcessing = this.IsProcessing()
                };
            }
        }



        private object processLock = new object();

        public int ProcessQueue(int throttle = 100)
        {
            if (processing) return -1;

            logger.Debug<QueueService>("Processing the Queue");

            int count = 0;
            int queueSize = this.Count();
            int startSize = Math.Min(queueSize, throttle); 

            lock (processLock)
            {
                if (processing) return -1;

                try
                {
                    processing = true;

                    while (Count() > 0 && count < throttle)
                    {
                        count++;

                        var item = Dequeue();
                        if (item != null)
                        {
                            var result = new List<Attempt<QueuedItem>>();

                            // find the actions
                            var actions = queueActions.GetActions(item.Action);
                            if (actions != null && actions.Any())
                            {
                                // process the actions
                                foreach (var action in actions)
                                {
                                    logger.Debug<QueueService>("Processing {itemName} with {actionName}", item.Name, action.Name);
                                    result.Add(action.Process(item));
                                }
                            }
                            else
                            {
                                logger.Warn<QueueService>("No Action {actionName} found", item.Action);
                                // do we requeue ?
                            }

                            if (result.Any(x => x.Success == false))
                            {
                                // there where failures.
                                // do we requeue ?                                
                            }
                        }

                        // don't want to batter the db, with the count.
                        if (count % 5 == 0) queueSize = this.Count();

                        QueueUpdate?.Invoke(new QueueEventArgs()
                        {
                            Processed = count,
                            Remaining = startSize - count,
                            QueueSize = queueSize,
                            Processing = true
                        }); ;

                    }

                }
                catch (Exception ex)
                {
                    // do something
                    throw ex;
                }
                finally
                {
                    processing = false;
                }
            }

            QueueUpdate?.Invoke(new QueueEventArgs()
            {
                Processing = false,
                Processed = count,
                Remaining = 0,
                QueueSize = this.Count()
            });

            return count;

        }

        /// <summary>
        ///  triggers the update event, with refresh set to true, so 
        ///  everyone can update their counts.
        /// </summary>
        public void Refresh()
        {
            QueueUpdate?.Invoke(new QueueEventArgs()
            {
                Refresh = true,
                Processing = this.processing,
                Processed = 0,
                Remaining = 0,
                QueueSize = this.Count()
            });
        }

    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class QueueStatus
    {
        public int QueueSize { get; set; }
        public bool IsProcessing { get; set; }

        public int Processed { get; set; }
        public int Remaining { get; set; }

    }

}
