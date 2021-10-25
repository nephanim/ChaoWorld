using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Myriad.Types;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public static class ContextArgumentsExt
    {
        public static string PopArgument(this Context ctx) =>
            ctx.Parameters.Pop();

        public static string PeekArgument(this Context ctx) =>
            ctx.Parameters.Peek();

        public static string RemainderOrNull(this Context ctx, bool skipFlags = true) =>
            ctx.Parameters.Remainder(skipFlags).Length == 0 ? null : ctx.Parameters.Remainder(skipFlags);

        public static bool HasNext(this Context ctx, bool skipFlags = true) =>
            ctx.RemainderOrNull(skipFlags) != null;

        public static string FullCommand(this Context ctx) =>
            ctx.Parameters.FullCommand;

        /// <summary>
        /// Checks if the next parameter is equal to one of the given keywords. Case-insensitive.
        /// </summary>
        public static bool Match(this Context ctx, ref string used, params string[] potentialMatches)
        {
            var arg = ctx.PeekArgument();
            foreach (var match in potentialMatches)
            {
                if (arg.Equals(match, StringComparison.InvariantCultureIgnoreCase))
                {
                    used = ctx.PopArgument();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the next parameter is equal to one of the given keywords. Case-insensitive.
        /// </summary>
        public static bool Match(this Context ctx, params string[] potentialMatches)
        {
            string used = null; // Unused and unreturned, we just yeet it
            return ctx.Match(ref used, potentialMatches);
        }

        public static bool MatchFlag(this Context ctx, params string[] potentialMatches)
        {
            // Flags are *ALWAYS PARSED LOWERCASE*. This means we skip out on a "ToLower" call here.
            // Can assume the caller array only contains lowercase *and* the set below only contains lowercase

            var flags = ctx.Parameters.Flags();
            return potentialMatches.Any(potentialMatch => flags.Contains(potentialMatch));
        }

        public static async Task<bool> MatchClear(this Context ctx, string toClear = null)
        {
            var matched = ctx.Match("clear", "reset") || ctx.MatchFlag("c", "clear");
            if (matched && toClear != null)
                return await ctx.ConfirmClear(toClear);
            return matched;
        }

        public static bool MatchRaw(this Context ctx) =>
            ctx.Match("r", "raw") || ctx.MatchFlag("r", "raw");

        public static (ulong? messageId, ulong? channelId) MatchMessage(this Context ctx, bool parseRawMessageId)
        {
            if (ctx.Message.Type == Message.MessageType.Reply && ctx.Message.MessageReference?.MessageId != null)
                return (ctx.Message.MessageReference.MessageId, ctx.Message.MessageReference.ChannelId);

            var word = ctx.PeekArgument();
            if (word == null)
                return (null, null);

            if (parseRawMessageId && ulong.TryParse(word, out var mid))
                return (mid, null);

            var match = Regex.Match(word, "https://(?:\\w+.)?discord(?:app)?.com/channels/\\d+/(\\d+)/(\\d+)");
            if (!match.Success)
                return (null, null);

            var channelId = ulong.Parse(match.Groups[1].Value);
            var messageId = ulong.Parse(match.Groups[2].Value);
            ctx.PopArgument();
            return (messageId, channelId);
        }

        public static async Task<List<Core.Chao>> ParseChaoList(this Context ctx, GardenId? restrictToSystem)
        {
            var chao = new List<Core.Chao>();

            // Loop through all the given arguments
            while (ctx.HasNext())
            {
                // and attempt to match a chao 
                var gardenChao = await ctx.MatchChao(restrictToSystem);

                if (gardenChao == null)
                    // if we can't, big error. Every chao name must be valid.
                    throw new CWError(ctx.CreateChaoNotFoundError(ctx.PopArgument()));

                chao.Add(gardenChao); // Then add to the final output list
            }
            if (chao.Count == 0) throw new CWSyntaxError($"You must input at least one chao.");

            return chao;
        }
    }
}