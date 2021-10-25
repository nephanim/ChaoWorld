using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class CommandTree
    {
        public static Command SystemInfo = new Command("system", "system [system]", "Looks up information about a system");
        public static Command SystemNew = new Command("system new", "system new [name]", "Creates a new system");
        public static Command SystemRename = new Command("system name", "system rename [name]", "Renames your system");
        public static Command SystemDesc = new Command("system description", "system description [description]", "Changes your system's description");
        public static Command SystemColor = new Command("system color", "system color [color]", "Changes your system's color");
        public static Command SystemTag = new Command("system tag", "system tag [tag]", "Changes your system's tag");
        public static Command SystemServerTag = new Command("system servertag", "system servertag [tag|enable|disable]", "Changes your system's tag in the current server");
        public static Command SystemAvatar = new Command("system icon", "system icon [url|@mention]", "Changes your system's icon");
        public static Command SystemBannerImage = new Command("system banner", "system banner [url]", "Set the system's banner image");
        public static Command SystemDelete = new Command("system delete", "system delete", "Deletes your system");
        public static Command SystemTimezone = new Command("system timezone", "system timezone [timezone]", "Changes your system's time zone");
        public static Command SystemProxy = new Command("system proxy", "system proxy [server id] [on|off]", "Enables or disables message proxying in a specific server");
        public static Command SystemList = new Command("system list", "system [system] list [full]", "Lists a system's members");
        public static Command SystemFind = new Command("system find", "system [system] find [full] <search term>", "Searches a system's members given a search term");
        public static Command SystemFronter = new Command("system fronter", "system [system] fronter", "Shows a system's fronter(s)");
        public static Command SystemFrontHistory = new Command("system fronthistory", "system [system] fronthistory", "Shows a system's front history");
        public static Command SystemFrontPercent = new Command("system frontpercent", "system [system] frontpercent [timespan]", "Shows a system's front breakdown");
        public static Command SystemPing = new Command("system ping", "system ping <enable|disable>", "Changes your system's ping preferences");
        public static Command SystemPrivacy = new Command("system privacy", "system privacy <description|members|fronter|fronthistory|all> <public|private>", "Changes your system's privacy settings");
        public static Command AutoproxySet = new Command("autoproxy", "autoproxy [off|front|latch|member]", "Sets your system's autoproxy mode for the current server");
        public static Command AutoproxyTimeout = new Command("autoproxy", "autoproxy timeout [<duration>|off|reset]", "Sets the latch timeout duration for your system");
        public static Command AutoproxyAccount = new Command("autoproxy", "autoproxy account [on|off]", "Toggles autoproxy globally for the current account");
        public static Command MemberInfo = new Command("member", "member <member>", "Looks up information about a member");
        public static Command MemberNew = new Command("member new", "member new <name>", "Creates a new member");
        public static Command MemberRename = new Command("member rename", "member <member> rename <new name>", "Renames a member");
        public static Command MemberDesc = new Command("member description", "member <member> description [description]", "Changes a member's description");
        public static Command MemberPronouns = new Command("member pronouns", "member <member> pronouns [pronouns]", "Changes a member's pronouns");
        public static Command MemberColor = new Command("member color", "member <member> color [color]", "Changes a member's color");
        public static Command MemberBirthday = new Command("member birthday", "member <member> birthday [birthday]", "Changes a member's birthday");
        public static Command MemberProxy = new Command("member proxy", "member <member> proxy [add|remove] [example proxy]", "Changes, adds, or removes a member's proxy tags");
        public static Command MemberDelete = new Command("member delete", "member <member> delete", "Deletes a member");
        public static Command MemberBannerImage = new Command("member banner", "member <member> banner [url]", "Set the member's banner image");
        public static Command MemberAvatar = new Command("member avatar", "member <member> avatar [url|@mention]", "Changes a member's avatar");
        public static Command MemberGroups = new Command("member group", "member <member> group", "Shows the groups a member is in");
        public static Command MemberGroupAdd = new Command("member group", "member <member> group add <group> [group 2] [group 3...]", "Adds a member to one or more groups");
        public static Command MemberGroupRemove = new Command("member group", "member <member> group remove <group> [group 2] [group 3...]", "Removes a member from one or more groups");
        public static Command MemberServerAvatar = new Command("member serveravatar", "member <member> serveravatar [url|@mention]", "Changes a member's avatar in the current server");
        public static Command MemberDisplayName = new Command("member displayname", "member <member> displayname [display name]", "Changes a member's display name");
        public static Command MemberServerName = new Command("member servername", "member <member> servername [server name]", "Changes a member's display name in the current server");
        public static Command MemberAutoproxy = new Command("member autoproxy", "member <member> autoproxy [on|off]", "Sets whether a member will be autoproxied when autoproxy is set to latch or front mode.");
        public static Command MemberKeepProxy = new Command("member keepproxy", "member <member> keepproxy [on|off]", "Sets whether to include a member's proxy tags when proxying");
        public static Command MemberRandom = new Command("random", "random", "Shows the info card of a randomly selected member in your system.");
        public static Command MemberPrivacy = new Command("member privacy", "member <member> privacy <name|description|birthday|pronouns|metadata|visibility|all> <public|private>", "Changes a members's privacy settings");
        public static Command GroupInfo = new Command("group", "group <name>", "Looks up information about a group");
        public static Command GroupNew = new Command("group new", "group new <name>", "Creates a new group");
        public static Command GroupList = new Command("group list", "group list", "Lists all groups in this system");
        public static Command GroupMemberList = new Command("group members", "group <group> list", "Lists all members in a group");
        public static Command GroupRename = new Command("group rename", "group <group> rename <new name>", "Renames a group");
        public static Command GroupDisplayName = new Command("group displayname", "group <group> displayname [display name]", "Changes a group's display name");
        public static Command GroupDesc = new Command("group description", "group <group> description [description]", "Changes a group's description");
        public static Command GroupColor = new Command("group color", "group <group> color [color]", "Changes a group's color");
        public static Command GroupAdd = new Command("group add", "group <group> add <member> [member 2] [member 3...]", "Adds one or more members to a group");
        public static Command GroupRemove = new Command("group remove", "group <group> remove <member> [member 2] [member 3...]", "Removes one or more members from a group");
        public static Command GroupPrivacy = new Command("group privacy", "group <group> privacy <description|icon|visibility|all> <public|private>", "Changes a group's privacy settings");
        public static Command GroupBannerImage = new Command("group banner", "group <group> banner [url]", "Set the group's banner image");
        public static Command GroupIcon = new Command("group icon", "group <group> icon [url|@mention]", "Changes a group's icon");
        public static Command GroupDelete = new Command("group delete", "group <group> delete", "Deletes a group");
        public static Command GroupFrontPercent = new Command("group frontpercent", "group <group> frontpercent [timespan]", "Shows a group's front breakdown.");
        public static Command GroupMemberRandom = new Command("group random", "group <group> random", "Shows the info card of a randomly selected member in a group.");
        public static Command GroupRandom = new Command("random", "random group", "Shows the info card of a randomly selected group in your system.");
        public static Command Switch = new Command("switch", "switch <member> [member 2] [member 3...]", "Registers a switch");
        public static Command SwitchOut = new Command("switch out", "switch out", "Registers a switch with no members");
        public static Command SwitchMove = new Command("switch move", "switch move <date/time>", "Moves the latest switch in time");
        public static Command SwitchEdit = new Command("switch edit", "switch edit <member> [member 2] [member 3...]", "Edits the members in the latest switch");
        public static Command SwitchEditOut = new Command("switch edit out", "switch edit out", "Turns the latest switch into a switch-out");
        public static Command SwitchDelete = new Command("switch delete", "switch delete", "Deletes the latest switch");
        public static Command SwitchDeleteAll = new Command("switch delete", "switch delete all", "Deletes all logged switches");
        public static Command Link = new Command("link", "link <account>", "Links your system to another account");
        public static Command Unlink = new Command("unlink", "unlink [account]", "Unlinks your system from an account");
        public static Command TokenGet = new Command("token", "token", "Gets your system's API token");
        public static Command TokenRefresh = new Command("token refresh", "token refresh", "Resets your system's API token");
        public static Command Import = new Command("import", "import [fileurl]", "Imports system information from a data file");
        public static Command Export = new Command("export", "export", "Exports system information to a data file");
        public static Command Help = new Command("help", "help", "Shows help information about ChaoWorld");
        public static Command Explain = new Command("explain", "explain", "Explains the basics of systems and proxying");
        public static Command Message = new Command("message", "message <id|link> [delete|author]", "Looks up a proxied message");
        public static Command MessageEdit = new Command("edit", "edit [link] <text>", "Edit a previously proxied message");
        public static Command ProxyCheck = new Command("debug proxy", "debug proxy [link|reply]", "Checks why your message has not been proxied");
        public static Command LogChannel = new Command("log channel", "log channel <channel>", "Designates a channel to post proxied messages to");
        public static Command LogChannelClear = new Command("log channel", "log channel -clear", "Clears the currently set log channel");
        public static Command LogEnable = new Command("log enable", "log enable all|<channel> [channel 2] [channel 3...]", "Enables message logging in certain channels");
        public static Command LogDisable = new Command("log disable", "log disable all|<channel> [channel 2] [channel 3...]", "Disables message logging in certain channels");
        public static Command LogClean = new Command("logclean", "logclean [on|off]", "Toggles whether to clean up other bots' log channels");
        public static Command BlacklistShow = new Command("blacklist show", "blacklist show", "Displays the current proxy blacklist");
        public static Command BlacklistAdd = new Command("blacklist add", "blacklist add all|<channel> [channel 2] [channel 3...]", "Adds certain channels to the proxy blacklist");
        public static Command BlacklistRemove = new Command("blacklist remove", "blacklist remove all|<channel> [channel 2] [channel 3...]", "Removes certain channels from the proxy blacklist");
        public static Command Invite = new Command("invite", "invite", "Gets a link to invite ChaoWorld to other servers");
        public static Command PermCheck = new Command("permcheck", "permcheck <guild>", "Checks whether a server's permission setup is correct");
        public static Command Admin = new Command("admin", "admin", "Super secret admin commands (sshhhh)");

        public static Command[] SystemCommands = {
            SystemInfo, SystemNew, SystemRename, SystemTag, SystemDesc, SystemAvatar, SystemBannerImage, SystemColor, SystemDelete,
            SystemTimezone, SystemList, SystemFronter, SystemFrontHistory, SystemFrontPercent, SystemPrivacy, SystemProxy
        };

        public static Command[] MemberCommands = {
            MemberInfo, MemberNew, MemberRename, MemberDisplayName, MemberServerName, MemberDesc, MemberPronouns,
            MemberColor, MemberBirthday, MemberProxy, MemberAutoproxy, MemberKeepProxy, MemberGroups, MemberGroupAdd, MemberGroupRemove,
            MemberDelete, MemberAvatar, MemberServerAvatar, MemberBannerImage, MemberPrivacy, MemberRandom
        };

        public static Command[] GroupCommands =
        {
            GroupInfo, GroupList, GroupNew, GroupAdd, GroupRemove, GroupMemberList, GroupRename, GroupDesc,
            GroupIcon, GroupBannerImage, GroupColor, GroupPrivacy, GroupDelete
        };

        public static Command[] GroupCommandsTargeted =
        {
            GroupInfo, GroupAdd, GroupRemove, GroupMemberList, GroupRename, GroupDesc, GroupIcon, GroupPrivacy,
            GroupDelete, GroupMemberRandom, GroupFrontPercent
        };

        public static Command[] SwitchCommands = { Switch, SwitchOut, SwitchMove, SwitchEdit, SwitchEditOut, SwitchDelete, SwitchDeleteAll };

        public static Command[] AutoproxyCommands = { AutoproxySet, AutoproxyTimeout, AutoproxyAccount };

        public static Command[] LogCommands = { LogChannel, LogChannelClear, LogEnable, LogDisable };

        public static Command[] BlacklistCommands = { BlacklistAdd, BlacklistRemove, BlacklistShow };

        public Task ExecuteCommand(Context ctx)
        {
            if (ctx.Match("system", "s"))
                return HandleSystemCommand(ctx);
            if (ctx.Match("member", "m"))
                return HandleMemberCommand(ctx);
            if (ctx.Match("commands", "cmd", "c"))
                return CommandHelpRoot(ctx);
            if (ctx.Match("list", "find", "members", "search", "query", "l", "f", "fd"))
                return ctx.Execute<SystemList>(SystemList, m => m.MemberList(ctx, ctx.System));
            if (ctx.Match("link"))
                return ctx.Execute<SystemLink>(Link, m => m.LinkSystem(ctx));
            if (ctx.Match("unlink"))
                return ctx.Execute<SystemLink>(Unlink, m => m.UnlinkAccount(ctx));
            if (ctx.Match("help"))
                if (ctx.Match("commands"))
                    return ctx.Reply("For the list of commands, see the website: <https://pluralkit.me/commands>");
                else if (ctx.Match("proxy"))
                    return ctx.Reply("The proxy help page has been moved! See the website: https://pluralkit.me/guide#proxying");
                else return ctx.Execute<Help>(Help, m => m.HelpRoot(ctx));
            if (ctx.Match("explain"))
                return ctx.Execute<Help>(Explain, m => m.Explain(ctx));
            if (ctx.Match("message", "msg"))
                return ctx.Execute<ProxiedMessage>(Message, m => m.GetMessage(ctx));
            if (ctx.Match("edit", "e"))
                return ctx.Execute<ProxiedMessage>(MessageEdit, m => m.EditMessage(ctx));
            if (ctx.Match("log"))
                if (ctx.Match("channel"))
                    return ctx.Execute<ServerConfig>(LogChannel, m => m.SetLogChannel(ctx));
                else if (ctx.Match("enable", "on"))
                    return ctx.Execute<ServerConfig>(LogEnable, m => m.SetLogEnabled(ctx, true));
                else if (ctx.Match("disable", "off"))
                    return ctx.Execute<ServerConfig>(LogDisable, m => m.SetLogEnabled(ctx, false));
                else if (ctx.Match("commands"))
                    return PrintCommandList(ctx, "message logging", LogCommands);
                else return PrintCommandExpectedError(ctx, LogCommands);
            if (ctx.Match("logclean"))
                return ctx.Execute<ServerConfig>(LogClean, m => m.SetLogCleanup(ctx));
            if (ctx.Match("blacklist", "bl"))
                if (ctx.Match("enable", "on", "add", "deny"))
                    return ctx.Execute<ServerConfig>(BlacklistAdd, m => m.SetBlacklisted(ctx, true));
                else if (ctx.Match("disable", "off", "remove", "allow"))
                    return ctx.Execute<ServerConfig>(BlacklistRemove, m => m.SetBlacklisted(ctx, false));
                else if (ctx.Match("list", "show"))
                    return ctx.Execute<ServerConfig>(BlacklistShow, m => m.ShowBlacklisted(ctx));
                else if (ctx.Match("commands"))
                    return PrintCommandList(ctx, "channel blacklisting", BlacklistCommands);
                else return PrintCommandExpectedError(ctx, BlacklistCommands);
            if (ctx.Match("proxy"))
                if (ctx.Match("debug"))
                    return ctx.Execute<Checks>(ProxyCheck, m => m.MessageProxyCheck(ctx));
                else
                    return ctx.Execute<SystemEdit>(SystemProxy, m => m.SystemProxy(ctx));
            if (ctx.Match("invite")) return ctx.Execute<Misc>(Invite, m => m.Invite(ctx));
            if (ctx.Match("stats")) return ctx.Execute<Misc>(null, m => m.Stats(ctx));
            if (ctx.Match("permcheck"))
                return ctx.Execute<Checks>(PermCheck, m => m.PermCheckGuild(ctx));
            if (ctx.Match("proxycheck"))
                return ctx.Execute<Checks>(ProxyCheck, m => m.MessageProxyCheck(ctx));
            if (ctx.Match("debug"))
                return HandleDebugCommand(ctx);
            if (ctx.Match("random", "r"))
                return ctx.Execute<Random>(MemberRandom, m => m.Member(ctx));

            // remove compiler warning
            return ctx.Reply(
                $"{Emojis.Error} Unknown command {ctx.PeekArgument().AsCode()}. For a list of possible commands, see <https://pluralkit.me/commands>.");
        }

        private async Task HandleDebugCommand(Context ctx)
        {
            var availableCommandsStr = "Available debug targets: `permissions`, `proxying`";

            if (ctx.Match("permissions", "perms", "permcheck"))
                if (ctx.Match("channel", "ch"))
                    await ctx.Execute<Checks>(PermCheck, m => m.PermCheckChannel(ctx));
                else
                    await ctx.Execute<Checks>(PermCheck, m => m.PermCheckGuild(ctx));
            else if (ctx.Match("channel"))
                await ctx.Execute<Checks>(PermCheck, m => m.PermCheckChannel(ctx));
            else if (ctx.Match("proxy", "proxying", "proxycheck"))
                await ctx.Execute<Checks>(ProxyCheck, m => m.MessageProxyCheck(ctx));
            else if (!ctx.HasNext())
                await ctx.Reply($"{Emojis.Error} You need to pass a command. {availableCommandsStr}");
            else
                await ctx.Reply($"{Emojis.Error} Unknown debug command {ctx.PeekArgument().AsCode()}. {availableCommandsStr}");
        }

        private async Task HandleSystemCommand(Context ctx)
        {
            // If we have no parameters, default to self-target
            if (!ctx.HasNext())
                await ctx.Execute<System>(SystemInfo, m => m.Query(ctx, ctx.System));

            // First, we match own-system-only commands (ie. no target system parameter)
            else if (ctx.Match("new", "create", "make", "add", "register", "init", "n"))
                await ctx.Execute<System>(SystemNew, m => m.New(ctx));
            else if (ctx.Match("name", "rename", "changename"))
                await ctx.Execute<SystemEdit>(SystemRename, m => m.Name(ctx));
            else if (ctx.Match("tag"))
                await ctx.Execute<SystemEdit>(SystemTag, m => m.Tag(ctx));
            else if (ctx.Match("servertag"))
                await ctx.Execute<SystemEdit>(SystemServerTag, m => m.ServerTag(ctx));
            else if (ctx.Match("description", "desc", "bio"))
                await ctx.Execute<SystemEdit>(SystemDesc, m => m.Description(ctx));
            else if (ctx.Match("color", "colour"))
                await ctx.Execute<SystemEdit>(SystemColor, m => m.Color(ctx));
            else if (ctx.Match("banner", "splash", "cover"))
                await ctx.Execute<SystemEdit>(SystemBannerImage, m => m.BannerImage(ctx));
            else if (ctx.Match("avatar", "picture", "icon", "image", "pic", "pfp"))
                await ctx.Execute<SystemEdit>(SystemAvatar, m => m.Avatar(ctx));
            else if (ctx.Match("delete", "remove", "destroy", "erase", "yeet"))
                await ctx.Execute<SystemEdit>(SystemDelete, m => m.Delete(ctx));
            else if (ctx.Match("timezone", "tz"))
                await ctx.Execute<SystemEdit>(SystemTimezone, m => m.SystemTimezone(ctx));
            else if (ctx.Match("proxy"))
                await ctx.Execute<SystemEdit>(SystemProxy, m => m.SystemProxy(ctx));
            else if (ctx.Match("list", "l", "members"))
                await ctx.Execute<SystemList>(SystemList, m => m.MemberList(ctx, ctx.System));
            else if (ctx.Match("find", "search", "query", "fd", "s"))
                await ctx.Execute<SystemList>(SystemFind, m => m.MemberList(ctx, ctx.System));
            else if (ctx.Match("privacy"))
                await ctx.Execute<SystemEdit>(SystemPrivacy, m => m.SystemPrivacy(ctx));
            else if (ctx.Match("ping"))
                await ctx.Execute<SystemEdit>(SystemPing, m => m.SystemPing(ctx));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "systems", SystemCommands);
            else
                await HandleSystemCommandTargeted(ctx);
        }

        private async Task HandleSystemCommandTargeted(Context ctx)
        {
            // Commands that have a system target (eg. pk;system <system> fronthistory)
            var target = await ctx.MatchSystem();
            if (target == null)
            {
                var list = CreatePotentialCommandList(SystemInfo, SystemNew, SystemRename, SystemTag, SystemDesc, SystemAvatar, SystemDelete, SystemTimezone, SystemList, SystemFronter, SystemFrontHistory, SystemFrontPercent);
                await ctx.Reply(
                    $"{Emojis.Error} {await CreateSystemNotFoundError(ctx)}\n\nPerhaps you meant to use one of the following commands?\n{list}");
            }
            else if (ctx.Match("list", "l", "members"))
                await ctx.Execute<SystemList>(SystemList, m => m.MemberList(ctx, target));
            else if (ctx.Match("find", "search", "query", "fd", "s"))
                await ctx.Execute<SystemList>(SystemFind, m => m.MemberList(ctx, target));
            else if (ctx.Match("info", "view", "show"))
                await ctx.Execute<System>(SystemInfo, m => m.Query(ctx, target));
            else if (!ctx.HasNext())
                await ctx.Execute<System>(SystemInfo, m => m.Query(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, SystemList, SystemFronter, SystemFrontHistory, SystemFrontPercent,
                    SystemInfo);
        }

        private async Task HandleMemberCommand(Context ctx)
        {
            if (ctx.Match("new", "n", "add", "create", "register"))
                await ctx.Execute<Member>(MemberNew, m => m.NewMember(ctx));
            else if (ctx.Match("list"))
                await ctx.Execute<SystemList>(SystemList, m => m.MemberList(ctx, ctx.System));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "members", MemberCommands);
            else if (await ctx.MatchMember() is Chao target)
                await HandleMemberCommandTargeted(ctx, target);
            else if (!ctx.HasNext())
                await PrintCommandExpectedError(ctx, MemberNew, MemberInfo, MemberRename, MemberDisplayName, MemberServerName, MemberDesc, MemberPronouns,
                    MemberColor, MemberBirthday, MemberProxy, MemberDelete, MemberAvatar);
            else
                await ctx.Reply($"{Emojis.Error} {ctx.CreateMemberNotFoundError(ctx.PopArgument())}");
        }

        private async Task HandleMemberCommandTargeted(Context ctx, Chao target)
        {
            // Commands that have a member target (eg. pk;member <member> delete)
            if (ctx.Match("rename", "name", "changename", "setname"))
                await ctx.Execute<MemberEdit>(MemberRename, m => m.Name(ctx, target));
            else if (ctx.Match("description", "info", "bio", "text", "desc"))
                await ctx.Execute<MemberEdit>(MemberDesc, m => m.Description(ctx, target));
            else if (ctx.Match("pronouns", "pronoun"))
                await ctx.Execute<MemberEdit>(MemberPronouns, m => m.Pronouns(ctx, target));
            else if (ctx.Match("color", "colour"))
                await ctx.Execute<MemberEdit>(MemberColor, m => m.Color(ctx, target));
            else if (ctx.Match("birthday", "bday", "birthdate", "cakeday", "bdate"))
                await ctx.Execute<MemberEdit>(MemberBirthday, m => m.Birthday(ctx, target));
            else if (ctx.Match("delete", "remove", "destroy", "erase", "yeet"))
                await ctx.Execute<MemberEdit>(MemberDelete, m => m.Delete(ctx, target));
            else if (ctx.Match("avatar", "profile", "picture", "icon", "image", "pfp", "pic"))
                await ctx.Execute<MemberAvatar>(MemberAvatar, m => m.Avatar(ctx, target));
            else if (ctx.Match("banner", "splash", "cover"))
                await ctx.Execute<MemberEdit>(MemberBannerImage, m => m.BannerImage(ctx, target));
            else if (ctx.Match("serveravatar", "servericon", "serverimage", "serverpfp", "serverpic", "savatar", "spic", "guildavatar", "guildpic", "guildicon", "sicon"))
                await ctx.Execute<MemberAvatar>(MemberServerAvatar, m => m.ServerAvatar(ctx, target));
            else if (ctx.Match("displayname", "dn", "dname", "nick", "nickname", "dispname"))
                await ctx.Execute<MemberEdit>(MemberDisplayName, m => m.DisplayName(ctx, target));
            else if (ctx.Match("servername", "sn", "sname", "snick", "snickname", "servernick", "servernickname", "serverdisplayname", "guildname", "guildnick", "guildnickname", "serverdn"))
                await ctx.Execute<MemberEdit>(MemberServerName, m => m.ServerName(ctx, target));
            else if (ctx.Match("autoproxy", "ap"))
                await ctx.Execute<MemberEdit>(MemberAutoproxy, m => m.MemberAutoproxy(ctx, target));
            else if (ctx.Match("keepproxy", "keeptags", "showtags"))
                await ctx.Execute<MemberEdit>(MemberKeepProxy, m => m.KeepProxy(ctx, target));
            else if (ctx.Match("privacy"))
                await ctx.Execute<MemberEdit>(MemberPrivacy, m => m.Privacy(ctx, target, null));
            else if (ctx.Match("private", "hidden", "hide"))
                await ctx.Execute<MemberEdit>(MemberPrivacy, m => m.Privacy(ctx, target, PrivacyLevel.Private));
            else if (ctx.Match("public", "shown", "show"))
                await ctx.Execute<MemberEdit>(MemberPrivacy, m => m.Privacy(ctx, target, PrivacyLevel.Public));
            else if (ctx.Match("soulscream"))
                await ctx.Execute<Member>(MemberInfo, m => m.Soulscream(ctx, target));
            else if (!ctx.HasNext()) // Bare command
                await ctx.Execute<Member>(MemberInfo, m => m.ViewMember(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, MemberInfo, MemberRename, MemberDisplayName, MemberServerName, MemberDesc, MemberPronouns, MemberColor, MemberBirthday, MemberProxy, MemberDelete, MemberAvatar, SystemList);
        }

        private async Task CommandHelpRoot(Context ctx)
        {
            if (!ctx.HasNext())
            {
                await ctx.Reply($"Available command help targets: `system`, `member`, `group`, `switch`, `autoproxy`, `log`, `blacklist`."
                    + "\n- **pk;commands <target>** - *View commands related to a help target.*"
                    + "\n\nFor the full list of commands, see the website: <https://pluralkit.me/commands>");
                return;
            }

            switch (ctx.PeekArgument())
            {
                case "system":
                case "systems":
                case "s":
                    await PrintCommandList(ctx, "systems", SystemCommands);
                    break;
                case "member":
                case "members":
                case "m":
                    await PrintCommandList(ctx, "members", MemberCommands);
                    break;
                case "group":
                case "groups":
                case "g":
                    await PrintCommandList(ctx, "groups", GroupCommands);
                    break;
                case "switch":
                case "switches":
                case "switching":
                case "sw":
                    await PrintCommandList(ctx, "switching", SwitchCommands);
                    break;
                case "log":
                    await PrintCommandList(ctx, "message logging", LogCommands);
                    break;
                case "blacklist":
                case "bl":
                    await PrintCommandList(ctx, "channel blacklisting", BlacklistCommands);
                    break;
                case "autoproxy":
                case "ap":
                    await PrintCommandList(ctx, "autoproxy", AutoproxyCommands);
                    break;
                // todo: are there any commands that still need to be added?
                default:
                    await ctx.Reply("For the full list of commands, see the website: <https://pluralkit.me/commands>");
                    break;
            }
        }

        private async Task PrintCommandNotFoundError(Context ctx, params Command[] potentialCommands)
        {
            var commandListStr = CreatePotentialCommandList(potentialCommands);
            await ctx.Reply(
                $"{Emojis.Error} Unknown command `pk;{ctx.FullCommand().Truncate(100)}`. Perhaps you meant to use one of the following commands?\n{commandListStr}\n\nFor a full list of possible commands, see <https://pluralkit.me/commands>.");
        }

        private async Task PrintCommandExpectedError(Context ctx, params Command[] potentialCommands)
        {
            var commandListStr = CreatePotentialCommandList(potentialCommands);
            await ctx.Reply(
                $"{Emojis.Error} You need to pass a command. Perhaps you meant to use one of the following commands?\n{commandListStr}\n\nFor a full list of possible commands, see <https://pluralkit.me/commands>.");
        }

        private static string CreatePotentialCommandList(params Command[] potentialCommands)
        {
            return string.Join("\n", potentialCommands.Select(cmd => $"- **pk;{cmd.Usage}** - *{cmd.Description}*"));
        }

        private async Task PrintCommandList(Context ctx, string subject, params Command[] commands)
        {
            var str = CreatePotentialCommandList(commands);
            await ctx.Reply($"Here is a list of commands related to {subject}: \n{str}\nFor a full list of possible commands, see <https://pluralkit.me/commands>.");
        }

        private async Task<string> CreateSystemNotFoundError(Context ctx)
        {
            var input = ctx.PopArgument();
            if (input.TryParseMention(out var id))
            {
                // Try to resolve the user ID to find the associated account,
                // so we can print their username.
                var user = await ctx.Rest.GetUser(id);
                if (user != null)
                    return $"Account **{user.Username}#{user.Discriminator}** does not have a system registered.";
                else
                    return $"Account with ID `{id}` not found.";
            }

            return $"Garden with ID {input.AsCode()} not found.";
        }
    }
}