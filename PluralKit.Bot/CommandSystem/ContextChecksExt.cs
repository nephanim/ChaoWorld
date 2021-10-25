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
            throw new PKError("This command can not be run in a DM.");
        }

        public static Context CheckSystemPrivacy(this Context ctx, Garden target, PrivacyLevel level)
        {
            if (level.CanAccess(ctx.LookupContextFor(target))) return ctx;
            throw new PKError("You do not have permission to access this information.");
        }

        public static Context CheckOwnMember(this Context ctx, Chao member)
        {
            if (member.Garden != ctx.System?.Id)
                throw Errors.NotOwnMemberError;
            return ctx;
        }

        public static Context CheckOwnGroup(this Context ctx, PKGroup group)
        {
            if (group.System != ctx.System?.Id)
                throw Errors.NotOwnGroupError;
            return ctx;
        }

        public static Context CheckSystem(this Context ctx)
        {
            if (ctx.System == null)
                throw Errors.NoSystemError;
            return ctx;
        }

        public static Context CheckNoSystem(this Context ctx)
        {
            if (ctx.System != null)
                throw Errors.ExistingSystemError;
            return ctx;
        }

        public static Context CheckAuthorPermission(this Context ctx, PermissionSet neededPerms, string permissionName)
        {
            if ((ctx.UserPermissions & neededPerms) != neededPerms)
                throw new PKError($"You must have the \"{permissionName}\" permission in this server to use this command.");
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
                throw new PKError("This command is only usable by bot admins.");

            return ctx;
        }
    }
}