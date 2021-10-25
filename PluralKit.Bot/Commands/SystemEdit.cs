using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Myriad.Builders;

using NodaTime;
using NodaTime.Text;
using NodaTime.TimeZones;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class SystemEdit
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public SystemEdit(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task Name(Context ctx)
        {
            var noNameSetMessage = "Your system does not have a name set. Type `pk;system name <name>` to set one.";

            ctx.CheckGarden();

            if (ctx.MatchRaw())
            {
                if (ctx.System.Name != null)
                    await ctx.Reply($"```\n{ctx.System.Name}\n```");
                else
                    await ctx.Reply(noNameSetMessage);
                return;
            }
            if (!ctx.HasNext(false))
            {
                if (ctx.System.Name != null)
                    await ctx.Reply($"Your garden's name is currently **{ctx.System.Name}**. Type `pk;system name -clear` to clear it.");
                else
                    await ctx.Reply(noNameSetMessage);
                return;
            }

            if (await ctx.MatchClear("your garden's name"))
            {
                await _repo.UpdateGarden(ctx.System.Id, new() { Name = null });

                await ctx.Reply($"{Emojis.Success} Garden name cleared.");
            }
            else
            {
                var newSystemName = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();

                if (newSystemName.Length > Limits.MaxSystemNameLength)
                    throw Errors.StringTooLongError("Garden name", newSystemName.Length, Limits.MaxSystemNameLength);

                await _repo.UpdateGarden(ctx.System.Id, new() { Name = newSystemName });

                await ctx.Reply($"{Emojis.Success} Garden name changed.");
            }
        }

        public async Task Description(Context ctx)
        {
            var noDescriptionSetMessage = "Your system does not have a description set. To set one, type `pk;s description <description>`.";

            ctx.CheckGarden();

            if (ctx.MatchRaw())
            {
                if (ctx.System.Description == null)
                    await ctx.Reply(noDescriptionSetMessage);
                else
                    await ctx.Reply($"```\n{ctx.System.Description}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                if (ctx.System.Description == null)
                    await ctx.Reply(noDescriptionSetMessage);
                else
                    await ctx.Reply(embed: new EmbedBuilder()
                        .Title("Garden description")
                        .Description(ctx.System.Description)
                        .Footer(new("To print the description with formatting, type `pk;s description -raw`. To clear it, type `pk;s description -clear`. To change it, type `pk;s description <new description>`."))
                        .Build());
                return;
            }

            if (await ctx.MatchClear("your system's description"))
            {
                await _repo.UpdateGarden(ctx.System.Id, new() { Description = null });

                await ctx.Reply($"{Emojis.Success} Garden description cleared.");
            }
            else
            {
                var newDescription = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();
                if (newDescription.Length > Limits.MaxDescriptionLength)
                    throw Errors.StringTooLongError("Description", newDescription.Length, Limits.MaxDescriptionLength);

                await _repo.UpdateGarden(ctx.System.Id, new() { Description = newDescription });

                await ctx.Reply($"{Emojis.Success} Garden description changed.");
            }
        }

        public async Task Color(Context ctx)
        {
            ctx.CheckGarden();

            if (await ctx.MatchClear())
            {
                await _repo.UpdateGarden(ctx.System.Id, new() { Color = Partial<string>.Null() });

                await ctx.Reply($"{Emojis.Success} Garden color cleared.");
            }
            else if (!ctx.HasNext())
            {
                if (ctx.System.Color == null)
                    await ctx.Reply(
                            $"Your system does not have a color set. To set one, type `pk;system color <color>`.");
                else
                    await ctx.Reply(embed: new EmbedBuilder()
                        .Title("Garden color")
                        .Color(ctx.System.Color.ToDiscordColor())
                        .Thumbnail(new($"https://fakeimg.pl/256x256/{ctx.System.Color}/?text=%20"))
                        .Description($"Your system's color is **#{ctx.System.Color}**. To clear it, type `pk;s color -clear`.")
                        .Build());
            }
            else
            {
                var color = ctx.RemainderOrNull();

                if (color.StartsWith("#")) color = color.Substring(1);
                if (!Regex.IsMatch(color, "^[0-9a-fA-F]{6}$")) throw Errors.InvalidColorError(color);

                await _repo.UpdateGarden(ctx.System.Id, new() { Color = Partial<string>.Present(color.ToLowerInvariant()) });

                await ctx.Reply(embed: new EmbedBuilder()
                    .Title($"{Emojis.Success} Garden color changed.")
                    .Color(color.ToDiscordColor())
                    .Thumbnail(new($"https://fakeimg.pl/256x256/{color}/?text=%20"))
                    .Build());
            }
        }

        public async Task BannerImage(Context ctx)
        {
            ctx.CheckGarden();

            async Task ClearImage()
            {
                await _repo.UpdateGarden(ctx.System.Id, new() { BannerImage = null });
                await ctx.Reply($"{Emojis.Success} Garden banner image cleared.");
            }

            async Task SetImage(ParsedImage img)
            {
                await AvatarUtils.VerifyAvatarOrThrow(_client, img.Url, isFullSizeImage: true);

                await _repo.UpdateGarden(ctx.System.Id, new() { BannerImage = img.Url });

                var msg = img.Source switch
                {
                    AvatarSource.Url => $"{Emojis.Success} Garden banner image changed to the image at the given URL.",
                    AvatarSource.Attachment => $"{Emojis.Success} Garden banner image changed to attached image.\n{Emojis.Warn} If you delete the message containing the attachment, the banner image will stop working.",
                    AvatarSource.User => throw new CWError("Cannot set a banner image to an user's avatar."),
                    _ => throw new ArgumentOutOfRangeException()
                };

                // The attachment's already right there, no need to preview it.
                var hasEmbed = img.Source != AvatarSource.Attachment;
                await (hasEmbed
                    ? ctx.Reply(msg, embed: new EmbedBuilder().Image(new(img.Url)).Build())
                    : ctx.Reply(msg));
            }

            async Task ShowImage()
            {
                if ((ctx.System.BannerImage?.Trim() ?? "").Length > 0)
                {
                    var eb = new EmbedBuilder()
                        .Title("Garden banner image")
                        .Image(new(ctx.System.BannerImage))
                        .Description("To clear, use `pk;system banner clear`.");
                    await ctx.Reply(embed: eb.Build());
                }
                else
                    throw new CWSyntaxError("This system does not have a banner image set. Set one by attaching an image to this command, or by passing an image URL or @mention.");
            }

            if (await ctx.MatchClear("your system's banner image"))
                await ClearImage();
            else if (await ctx.MatchImage() is { } img)
                await SetImage(img);
            else
                await ShowImage();
        }

        public async Task Delete(Context ctx)
        {
            ctx.CheckGarden();

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete your system? If so, reply to this message with your system's ID (`{ctx.System.Hid}`).\n**Note: this action is permanent.**");
            if (!await ctx.ConfirmWithReply(ctx.System.Hid))
                throw new CWError($"Garden deletion cancelled. Note that you must reply with your system ID (`{ctx.System.Hid}`) *verbatim*.");

            await _repo.DeleteGarden(ctx.System.Id);

            await ctx.Reply($"{Emojis.Success} Garden deleted.");
        }

        public async Task SystemProxy(Context ctx)
        {
            ctx.CheckGarden();

            var guild = ctx.MatchGuild() ?? ctx.Guild ??
                throw new CWError("You must run this command in a server or pass a server ID.");

            var gs = await _repo.GetSystemGuild(guild.Id, ctx.System.Id);

            string serverText;
            if (guild.Id == ctx.Guild?.Id)
                serverText = $"this server ({guild.Name.EscapeMarkdown()})";
            else
                serverText = $"the server {guild.Name.EscapeMarkdown()}";

            bool newValue;
            if (ctx.Match("on", "enabled", "true", "yes")) newValue = true;
            else if (ctx.Match("off", "disabled", "false", "no")) newValue = false;
            else if (ctx.HasNext()) throw new CWSyntaxError("You must pass either \"on\" or \"off\".");
            else
            {
                if (gs.ProxyEnabled)
                    await ctx.Reply($"Proxying in {serverText} is currently **enabled** for your system. To disable it, type `pk;system proxy off`.");
                else
                    await ctx.Reply($"Proxying in {serverText} is currently **disabled** for your system. To enable it, type `pk;system proxy on`.");
                return;
            }

            await _repo.UpdateSystemGuild(ctx.System.Id, guild.Id, new() { ProxyEnabled = newValue });

            if (newValue)
                await ctx.Reply($"Message proxying in {serverText} is now **enabled** for your system.");
            else
                await ctx.Reply($"Message proxying in {serverText} is now **disabled** for your system.");
        }
    }
}