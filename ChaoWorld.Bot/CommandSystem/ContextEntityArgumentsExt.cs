using System;
using System.Threading.Tasks;

using Myriad.Extensions;
using Myriad.Types;

using ChaoWorld.Bot.Utils;
using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public static class ContextEntityArgumentsExt
    {
        public static async Task<User> MatchUser(this Context ctx)
        {
            var text = ctx.PeekArgument();
            if (text.TryParseMention(out var id))
                return await ctx.Cache.GetOrFetchUser(ctx.Rest, id);

            return null;
        }

        public static bool MatchUserRaw(this Context ctx, out ulong id)
        {
            id = 0;

            var text = ctx.PeekArgument();
            if (text.TryParseMention(out var mentionId))
                id = mentionId;

            return id != 0;
        }

        public static Task<Core.Garden> PeekSystem(this Context ctx) => ctx.MatchSystemInner();

        public static async Task<Core.Garden> MatchSystem(this Context ctx)
        {
            var system = await ctx.MatchSystemInner();
            if (system != null) ctx.PopArgument();
            return system;
        }

        private static async Task<Core.Garden> MatchSystemInner(this Context ctx)
        {
            var input = ctx.PeekArgument();

            // Garden references can take three forms:
            // - The direct user ID of an account connected to the garden
            // - A @mention of an account connected to the garden (<@uid>)
            // - A garden id

            // Direct IDs and mentions are both handled by the below method:
            if (input.TryParseMention(out var uid))
            {
                var gardenByAccount = await ctx.Repository.GetGardenByAccount(uid);
                if (gardenByAccount != null)
                    return gardenByAccount;

                // Try looking it up by garden id if that doesn't work
                if (long.TryParse(input, out long gardenId))
                    return await ctx.Repository.GetGarden(gardenId);
            }

            return null; //TODO: Is this safe?
        }

        public static async Task<Core.Chao> PeekChao(this Context ctx, GardenId? restrictToSystem = null)
        {
            var input = ctx.PeekArgument();

            // Chao references can have one of three forms, depending on
            // whether you're in a garden or not:
            // - A chao id
            // - A textual name of a chao *in your own system*
            // - a textual name of a chao *in another system*

            // First, if we have a garden, try finding by chao name in garden
            if (ctx.Member != null && ctx.Garden != null && await ctx.Repository.GetChaoByName(ctx.Garden.Id, input) is Core.Chao chaoByName)
                return chaoByName;

            // Then try by name across all gardens
            if (await ctx.Repository.GetChaoByName(input) is Core.Chao globalChaoByName)
                return globalChaoByName;

            // Try looking it up by its ID
            long.TryParse(input.Replace("#", string.Empty), out long id);
            if (ctx.Member != null && await ctx.Repository.GetChao(id) is Core.Chao chaoById)
                return chaoById;

            // We didn't find anything, so we return null.
            return null;
        }

        /// <summary>
        /// Attempts to pop a chao descriptor from the stack, returning it if present. If a chao could not be
        /// resolved by the next word in the argument stack, does *not* touch the stack, and returns null.
        /// </summary>
        public static async Task<Core.Chao> MatchChao(this Context ctx, GardenId? restrictToSystem = null)
        {
            // First, peek a chao
            var chao = await ctx.PeekChao(restrictToSystem);

            // If the peek was successful, we've used up the next argument, so we pop that just to get rid of it.
            if (chao != null) ctx.PopArgument();

            // Finally, we return the chao value.
            return chao;
        }

        public static async Task<RaceInstance> PeekRaceInstance(this Context ctx)
        {
            var input = ctx.PeekArgument();

            // Race instances might be referenced by their ID or by the name of the associated race
            // e.g. "!race 123" or "!race 'Mushroom Forest'"

            // Try finding the race instance by its ID first (most likely usage)
            long.TryParse(input.Replace("#", string.Empty), out long id);
            if (await ctx.Repository.GetRaceInstanceById(id) is RaceInstance raceInstanceById)
                return raceInstanceById;

            // Try looking it up by the race name
            if (await ctx.Repository.GetRaceInstanceByName(input) is RaceInstance raceInstanceByName)
                return raceInstanceByName;

            // We didn't find anything, so we return null.
            return null;
        }

        public static async Task<RaceInstance> MatchRaceInstance(this Context ctx)
        {
            // First, peek a race instance
            var raceInstance = await ctx.PeekRaceInstance();

            // If the peek was successful, we've used up the next argument, so we pop that just to get rid of it.
            if (raceInstance != null) ctx.PopArgument();

            // Finally, we return the chao value.
            return raceInstance;
        }

        public static async Task<Core.Item> PeekItem(this Context ctx)
        {
            var input = ctx.PeekArgument();
            if (!string.IsNullOrEmpty(input))
                input = input.Replace("\"", string.Empty).Replace(" ", string.Empty);

            // Items might be referenced by their ID or by their name (i.e. item type)
            // e.g. "!item 123" or "!item 'Red Egg'"

            // Try finding the item by its ID first (most likely usage)
            long.TryParse(input.Replace("#", string.Empty), out long id);
            if (await ctx.Repository.GetItem(id) is Core.Item itemById)
                return itemById;

            // Try looking it up by the name / type instead
            ItemBase.ItemTypes type;
            Enum.TryParse(input, out type);
            if (await ctx.Repository.GetItemByType(ctx.Garden.Id.Value, (int)type) is Core.Item itemByType)
                return itemByType;

            // We didn't find anything, so we return null.
            return null;
        }

        public static async Task<Core.Item> MatchItem(this Context ctx)
        {
            // First, peek an item
            var item = await ctx.PeekItem();

            // If the peek was successful, we've used up the next argument, so we pop that just to get rid of it.
            if (item != null) ctx.PopArgument();

            // Finally, we return the item
            return item;
        }

        public static async Task<MarketItem> PeekMarketItem(this Context ctx)
        {
            var input = ctx.PeekArgument();
            if (!string.IsNullOrEmpty(input))
                input = input.Replace("\"", string.Empty).Replace(" ", string.Empty);

            // Market items don't have a global ID, so they'll be referenced by their item type (which could be the type ID or name)
            // e.g. "!item 2" or "!item 'Red Egg'"

            // Try looking the item up by its type
            ItemBase.ItemTypes type;
            if (Enum.TryParse(input, out type))
            {
                var marketItem = await ctx.Repository.GetMarketItemByType(type);
                if (marketItem != null)
                    return marketItem;
                else
                    return new MarketItem
                    {
                        Quantity = 0,
                        ItemType = type
                    };
            }

            // We didn't find anything, so we return null.
            return null;
        }

        public static async Task<MarketItem> MatchMarketItem(this Context ctx)
        {
            // First, peek an item
            var item = await ctx.PeekMarketItem();

            // If the peek was successful, we've used up the next argument, so we pop that just to get rid of it.
            if (item != null) ctx.PopArgument();

            // Finally, we return the item
            return item;
        }

        public static string CreateChaoNotFoundError(this Context ctx, string input)
        {
            if (ctx.Member != null)
                return $"Chao with name \"{input}\" not found.";
            return $"Chao not found.";
        }

        public static Task<Channel> MatchChannel(this Context ctx)
        {
            if (!MentionUtils.TryParseChannel(ctx.PeekArgument(), out var id))
                return Task.FromResult<Channel>(null);

            if (!ctx.Cache.TryGetChannel(id, out var channel))
                return Task.FromResult<Channel>(null);

            if (!DiscordUtils.IsValidGuildChannel(channel))
                return Task.FromResult<Channel>(null);

            ctx.PopArgument();
            return Task.FromResult(channel);
        }

        public static Guild MatchGuild(this Context ctx)
        {
            try
            {
                var id = ulong.Parse(ctx.PeekArgument());
                ctx.Cache.TryGetGuild(id, out var guild);
                if (guild != null)
                    ctx.PopArgument();

                return guild;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}