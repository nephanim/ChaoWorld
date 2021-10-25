#nullable enable
using System;
using System.Net.Http;
using System.Threading.Tasks;

using Myriad.Builders;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class MemberAvatar
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public MemberAvatar(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        private async Task AvatarClear(AvatarLocation location, Context ctx, Chao target, MemberGuildSettings? mgs)
        {
            await UpdateAvatar(location, ctx, target, null);
            if (location == AvatarLocation.Server)
            {
                if (target.AvatarUrl != null)
                    await ctx.Reply($"{Emojis.Success} Member server avatar cleared. This member will now use the global avatar in this server (**{ctx.Guild.Name}**).");
                else
                    await ctx.Reply($"{Emojis.Success} Member server avatar cleared. This member now has no avatar.");
            }
            else
            {
                if (mgs?.AvatarUrl != null)
                    await ctx.Reply($"{Emojis.Success} Member avatar cleared. Note that this member has a server-specific avatar set here, type `pk;member {target.Reference()} serveravatar clear` if you wish to clear that too.");
                else
                    await ctx.Reply($"{Emojis.Success} Member avatar cleared.");
            }
        }

        private async Task AvatarShow(AvatarLocation location, Context ctx, Chao target, MemberGuildSettings? guildData)
        {
            var currentValue = location == AvatarLocation.Member ? target.AvatarUrl : guildData?.AvatarUrl;
            var canAccess = location != AvatarLocation.Member || target.AvatarPrivacy.CanAccess(ctx.LookupContextFor(target));
            if (string.IsNullOrEmpty(currentValue) || !canAccess)
            {
                if (location == AvatarLocation.Member)
                {
                    if (target.Garden == ctx.System?.Id)
                        throw new CWSyntaxError("This member does not have an avatar set. Set one by attaching an image to this command, or by passing an image URL or @mention.");
                    throw new CWError("This member does not have an avatar set.");
                }

                if (location == AvatarLocation.Server)
                    throw new CWError($"This member does not have a server avatar set. Type `pk;member {target.Reference()} avatar` to see their global avatar.");
            }

            var field = location == AvatarLocation.Server ? $"server avatar (for {ctx.Guild.Name})" : "avatar";
            var cmd = location == AvatarLocation.Server ? "serveravatar" : "avatar";

            var eb = new EmbedBuilder()
                .Title($"{target.NameFor(ctx)}'s {field}")
                .Image(new(currentValue?.TryGetCleanCdnUrl()));
            if (target.Garden == ctx.System?.Id)
                eb.Description($"To clear, use `pk;member {target.Reference()} {cmd} clear`.");
            await ctx.Reply(embed: eb.Build());
        }

        public async Task ServerAvatar(Context ctx, Chao target)
        {
            ctx.CheckGuildContext();
            var guildData = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);
            await AvatarCommandTree(AvatarLocation.Server, ctx, target, guildData);
        }

        public async Task Avatar(Context ctx, Chao target)
        {
            var guildData = ctx.Guild != null ?
                await _repo.GetMemberGuild(ctx.Guild.Id, target.Id)
                : null;

            await AvatarCommandTree(AvatarLocation.Member, ctx, target, guildData);
        }

        private async Task AvatarCommandTree(AvatarLocation location, Context ctx, Chao target, MemberGuildSettings? guildData)
        {
            // First, see if we need to *clear*
            if (await ctx.MatchClear(location == AvatarLocation.Server ? "this member's server avatar" : "this member's avatar"))
            {
                ctx.CheckGarden().CheckOwnMember(target);
                await AvatarClear(location, ctx, target, guildData);
                return;
            }

            // Then, parse an image from the command (from various sources...)
            var avatarArg = await ctx.MatchImage();
            if (avatarArg == null)
            {
                // If we didn't get any, just show the current avatar
                await AvatarShow(location, ctx, target, guildData);
                return;
            }

            ctx.CheckGarden().CheckOwnMember(target);
            await AvatarUtils.VerifyAvatarOrThrow(_client, avatarArg.Value.Url);
            await UpdateAvatar(location, ctx, target, avatarArg.Value.Url);
            await PrintResponse(location, ctx, target, avatarArg.Value, guildData);
        }

        private Task PrintResponse(AvatarLocation location, Context ctx, Chao target, ParsedImage avatar,
                                   MemberGuildSettings? targetGuildData)
        {
            var typeFrag = location switch
            {
                AvatarLocation.Server => "server avatar",
                AvatarLocation.Member => "avatar",
                _ => throw new ArgumentOutOfRangeException(nameof(location))
            };

            var serverFrag = location switch
            {
                AvatarLocation.Server => $" This avatar will now be used when proxying in this server (**{ctx.Guild.Name}**).",
                AvatarLocation.Member when targetGuildData?.AvatarUrl != null => $"\n{Emojis.Note} Note that this member *also* has a server-specific avatar set in this server (**{ctx.Guild.Name}**), and thus changing the global avatar will have no effect here.",
                _ => ""
            };

            var msg = avatar.Source switch
            {
                AvatarSource.User => $"{Emojis.Success} Member {typeFrag} changed to {avatar.SourceUser?.Username}'s avatar!{serverFrag}\n{Emojis.Warn} If {avatar.SourceUser?.Username} changes their avatar, the member's avatar will need to be re-set.",
                AvatarSource.Url => $"{Emojis.Success} Member {typeFrag} changed to the image at the given URL.{serverFrag}",
                AvatarSource.Attachment => $"{Emojis.Success} Member {typeFrag} changed to attached image.{serverFrag}\n{Emojis.Warn} If you delete the message containing the attachment, the avatar will stop working.",
                _ => throw new ArgumentOutOfRangeException()
            };

            // The attachment's already right there, no need to preview it.
            var hasEmbed = avatar.Source != AvatarSource.Attachment;
            return hasEmbed
                ? ctx.Reply(msg, embed: new EmbedBuilder().Image(new(avatar.Url)).Build())
                : ctx.Reply(msg);
        }

        private Task UpdateAvatar(AvatarLocation location, Context ctx, Chao target, string? url)
        {
            switch (location)
            {
                case AvatarLocation.Server:
                    return _repo.UpdateMemberGuild(target.Id, ctx.Guild.Id, new() { AvatarUrl = url });
                case AvatarLocation.Member:
                    return _repo.UpdateMember(target.Id, new() { AvatarUrl = url });
                default:
                    throw new ArgumentOutOfRangeException($"Unknown avatar location {location}");
            }
        }

        private enum AvatarLocation
        {
            Member,
            Server
        }
    }
}