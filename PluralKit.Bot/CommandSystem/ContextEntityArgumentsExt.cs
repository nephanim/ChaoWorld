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
            // - The direct user ID of an account connected to the system
            // - A @mention of an account connected to the system (<@uid>)
            // - A system hid

            // Direct IDs and mentions are both handled by the below method:
            if (input.TryParseMention(out var id))
                return await ctx.Repository.GetGardenByAccount(id);
            return null; //TODO: Is this safe?
        }

        public static async Task<Core.Chao> PeekChao(this Context ctx, GardenId? restrictToSystem = null)
        {
            var input = ctx.PeekArgument();

            // Chao references can have one of three forms, depending on
            // whether you're in a system or not:
            // - A chao hid
            // - A textual name of a chao *in your own system*
            // - a textual display name of a chao *in your own system*

            // First, if we have a system, try finding by chao name in system
            if (ctx.System != null && await ctx.Repository.GetChaoByName(ctx.System.Id, input) is Core.Chao chaoByName)
                return chaoByName;

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

        public static string CreateChaoNotFoundError(this Context ctx, string input)
        {
            // TODO: does this belong here?
            if (input.Length == 5)
            {
                if (ctx.System != null)
                    return $"Chao with ID or name \"{input}\" not found.";
                return $"Chao with ID \"{input}\" not found."; // Accounts without systems can't query by name
            }

            if (ctx.System != null)
                return $"Chao with name \"{input}\" not found. Note that a chao ID is 5 characters long.";
            return $"Chao not found. Note that a chao ID is 5 characters long.";
        }

        public static string CreateGroupNotFoundError(this Context ctx, string input)
        {
            // TODO: does this belong here?
            if (input.Length == 5)
            {
                if (ctx.System != null)
                    return $"Group with ID or name \"{input}\" not found.";
                return $"Group with ID \"{input}\" not found."; // Accounts without systems can't query by name
            }

            if (ctx.System != null)
                return $"Group with name \"{input}\" not found. Note that a group ID is 5 characters long.";
            return $"Group not found. Note that a group ID is 5 characters long.";
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