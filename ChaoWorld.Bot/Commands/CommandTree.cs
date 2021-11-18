using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using ChaoWorld.Core;
using System;

namespace ChaoWorld.Bot
{
    public class CommandTree
    {
        public static Command GardenInfo = new Command("garden", "garden {id}", "Looks up information about a garden; you can check your own without any parameters (e.g. `!garden`)");
        public static Command GardenNew = new Command("garden new", "garden new", "Creates a new garden");
        public static Command GardenList = new Command("garden list", "garden {id} list [full]", "Lists all chao in a garden");
        public static Command GardenFind = new Command("garden find", "garden {id} find [full] [search term]", "Searches a garden for chao given a search term");
        public static Command GardenRaise = new Command("garden raise", "garden raise {chao id/name}", "Selects a chao to use by default with certain commands");
        public static Command ChaoInfo = new Command("chao", "chao {id/name}", "Looks up information about a chao using either the name or ID");
        public static Command ChaoRankings = new Command("chao rankings", "chao rankings", "Lists chao across all gardens in order by their overall ability");
        public static Command ChaoPet = new Command("chao pet", "chao {id/name} pet", "Pets the specified chao");
        public static Command ChaoRock = new Command("chao rock", "chao {id/name} rock", "Rocks the specified chao in your arms");
        public static Command ChaoCuddle = new Command("chao cuddle", "chao {id/name} cuddle", "Cuddles the specified chao");
        public static Command ChaoRename = new Command("chao name", "chao {id/name} name {new name}", "Changes a chao's name");
        public static Command ChaoTag = new Command("chao tag", "chao {id/name} tag {emoji}", "Changes a chao's tag");
        public static Command ChaoBreed = new Command("chao breed", "chao {id/name} {id/name}", "Breeds the specified chao with one another (first chao must belong to your garden and must have eaten a heart fruit, and breeding between gardens requires user approval)");
        public static Command ChaoGoodbye = new Command("chao goodbye", "chao {id/name} goodbye", "Sends a chao to the forest forever");
        public static Command RaceInstanceList = new Command("race list", "race list [all/complete/incomplete]", "Lists all races in reverse chronological order, with optional filters");
        public static Command RaceInfo = new Command("race", "race {id/name}", "Looks up information about a race using either the name or ID");
        public static Command RaceJoin = new Command("race join", "race join {race id/name} [chao id/name]", "Joins a race with the specified chao (garden default is used if no chao is specified)");
        public static Command RaceLeave = new Command("race leave", "race leave", "Leaves a race your chao is currently waiting for (provided it hasn't started yet)");
        public static Command RacePings = new Command("race pings", "race pings {on/off}", "Updates account settings for pings on race completion (e.g. `!race pings on` will notify you when your chao completes a race");
        public static Command TournamentInstanceList = new Command("tournament list", "tournament list [all/complete/incomplete]", "Lists all tournaments in reverse chronological order, with optional filters");
        public static Command TournamentInfo = new Command("tournament", "tournament {id/name}", "Looks up information about a tournament using either the name or ID");
        public static Command TournamentJoin = new Command("tournament join", "tournament join {tournament id/name} [chao id/name]", "Joins a tournament with the specified chao (garden default is used if no chao is specified)");
        public static Command TournamentLeave = new Command("tournament leave", "tournament leave", "Leaves a tournament your chao is currently waiting for (provided it hasn't started yet)");
        public static Command TournamentPings = new Command("tournament pings", "tournament pings {on/off}", "Updates account settings for pings on completion of tournament matches (e.g. `!tournament pings on` will notify you when your chao completes a match");
        public static Command ItemList = new Command("item list", "item list", "Lists all items in your inventory");
        public static Command ItemInfo = new Command("item info", "item info {id/name}", "Displays information about an item and its uses");
        public static Command ItemUse = new Command("item use", "item use {item id/name} [chao id/name]", "Uses the specified item in your inventory (chao target is only used for certain items)");
        public static Command ItemGive = new Command("item give", "item give {item id/name} {@user}", "Offers the specified item in your inventory to another player (target can accept or reject the offer)");
        public static Command ItemSell = new Command("item sell", "item sell {item id/name} [qty]", "Sells the specified item from your inventory on the Black Market (quantity of 1 is assumed if not provided)");
        //public static Command ItemDiscard = new Command("item discard", "item {item id/name} discard", "Discards the specified item from your inventory");
        public static Command MarketList = new Command("market list", "market list", "Lists all items for sale at the Black Market");
        public static Command MarketBuy = new Command("market buy", "market buy {id/name} [qty]", "Purchases the specified item from the Black Market (quantity of 1 is assumed if not provided)");
        public static Command MarketSell = new Command("market sell", "market sell {id/name} [qty]", "Sells the specified item from your inventory on the Black Market (quantity of 1 is assumed if not provided)");
        public static Command TreeInfo = new Command("tree info", "tree info {id/name}", "Looks up information about one of your trees using either the name or ID");
        public static Command TreeList = new Command("tree list", "tree list", "Lists all trees in your orchard and their health");
        public static Command TreeWater = new Command("tree water", "tree water {id/name}", "Waters a tree in your orchard, improving its overall health when done at the proper time");
        public static Command TreeRemove = new Command("tree remove", "tree remove {id/name}", "Removes a tree from your orchard to make room for another one (each garden can have up to 7 at at time");
        public static Command GiveItem = new Command("give item", "give item {id/name} {@user}", "Offers the specified item in your inventory to another player (target can accept or reject the offer)");
        public static Command GiveRings = new Command("give rings", "give rings {qty} {@user}", "Offers the specified amount of rings to another player (target can accept or reject the offer)");
        public static Command Collect = new Command("collect", "collect", "Can be used every 24 hours to collect rings for use in the market");
        public static Command SlotsPlay = new Command("slots", "slots play", "Try your luck with the chao slots and win big money!");
        public static Command SlotsJackpot = new Command("slots", "slots jackpot", "Check the current slots jackpot amount");
        public static Command Help = new Command("help", "help", "Shows help information about Chao World");
        public static Command Admin = new Command("admin", "admin", "What? Nothing to see here...");

        public static Command[] GardenCommands = {
            GardenInfo, GardenNew, GardenList, GardenRaise
        };

        public static Command[] ChaoCommands = {
            ChaoInfo, ChaoRename, ChaoGoodbye, ChaoPet, ChaoRock, ChaoCuddle, ChaoRankings, ChaoBreed
        };

        public static Command[] RaceCommands =
        {
            RaceInstanceList, RaceInfo, RaceJoin, RaceLeave, RacePings
        };

        public static Command[] TournamentCommands =
        {
            TournamentInstanceList, TournamentInfo, TournamentJoin, TournamentLeave, TournamentPings
        };

        public static Command[] ItemCommands =
        {
            ItemList, ItemUse, ItemInfo, ItemGive, ItemSell
        };

        public static Command[] MarketCommands =
        {
            MarketList, MarketBuy, MarketSell
        };

        public static Command[] TreeCommands =
        {
            TreeInfo, TreeList, TreeWater, TreeRemove
        };

        public static Command[] GiveCommands =
        {
            GiveItem, GiveRings
        };

        public static Command[] SlotsCommands =
        {
            SlotsPlay, SlotsJackpot
        };

        public Task ExecuteCommand(Context ctx)
        {
            if (ctx.Match("garden", "g", "gardens"))
                return HandleGardenCommand(ctx);
            if (ctx.Match("chao", "c"))
                return HandleChaoCommand(ctx);
            if (ctx.Match("race", "r", "races"))
                return HandleRaceCommand(ctx);
            if (ctx.Match("tournament", "t", "tourney", "tourneys", "karate", "k"))
                return HandleTournamentCommand(ctx);
            if (ctx.Match("item", "i", "items"))
                return HandleItemCommand(ctx);
            if (ctx.Match("market", "m"))
                return HandleMarketCommand(ctx);
            if (ctx.Match("tree", "trees", "plants", "orchard", "o"))
                return HandleTreeCommand(ctx);
            if (ctx.Match("give"))
                return HandleGiveCommand(ctx);
            if (ctx.Match("commands", "cmd", "command"))
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
            if (ctx.Match("slots"))
                if (ctx.Match("play"))
                    return ctx.Execute<Misc>(SlotsPlay, m => m.PlaySlots(ctx));
                else if (ctx.Match("jackpot"))
                    return ctx.Execute<Misc>(SlotsJackpot, m => m.SeeJackpot(ctx));
                else if (ctx.Match("simulate"))
                    return ctx.Execute<Misc>(SlotsPlay, m => m.SimulateSlots(ctx));
                else
                    PrintCommandExpectedError(ctx, SlotsCommands);

            // remove compiler warning
            return ctx.Reply($"{Emojis.Error} Unknown command {ctx.PeekArgument().AsCode()}. For a full list of commands, see: https://bytebarcafe.com/chao/commands.php");
        }

        private async Task HandleGardenCommand(Context ctx)
        {
            // If we have no parameters, default to self-target
            if (!ctx.HasNext())
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, ctx.Garden));

            // First, we match own-garden-only commands (ie. no target garden parameter)
            else if (ctx.Match("new", "create", "make", "add", "register", "init", "n"))
                await ctx.Execute<Garden>(GardenNew, m => m.New(ctx));
            else if (ctx.Match("list", "l", "chao"))
                await ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("find", "search", "query", "fd", "s"))
                await ctx.Execute<GardenList>(GardenFind, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("select", "active", "raise", "pick", "use", "default", "main"))
                await ctx.Execute<Garden>(GardenRaise, m => m.ChangeActiveChao(ctx, ctx.Garden));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "gardens", GardenCommands);
            else
                // We've exhausted those options - try commands with a target garden parameter
                await HandleGardenCommandTargeted(ctx);
        }

        private async Task HandleGardenCommandTargeted(Context ctx)
        {
            // Commands that have a garden target (eg. !garden <id>)
            var target = await ctx.MatchGarden();
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
            else if (ctx.Match("select", "active", "raise", "pick", "use", "default", "main"))
                await ctx.Execute<Garden>(GardenRaise, m => m.ChangeActiveChao(ctx, ctx.Garden));
            else if (ctx.Match("info", "view", "show"))
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, target));
            else if (!ctx.HasNext())
                await ctx.Execute<Garden>(GardenInfo, m => m.Query(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, GardenList, GardenInfo, GardenRaise);
        }

        private async Task HandleChaoCommand(Context ctx)
        {
            if (ctx.Match("list") || !ctx.HasNext())
                await ctx.Execute<GardenList>(GardenList, m => m.ChaoList(ctx, ctx.Garden));
            else if (ctx.Match("commands", "help"))
                await PrintCommandList(ctx, "chao", ChaoCommands);
            else if (ctx.Match("rank", "ranking", "rankings", "top", "leaderboard"))
                await ctx.Execute<Chao>(ChaoRankings, m => m.ChaoRankings(ctx));
            else if (ctx.Match("rename", "name", "changename", "setname"))
                if (await ctx.MatchChao() is Core.Chao target)
                    await ctx.Execute<ChaoEdit>(ChaoRename, m => m.Name(ctx, target));
                else
                    await PrintCommandExpectedError(ctx, ChaoRename);
            else if (ctx.Match("tag", "settag", "emoji", "setemoji"))
                if (await ctx.MatchChao() is Core.Chao target)
                    await ctx.Execute<ChaoEdit>(ChaoTag, m => m.Tag(ctx, target));
                else
                    await PrintCommandExpectedError(ctx, ChaoTag);
            else if (ctx.Match("pet"))
                if (await ctx.MatchChao() is Core.Chao petTarget)
                    await ctx.Execute<Chao>(ChaoPet, m => m.PetChao(ctx, petTarget));
                else
                    await PrintCommandExpectedError(ctx, ChaoPet);
            else if (ctx.Match("rock"))
                if (await ctx.MatchChao() is Core.Chao rockTarget)
                    await ctx.Execute<Chao>(ChaoRock, m => m.RockChao(ctx, rockTarget));
                else
                    await PrintCommandExpectedError(ctx, ChaoRock);
            else if (ctx.Match("cuddle"))
                if (await ctx.MatchChao() is Core.Chao cuddleTarget)
                    await ctx.Execute<Chao>(ChaoCuddle, m => m.CuddleChao(ctx, cuddleTarget));
                else
                    await PrintCommandExpectedError(ctx, ChaoCuddle);
            else if (ctx.Match("breed", "mate"))
                if (await ctx.MatchChao() is Core.Chao breedTarget)
                    await ctx.Execute<Chao>(ChaoBreed, m => m.Breed(ctx, breedTarget));
                else
                    await PrintCommandExpectedError(ctx, ChaoBreed);
            else if (await ctx.MatchChao() is Core.Chao target)
                await HandleChaoCommandTargeted(ctx, target);
            else
                await ctx.Reply($"{Emojis.Error} {ctx.CreateChaoNotFoundError(ctx.PopArgument())}");
        }

        private async Task HandleChaoCommandTargeted(Context ctx, Core.Chao target)
        {
            // Commands that have a chao target (eg. !chao <chao> delete)
            if (ctx.Match("rename", "name", "changename", "setname"))
                await ctx.Execute<ChaoEdit>(ChaoRename, m => m.Name(ctx, target));
            else if (ctx.Match("tag", "settag", "emoji", "setemoji"))
                await ctx.Execute<ChaoEdit>(ChaoTag, m => m.Tag(ctx, target));
            else if (ctx.Match("delete", "remove", "destroy", "erase", "yeet", "depart", "goodbye", "farewell"))
                await ctx.Execute<ChaoEdit>(ChaoGoodbye, m => m.Delete(ctx, target));
            else if (ctx.Match("pet"))
                await ctx.Execute<Chao>(ChaoPet, m => m.PetChao(ctx, target));
            else if (ctx.Match("rock"))
                await ctx.Execute<Chao>(ChaoRock, m => m.RockChao(ctx, target));
            else if (ctx.Match("cuddle"))
                await ctx.Execute<Chao>(ChaoCuddle, m => m.CuddleChao(ctx, target));
            else if (ctx.Match("breed", "mate"))
                await ctx.Execute<Chao>(ChaoBreed, m => m.Breed(ctx, target));
            else if (!ctx.HasNext()) // Bare command
                await ctx.Execute<Chao>(ChaoInfo, m => m.ViewChao(ctx, target));
            else
                await PrintCommandNotFoundError(ctx, ChaoInfo, ChaoRename, ChaoGoodbye, GardenList);
        }

        private async Task HandleRaceCommand(Context ctx)
        {
            if (ctx.Match("list", "l") || !ctx.HasNext())
                await ctx.Execute<RaceList>(RaceInstanceList, m => m.RaceInstanceList(ctx));
            else if (ctx.Match("withdraw", "leave", "quit", "abandon", "cancel"))
                await ctx.Execute<Race>(RaceLeave, m => m.LeaveRace(ctx));
            else if (ctx.Match("ping", "pings", "notify"))
                await ctx.Execute<Race>(RacePings, m => m.UpdatePingSettings(ctx));
            else if (ctx.Match("commands", "help", "h"))
                await PrintCommandList(ctx, "races", RaceCommands);
            else if (ctx.Match("enter", "join", "j")) // !race join x x
            {
                if (await ctx.MatchRaceInstance() is { } raceInstanceTarget)
                    if (ctx.HasNext())
                        if (await ctx.MatchChao(ctx.Garden.Id) is { } chaoTarget)
                            await ctx.Execute<Race>(RaceJoin, m => m.EnterChaoInRace(ctx, chaoTarget, raceInstanceTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find a chao using identifier {ctx.RemainderOrNull()}");
                    else if (ctx.Garden.ActiveChao.HasValue)
                        if (await ctx.Repository.GetActiveChaoForGarden(ctx.Garden.Id.Value) is { } chaoTarget)
                            await ctx.Execute<Race>(RaceJoin, m => m.EnterChaoInRace(ctx, chaoTarget, raceInstanceTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!race {{race id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                    else
                        await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!race {{race id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                else
                    await PrintCommandNotFoundError(ctx, RaceCommands);
            }
            else if (await ctx.MatchRaceInstance() is { } raceInstanceTarget)
                if (ctx.Match("info", "i")) // !race x info
                    await ctx.Execute<Race>(RaceInfo, m => m.ViewRaceInstance(ctx, raceInstanceTarget));
                else if (ctx.Match("enter", "join", "j")) // !race x join x
                {
                    if (ctx.HasNext())
                        if (await ctx.MatchChao(ctx.Garden.Id) is { } chaoTarget)
                            await ctx.Execute<Race>(RaceJoin, m => m.EnterChaoInRace(ctx, chaoTarget, raceInstanceTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find a chao using identifier {ctx.RemainderOrNull()}");
                    else if (ctx.Garden.ActiveChao.HasValue)
                        if (await ctx.Repository.GetActiveChaoForGarden(ctx.Garden.Id.Value) is { } chaoTarget)
                            await ctx.Execute<Race>(RaceJoin, m => m.EnterChaoInRace(ctx, chaoTarget, raceInstanceTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!race {{race id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                    else
                        await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!race {{race id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                }
                else
                    await ctx.Execute<Race>(RaceInfo, m => m.ViewRaceInstance(ctx, raceInstanceTarget));
            //else if (!ctx.HasNext())
            //    await ctx.Execute<RaceList>(RaceInstanceList, m => m.RaceInstanceList(ctx));
            else
                await PrintCommandNotFoundError(ctx, RaceCommands);
        }

        private async Task HandleTournamentCommand(Context ctx)
        {
            if (ctx.Match("list", "l") || !ctx.HasNext())
                await ctx.Execute<TournamentList>(TournamentInstanceList, m => m.TournamentInstanceList(ctx));
            else if (ctx.Match("withdraw", "leave", "quit", "abandon", "cancel"))
                await ctx.Execute<Tournament>(TournamentLeave, m => m.LeaveTournament(ctx));
            else if (ctx.Match("ping", "pings", "notify"))
                await ctx.Execute<Tournament>(TournamentPings, m => m.UpdatePingSettings(ctx));
            else if (ctx.Match("commands", "help", "h"))
                await PrintCommandList(ctx, "tournaments", TournamentCommands);
            else if (ctx.Match("enter", "join", "j")) // !tournament x join x
            {
                if (await ctx.MatchTournamentInstance() is { } tournamentTarget)
                {
                    if (ctx.HasNext())
                        if (await ctx.MatchChao(ctx.Garden.Id) is { } chaoTarget)
                            await ctx.Execute<Tournament>(TournamentJoin, m => m.JoinTournament(ctx, chaoTarget, tournamentTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find a chao using identifier {ctx.RemainderOrNull()}");
                    else if (ctx.Garden.ActiveChao.HasValue)
                        if (await ctx.Repository.GetActiveChaoForGarden(ctx.Garden.Id.Value) is { } chaoTarget)
                            await ctx.Execute<Tournament>(TournamentJoin, m => m.JoinTournament(ctx, chaoTarget, tournamentTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!tournament {{tournament id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                    else
                        await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!tournament {{tournament id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                }
                else
                    await PrintCommandNotFoundError(ctx, TournamentCommands);
            }
            else if (await ctx.MatchTournamentInstance() is { } tournamentTarget)
            {
                if (ctx.Match("join", "j")) // !tournament x join x
                {
                    if (ctx.HasNext())
                        if (await ctx.MatchChao(ctx.Garden.Id) is { } chaoTarget)
                            await ctx.Execute<Tournament>(TournamentJoin, m => m.JoinTournament(ctx, chaoTarget, tournamentTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find a chao using identifier {ctx.RemainderOrNull()}");
                    else if (ctx.Garden.ActiveChao.HasValue)
                        if (await ctx.Repository.GetActiveChaoForGarden(ctx.Garden.Id.Value) is { } chaoTarget)
                            await ctx.Execute<Tournament>(TournamentJoin, m => m.JoinTournament(ctx, chaoTarget, tournamentTarget));
                        else
                            await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!tournament {{tournament id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                    else
                        await ctx.Reply($"{Emojis.Error} Couldn't find an active chao for the garden. Please specify a chao (e.g. `!tournament {{tournament id/name}} join {{chao id/name}}` or use `!garden raise {{chao id/name}}` first to select a default chao.");
                }
                else
                {
                    await ctx.Execute<Tournament>(TournamentInfo, m => m.ViewTournamentInstance(ctx, tournamentTarget));
                }
            }
            else if (!ctx.HasNext())
                await ctx.Execute<TournamentList>(TournamentInstanceList, m => m.TournamentInstanceList(ctx));
            else
                await PrintCommandNotFoundError(ctx, TournamentCommands);
        }

        private async Task HandleItemCommand(Context ctx)
        {
            if (ctx.Match("list", "l") || !ctx.HasNext())
                await ctx.Execute<ItemList>(ItemList, m => m.InventoryItemList(ctx));
            else if (ctx.Match("commands", "help", "h"))
                await PrintCommandList(ctx, "items", ItemCommands);
            else if (ctx.Match("use", "u"))
                if (await ctx.MatchInventoryItem() is { } itemTarget)
                    await ctx.Execute<Item>(ItemUse, m => m.UseItem(ctx, itemTarget));
                else
                    await ctx.Reply($"{Emojis.Error} Unable to find the specified item in your inventory.");
            else if (ctx.Match("info", "i"))
                if (await ctx.MatchItemType() is { } infoTarget)
                    await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, infoTarget));
                else
                    await PrintCommandExpectedError(ctx, ItemInfo);
            else if (ctx.Match("give", "g"))
                if (await ctx.MatchInventoryItem() is { } giveItemTarget)
                    await ctx.Execute<Item>(ItemGive, m => m.GiveItem(ctx, giveItemTarget));
                else
                    await PrintCommandExpectedError(ctx, ItemGive);
            else if (ctx.Match("sell", "s"))
                if (await ctx.MatchInventoryItem() is { } sellItemTarget)
                    await ctx.Execute<Item>(ItemSell, m => m.SellItem(ctx, sellItemTarget));
                else
                    await PrintCommandExpectedError(ctx, ItemSell);
            else if (await ctx.MatchInventoryItem() is { } itemTarget)
                if (ctx.Match("use", "u"))
                    await ctx.Execute<Item>(ItemUse, m => m.UseItem(ctx, itemTarget));
                else if (ctx.Match("give", "g"))
                    await ctx.Execute<Item>(ItemGive, m => m.GiveItem(ctx, itemTarget));
                else if (ctx.Match("sell", "s"))
                    await ctx.Execute<Item>(ItemSell, m => m.SellItem(ctx, itemTarget));
                else
                    await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, itemTarget));
            else if (await ctx.MatchItemType() is { } itemType)
                await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, itemType));
            else
                await PrintCommandNotFoundError(ctx, ItemCommands);
        }

        private async Task HandleMarketCommand(Context ctx)
        {
            if (ctx.Match("list", "l") || !ctx.HasNext())
                await ctx.Execute<ItemList>(MarketList, m => m.MarketItemList(ctx));
            else if (ctx.Match("commands", "help", "h"))
                await PrintCommandList(ctx, "market", MarketCommands);
            else if (ctx.Match("info"))
                if (await ctx.MatchItemType() is { } infoTarget)
                    await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, infoTarget));
                else
                    await PrintCommandExpectedError(ctx, ItemInfo);
            else if (ctx.Match("buy", "b"))
                if (await ctx.MatchMarketItem() is { } marketBuyTarget)
                    await ctx.Execute<Item>(MarketBuy, m => m.BuyItem(ctx, marketBuyTarget));
                else
                    await PrintCommandExpectedError(ctx, MarketBuy);
            else if (ctx.Match("sell", "s"))
                if (await ctx.MatchInventoryItem() is { } marketSellTarget)
                    await ctx.Execute<Item>(MarketSell, m => m.SellItem(ctx, marketSellTarget));
                else
                    await PrintCommandExpectedError(ctx, MarketSell);
            else if (await ctx.MatchMarketItem() is { } marketTargetBuy)
                if (ctx.Match("buy", "b"))
                    await ctx.Execute<Item>(MarketBuy, m => m.BuyItem(ctx, marketTargetBuy));
                else
                    await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, marketTargetBuy));
            else if (await ctx.MatchItemType() is { } itemTarget)
                await ctx.Execute<Item>(ItemInfo, m => m.ItemInfo(ctx, itemTarget));
            else
                await PrintCommandNotFoundError(ctx, MarketCommands);
        }

        private async Task HandleTreeCommand(Context ctx)
        {
            
        }

        private async Task HandleGiveCommand(Context ctx)
        {
            if (ctx.Match("item", "i"))
                if (await ctx.MatchInventoryItem() is { } giveItemTarget)
                    await ctx.Execute<Item>(GiveItem, m => m.GiveItem(ctx, giveItemTarget));
                else
                    await PrintCommandExpectedError(ctx, GiveItem);
            else if (ctx.Match("rings", "r"))
                await ctx.Execute<Garden>(GiveRings, m => m.GiveRings(ctx));
            else
                PrintCommandNotFoundError(ctx, GiveCommands);
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
                case "race":
                case "r":
                    await PrintCommandList(ctx, "races", RaceCommands);
                    break;
                case "item":
                case "items":
                case "i":
                    await PrintCommandList(ctx, "items", ItemCommands);
                    break;
                case "market":
                case "m":
                    await PrintCommandList(ctx, "market", MarketCommands);
                    break;
                // TODO: Add help for tournament and misc commands
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