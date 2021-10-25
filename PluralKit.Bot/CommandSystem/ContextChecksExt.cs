using System.Linq;

using Autofac;

using Myriad.Types;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public static class ContextChecksExt
    {
        public static Context CheckGuildContext(this Context ctx)
        {
            if (ctx.Channel.GuildId != null) return ctx;
            throw new CWError("This command can not be run in a DM.");
        }

        public static Context CheckOwnMember(this Context ctx, Chao member)
        {
            if (member.Garden != ctx.System?.Id)
                throw Errors.NotOwnChaoError;
            return ctx;
        }

        public static Context CheckGarden(this Context ctx)
        {
            if (ctx.System == null)
                throw Errors.NoGardenError;
            return ctx;
        }

        public static Context CheckNoGarden(this Context ctx)
        {
            if (ctx.System != null)
                throw Errors.ExistingGardenError;
            return ctx;
        }

        public static Context CheckAuthorPermission(this Context ctx, PermissionSet neededPerms, string permissionName)
        {
            if ((ctx.UserPermissions & neededPerms) != neededPerms)
                throw new CWError($"You must have the \"{permissionName}\" permission in this server to use this command.");
            return ctx;
        }

        public static bool CheckBotAdmin(this Context ctx)
        {
            var botConfig = ctx.Services.Resolve<BotConfig>();
            return botConfig.AdminRole != null && ctx.Member != null && ctx.Member.Roles.Contains(botConfig.AdminRole.Value);
        }

        public static Context AssertBotAdmin(this Context ctx)
        {
            if (!ctx.CheckBotAdmin())
                throw new CWError("This command is only usable by bot admins.");

            return ctx;
        }
    }
}