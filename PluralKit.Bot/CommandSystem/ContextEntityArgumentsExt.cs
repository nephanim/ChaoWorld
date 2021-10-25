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

        public static Task<Garden> PeekSystem(this Context ctx) => ctx.MatchSystemInner();

        public static async Task<Garden> MatchSystem(this Context ctx)
        {
            var system = await ctx.MatchSystemInner();
            if (system != null) ctx.PopArgument();
            return system;
        }

        private static async Task<Garden> MatchSystemInner(this Context ctx)
        {
            var input = ctx.PeekArgument();

            // Garden references can take three forms:
            // - The direct user ID of an account connected to the system
            // - A @mention of an account connected to the system (<@uid>)
            // - A system hid

            // Direct IDs and mentions are both handled by the below method:
            if (input.TryParseMention(out var id))
                return await ctx.Repository.GetSystemByAccount(id);

            // Finally, try HID parsing
            var system = await ctx.Repository.GetSystemByHid(input);
            return system;
        }

        public static async Task<Chao> PeekMember(this Context ctx, GardenId? restrictToSystem = null)
        {
            var input = ctx.PeekArgument();

            // Member references can have one of three forms, depending on
            // whether you're in a system or not:
            // - A member hid
            // - A textual name of a member *in your own system*
            // - a textual display name of a member *in your own system*

            // First, if we have a system, try finding by member name in system
            if (ctx.System != null && await ctx.Repository.GetMemberByName(ctx.System.Id, input) is Chao memberByName)
                return memberByName;

            // Then, try member HID parsing:
            if (await ctx.Repository.GetMemberByHid(input, restrictToSystem) is Chao memberByHid)
                return memberByHid;

            // And if that again fails, we try finding a member with a display name matching the argument from the system
            if (ctx.System != null && await ctx.Repository.GetMemberByDisplayName(ctx.System.Id, input) is Chao memberByDisplayName)
                return memberByDisplayName;

            // We didn't find anything, so we return null.
            return null;
        }

        /// <summary>
        /// Attempts to pop a member descriptor from the stack, returning it if present. If a member could not be
        /// resolved by the next word in the argument stack, does *not* touch the stack, and returns null.
        /// </summary>
        public static async Task<Chao> MatchMember(this Context ctx, GardenId? restrictToSystem = null)
        {
            // First, peek a member
            var member = await ctx.PeekMember(restrictToSystem);

            // If the peek was successful, we've used up the next argument, so we pop that just to get rid of it.
            if (member != null) ctx.PopArgument();

            // Finally, we return the member value.
            return member;
        }

        public static async Task<PKGroup> PeekGroup(this Context ctx, GardenId? restrictToSystem = null)
        {
            var input = ctx.PeekArgument();

            if (ctx.System != null && await ctx.Repository.GetGroupByName(ctx.System.Id, input) is { } byName)
                return byName;
            if (await ctx.Repository.GetGroupByHid(input, restrictToSystem) is { } byHid)
                return byHid;
            if (await ctx.Repository.GetGroupByDisplayName(ctx.System.Id, input) is { } byDisplayName)
                return byDisplayName;

            return null;
        }

        public static async Task<PKGroup> MatchGroup(this Context ctx, GardenId? restrictToSystem = null)
        {
            var group = await ctx.PeekGroup(restrictToSystem);
            if (group != null) ctx.PopArgument();
            return group;
        }

        public static string CreateMemberNotFoundError(this Context ctx, string input)
        {
            // TODO: does this belong here?
            if (input.Length == 5)
            {
                if (ctx.System != null)
                    return $"Member with ID or name \"{input}\" not found.";
                return $"Member with ID \"{input}\" not found."; // Accounts without systems can't query by name
            }

            if (ctx.System != null)
                return $"Member with name \"{input}\" not found. Note that a member ID is 5 characters long.";
            return $"Member not found. Note that a member ID is 5 characters long.";
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