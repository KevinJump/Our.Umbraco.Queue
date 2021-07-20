using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Our.Umbraco.Queue.Controllers;
using Our.Umbraco.Queue.Persistance.Migrations;
using Our.Umbraco.Queue.Services;
using Our.Umbraco.Queue.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.JavaScript;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Scheduling;
using Umbraco.Web.Trees;

namespace Our.Umbraco.Queue
{
    public class QueueComponent : IComponent
    {
        private readonly IScopeProvider scopeProvider;
        private readonly IMigrationBuilder migrationBuilder;
        private readonly IKeyValueService keyValueService;
        private readonly IUserService userService;
        private readonly IProfilingLogger logger;
        private readonly IUmbracoContextFactory umbracoContextFactory;

        private readonly IRuntimeState runtimeState;

        private readonly QueueService queueService;

        private bool backgroundOn = true;
        private int pollPeriod = 60;

        public QueueComponent(
            IScopeProvider scopeProvider,
            IMigrationBuilder migrationBuilder,
            IKeyValueService keyValueService,
            IUserService userService,
            IProfilingLogger logger,
            IRuntimeState runtimeState,
            IUmbracoContextFactory umbracoContextFactory,
            QueueService queueService)
        {
            this.scopeProvider = scopeProvider;
            this.migrationBuilder = migrationBuilder;
            this.keyValueService = keyValueService;
            this.userService = userService;

            this.logger = logger;

            this.runtimeState = runtimeState;
            this.umbracoContextFactory = umbracoContextFactory;

            this.queueService = queueService;
        }

        public void Initialize()
        {
            // perform the migration (create the db tables, etc)
            var upgrader = new Upgrader(new QueueMigrationPlan());
            upgrader.Execute(scopeProvider, migrationBuilder, keyValueService, logger);

            var background = ConfigurationManager.AppSettings["TheQueue.BackgroundProcessing"]?.TryConvertTo<bool>();
            var period = ConfigurationManager.AppSettings["TheQueue.BackgroundPeriod"]?.TryConvertTo<int>();

            if (period.HasValue && period.Value.Success)
            {
                pollPeriod = period.Value.Result;
            }

            if (background.HasValue && background.Value.Success && background.Value.Result == false)
            {
                backgroundOn = false;
                // don't turn on the background task
            }
            else
            {
                RegisterBackgroundTask();
            }

            ContentTreeController.MenuRendering += ContentTreeController_MenuRendering;
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;

        }

        private void ContentTreeController_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            if (!sender.TreeAlias.InvariantEquals("content")) return;
            if (!UserHasPermissions(sender, e.NodeId, "q")) return; 

            var publishItem = new MenuItem("queue", "Send to Queue")
            {
                Icon = "indent",
                SeparatorBefore = true,
            };

            publishItem.AdditionalData.Add("actionView",
                "/App_Plugins/TheQueue/sendtoqueue.html?id={id}");
            e.Menu.Items.Insert(e.Menu.Items.Count - 1, publishItem);
        }

        /// <summary>
        ///  checks to see if the user has the permissions to perform an action
        /// </summary>
        private bool UserHasPermissions(TreeControllerBase sender, string nodeId, string permission)
        {
            if (int.TryParse(nodeId, out int nodeInt))
            {
                var permissions = sender.Services.UserService.GetPermissions(sender.Security.CurrentUser, nodeInt);
                return permissions.Any(x => x.AssignedPermissions.Contains("q"));
            }
            return false; 
        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("This method requires that an HttpContext be active");

            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            e.Add("theQueue", new Dictionary<string, object>
            {
                {"queueService", urlHelper.GetUmbracoApiServiceBaseUrl<QueueApiController>(controller => controller.GetServiceApi()) },
                {"backgroundOn", this.backgroundOn }
            });
        }

        public void Terminate()
        {
            // do nothing...
        }

        private void RegisterBackgroundTask()
        {
            int period = pollPeriod;

            logger.Debug<QueueComponent>("Starting background task, every {poll} seconds", period);

            var runner = new BackgroundTaskRunner<IBackgroundTask>("Queue processing", logger);
            if (runner != null)
            {
                var check = new BackgroundQueueProcessor(runner, period * 1000, period * 1000, logger, runtimeState, umbracoContextFactory, queueService);
                runner.Add(check);
            }

        }
    }
}
