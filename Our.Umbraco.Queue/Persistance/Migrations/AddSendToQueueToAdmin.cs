using System;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Services;

namespace Our.Umbraco.Queue.Persistance.Migrations
{
    public class AddSendToQueueToAdmin : MigrationBase
    {
        private readonly IUserService userService;
        public AddSendToQueueToAdmin(
            IUserService userService,
            IMigrationContext context) : base(context)
        {
            this.userService = userService;
        }

        public override void Migrate()
        {
            AddGroupPermissions(Constants.Security.AdminGroupAlias, Queue.SendQueueAction);
        }


        private void AddGroupPermissions(string groupAlias, string permission)
        {
            var group = userService.GetUserGroupByAlias(groupAlias);
            if (group != null)
            {
                if (!group.Permissions.Contains(permission))
                {
                    var permissions = group.Permissions.ToList();
                    permissions.Add(permission);
                    group.Permissions = permissions;

                    try
                    {
                        userService.Save(group);
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Warn<AddSendToQueueToAdmin>("Error adding {0} permission to {1} group [{2}]", permission, groupAlias, ex.Message);
                    }
                }
            }
        }
    }
}
