using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class CommandTree
    {
        public static Command GardenInfo = new Command("garden", "garden {id}", "Looks up information about a garden; you can check your own without any parameters (e.g. `!garden`)");
        public static Command GardenNew = new Command("garden new", "garden new", "Creates a new garden");
        public static Command GardenList = new Command("garden list", "garden {id} list [full]", "Lists all chao in a garden");
        public static Command GardenFind = new Command("garden find", "garden {id} find [full] [search term]", "Searches a garden for chao given a search term");
        public static Command ChaoInfo = new Command("chao", "chao {id/name}", "Looks up information about a chao using either the name or ID");
        public static Command ChaoNew = new Command("chao new", "chao new", "Creates a new chao"); //TODO: Remove this when the market is done
        public static Command ChaoRename = new Command("chao name", "chao {id/name} name {new name}", "Changes a chao's name");
        public static Command ChaoGoodbye = new Command("chao goodbye", "chao {id/name} goodbye", "Sends a chao to the forest forever");
        public static Command RaceInstanceList = new Command("race list", "race list [all/complete/incomplete]", "Lists all races in reverse chronological order");
        public static Command RaceInfo = new Command("race {id/name}", "race {id/name}", "Looks up information about a race using either the name or ID");
        public static Command RaceJoin = new Command("race join", "race {race id/name} join {chao id/name}", "Joins a race with the specified chao");
        public static Command Collect = new Command("collect", "collect", "Can be used every 24 hours to collect rings for use in the market");
        public static Command Help = new Command("help", "help", "Shows help information about Chao World");
        public static Command Admin = new Command("admin", "admin", "What? Nothing to see here...");

        public static Command[] GardenCommands = {
            GardenInfo, GardenNew, GardenList
        };

        public static Command[] ChaoCommands = {
            ChaoInfo, ChaoNew, ChaoRename, ChaoGoodbye
        };

        public Task ExecuteCommand(Context ctx)
        {
            if (ctx.Match("garden", "g"))
                return HandleGardenCommand(ctx);
            if (ctx.Match("chao", "c"))
                return HandleChaoCommand(ctx);
            if (ctx.Match("race", "r"))
                return HandleRaceCommand(ctx);
            if (ctx.Match("commands", "cmd"))
                return CommandHelpRoot(ctx);
            if (ctx.Match("list", "find", "chao", "search", "query", "l", "f", "fd"))
                return ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, ctx.Garden));
            if (ctx.Match("help"))
                if (ctx.Match("commands"))
                    return ctx.Reply("For a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
                else return ctx.Execute<Help>(Help, m => m.HelpRoot(ctx));
            if (ctx.Match("collect", "explore", "daily", "gather"))
                return ctx.Execute<Misc>(null, m => m.Collect(ctx));
            if (ctx.Match("stats"))
                return ctx.Execute<Misc>(null, m => m.Stats(ctx));

            // remove compiler warning
            return ctx.Reply(
                $"{Emojis.Error} Unknown command {ctx.PeekArgument().AsCode()}. For a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
        }

        private async Task HandleGardenCommand(Context ctx)
        {
            // If we have no parameters, default to self-target
            if (!ctx.HasNext())
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, ctx.Garden));

            // First, we match own-system-only commands (ie. no target system parameter)
            else if (ctx.Match("new", "create", "make", "add", "register", "init", "n"))
                await ctx.Execute<Garden>(GardenNew, m => m.New(ctx));
            else if (ctx.Match("list", "l", "chao"))
                await ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("find", "search", "query", "fd", "s"))
                await ctx.Execute<GardenList>(GardenFind, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "gardens", GardenCommands);
            else
                await HandleGardenCommandTargeted(ctx);
        }

        private async Task HandleGardenCommandTargeted(Context ctx)
        {
            // Commands that have a garden target (eg. !garden <id>)
            var target = await ctx.MatchSystem();
            if (target == null)
            {
                var list = CreatePotentialCommandList(GardenInfo, GardenNew, GardenList);
                await ctx.Reply(
                    $"{Emojis.Error} {await CreateSystemNotFoundError(ctx)}\n\nPerhaps you meant to use one of the following commands?\n{list}");
            }
            else if (ctx.Match("list", "l", "chao"))
                await ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, target));
            else if (ctx.Match("find", "search", "query", "fd", "s"))
                await ctx.Execute<GardenList>(GardenFind, m => m.ChaoList(ctx, target));
            else if (ctx.Match("info", "view", "show"))
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, target));
            else if (!ctx.HasNext())
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, GardenList, GardenInfo);
        }

        private async Task HandleChaoCommand(Context ctx)
        {
            /*if (ctx.Match("new", "n", "add", "create", "register"))
                await ctx.Execute<Chao>(ChaoNew, m => m.NewChao(ctx));*/
            if (ctx.Match("list"))
                await ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "chao", ChaoCommands);
            else if (await ctx.MatchChao() is Core.Chao target)
                await HandleChaoCommandTargeted(ctx, target);
            else if (!ctx.HasNext())
                await PrintCommandExpectedError(ctx, ChaoNew, ChaoInfo, ChaoRename, ChaoGoodbye);
            else
                await ctx.Reply($"{Emojis.Error} {ctx.CreateChaoNotFoundError(ctx.PopArgument())}");
        }

        private async Task HandleChaoCommandTargeted(Context ctx, Core.Chao target)
        {
            // Commands that have a chao target (eg. !chao <chao> delete)
            if (ctx.Match("rename", "name", "changename", "setname"))
                await ctx.Execute<ChaoEdit>(ChaoRename, m => m.Name(ctx, target));
            else if (ctx.Match("delete", "remove", "destroy", "erase", "yeet", "depart", "goodbye", "farewell"))
                await ctx.Execute<ChaoEdit>(ChaoGoodbye, m => m.Delete(ctx, target));
            else if (!ctx.HasNext()) // Bare command
                await ctx.Execute<Chao>(ChaoInfo, m => m.ViewChao(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, ChaoInfo, ChaoRename, ChaoGoodbye, GardenList);
        }

        private async Task HandleRaceCommand(Context ctx)
        {
            if (ctx.Match("list", "l"))
                await ctx.Execute<RaceList>(RaceInstanceList, m => m.RaceInstanceList(ctx));
            //else if (ctx.Match("commands", "help))
            //  await PrintCommandList(ctx, "races", RaceCommands);
            else if (await ctx.MatchRaceInstance() is { } raceInstanceTarget)
            {
                if (ctx.Match("info"))
                {
                    await ctx.Execute<Race>(RaceInfo, m => m.ViewRaceInstance(ctx, raceInstanceTarget));
                }
                else if (ctx.Match("join")) {
                    //ctx.PopArgument();
                    if (await ctx.MatchChao() is { } chaoTarget)
                        await ctx.Execute<Race>(RaceJoin, m => m.EnterChaoInRace(ctx, chaoTarget, raceInstanceTarget));
                    else
                        await ctx.Reply($"{Emojis.Error} Couldn't find a chao using identifier {ctx.RemainderOrNull()}");
                }
                else
                {
                    await ctx.Execute<Race>(RaceInfo, m => m.ViewRaceInstance(ctx, raceInstanceTarget));
                }
            }
            //else if (!ctx.HasNext())
                //await PrintCommandExpectedError(ctx, RaceCommands);
            else
                await PrintCommandNotFoundError(ctx, RaceInstanceList, RaceInfo, RaceJoin);
        }

        private async Task CommandHelpRoot(Context ctx)
        {
            if (!ctx.HasNext())
            {
                await ctx.Reply($"Available command help targets: `garden`, `chao`, `collect`, `race`, `tournament`, `market`, `item`"
                    + "\n- **!commands {target}** - *View commands related to a help target.*"
                    + "\n\nFor a full list of commands, see: https://bytebarcafe.com/chao/commands.php"
                    + "\nIf you have any questions, just ask!");
                return;
            }

            switch (ctx.PeekArgument())
            {
                case "garden":
                case "gardens":
                case "g":
                    await PrintCommandList(ctx, "gardens", GardenCommands);
                    break;
                case "chao":
                case "c":
                    await PrintCommandList(ctx, "chao", ChaoCommands);
                    break;
                // TODO: Add help for race, tournament, market, item commands
                default:
                    await ctx.Reply("For a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
                    break;
            }
        }

        private async Task PrintCommandNotFoundError(Context ctx, params Command[] potentialCommands)
        {
            var commandListStr = CreatePotentialCommandList(potentialCommands);
            await ctx.Reply(
                $"{Emojis.Error} Unknown command `!{ctx.FullCommand().Truncate(100)}`. Did you mean to use one of the following commands?\n{commandListStr}\n\nFor a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
        }

        private async Task PrintCommandExpectedError(Context ctx, params Command[] potentialCommands)
        {
            var commandListStr = CreatePotentialCommandList(potentialCommands);
            await ctx.Reply(
                $"{Emojis.Error} You need to pass a command. Did you mean to use one of the following commands?\n{commandListStr}\n\nFor a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
        }

        private static string CreatePotentialCommandList(params Command[] potentialCommands)
        {
            return string.Join("\n", potentialCommands.Select(cmd => $"- **!{cmd.Usage}** - *{cmd.Description}*"));
        }

        private async Task PrintCommandList(Context ctx, string subject, params Command[] commands)
        {
            var str = CreatePotentialCommandList(commands);
            await ctx.Reply($"Here is a list of commands related to {subject}: \n{str}\nFor a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
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
                    return $"Account **{user.Username}#{user.Discriminator}** does not have a garden.";
                else
                    return $"Account with ID `{id}` not found.";
            }

            return $"Garden with ID {input.AsCode()} not found.";
        }
    }
}