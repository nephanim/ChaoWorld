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

            ctx.CheckSystem();

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
                    await ctx.Reply($"Your system's name is currently **{ctx.System.Name}**. Type `pk;system name -clear` to clear it.");
                else
                    await ctx.Reply(noNameSetMessage);
                return;
            }

            if (await ctx.MatchClear("your system's name"))
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { Name = null });

                await ctx.Reply($"{Emojis.Success} Garden name cleared.");
            }
            else
            {
                var newSystemName = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();

                if (newSystemName.Length > Limits.MaxSystemNameLength)
                    throw Errors.StringTooLongError("Garden name", newSystemName.Length, Limits.MaxSystemNameLength);

                await _repo.UpdateSystem(ctx.System.Id, new() { Name = newSystemName });

                await ctx.Reply($"{Emojis.Success} Garden name changed.");
            }
        }

        public async Task Description(Context ctx)
        {
            var noDescriptionSetMessage = "Your system does not have a description set. To set one, type `pk;s description <description>`.";

            ctx.CheckSystem();

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
                await _repo.UpdateSystem(ctx.System.Id, new() { Description = null });

                await ctx.Reply($"{Emojis.Success} Garden description cleared.");
            }
            else
            {
                var newDescription = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();
                if (newDescription.Length > Limits.MaxDescriptionLength)
                    throw Errors.StringTooLongError("Description", newDescription.Length, Limits.MaxDescriptionLength);

                await _repo.UpdateSystem(ctx.System.Id, new() { Description = newDescription });

                await ctx.Reply($"{Emojis.Success} Garden description changed.");
            }
        }

        public async Task Color(Context ctx)
        {
            ctx.CheckSystem();

            if (await ctx.MatchClear())
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { Color = Partial<string>.Null() });

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

                await _repo.UpdateSystem(ctx.System.Id, new() { Color = Partial<string>.Present(color.ToLowerInvariant()) });

                await ctx.Reply(embed: new EmbedBuilder()
                    .Title($"{Emojis.Success} Garden color changed.")
                    .Color(color.ToDiscordColor())
                    .Thumbnail(new($"https://fakeimg.pl/256x256/{color}/?text=%20"))
                    .Build());
            }
        }

        public async Task Tag(Context ctx)
        {
            var noTagSetMessage = "You currently have no system tag. To set one, type `pk;s tag <tag>`.";

            ctx.CheckSystem();

            if (ctx.MatchRaw())
            {
                if (ctx.System.Tag == null)
                    await ctx.Reply(noTagSetMessage);
                else
                    await ctx.Reply($"```\n{ctx.System.Tag}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                if (ctx.System.Tag == null)
                    await ctx.Reply(noTagSetMessage);
                else
                    await ctx.Reply($"Your current system tag is {ctx.System.Tag.AsCode()}. To change it, type `pk;s tag <tag>`. To clear it, type `pk;s tag -clear`.");
                return;
            }

            if (await ctx.MatchClear("your system's tag"))
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { Tag = null });

                await ctx.Reply($"{Emojis.Success} Garden tag cleared.");
            }
            else
            {
                var newTag = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();
                if (newTag != null)
                    if (newTag.Length > Limits.MaxSystemTagLength)
                        throw Errors.StringTooLongError("Garden tag", newTag.Length, Limits.MaxSystemTagLength);

                await _repo.UpdateSystem(ctx.System.Id, new() { Tag = newTag });

                await ctx.Reply($"{Emojis.Success} Garden tag changed. Member names will now end with {newTag.AsCode()} when proxied.");
            }
        }

        public async Task ServerTag(Context ctx)
        {
            ctx.CheckSystem().CheckGuildContext();

            var setDisabledWarning = $"{Emojis.Warn} Your system tag is currently **disabled** in this server. No tag will be applied when proxying.\nTo re-enable the system tag in the current server, type `pk;s servertag -enable`.";

            var settings = await _repo.GetSystemGuild(ctx.Guild.Id, ctx.System.Id);

            async Task Show(bool raw = false)
            {
                if (settings.Tag != null)
                {
                    if (raw)
                    {
                        await ctx.Reply($"```{settings.Tag}```");
                        return;
                    }

                    var msg = $"Your current system tag in '{ctx.Guild.Name}' is {settings.Tag.AsCode()}";
                    if (!settings.TagEnabled)
                        msg += ", but it is currently **disabled**. To re-enable it, type `pk;s servertag -enable`.";
                    else
                        msg += ". To change it, type `pk;s servertag <tag>`. To clear it, type `pk;s servertag -clear`.";

                    await ctx.Reply(msg);
                    return;
                }

                else if (!settings.TagEnabled)
                    await ctx.Reply($"Your global system tag is {ctx.System.Tag}, but it is **disabled** in this server. To re-enable it, type `pk;s servertag -enable`");
                else
                    await ctx.Reply($"You currently have no system tag specific to the server '{ctx.Guild.Name}'. To set one, type `pk;s servertag <tag>`. To disable the system tag in the current server, type `pk;s servertag -disable`.");
            }

            async Task Set()
            {
                var newTag = ctx.RemainderOrNull(skipFlags: false);
                if (newTag != null && newTag.Length > Limits.MaxSystemTagLength)
                    throw Errors.StringTooLongError("Garden server tag", newTag.Length, Limits.MaxSystemTagLength);

                await _repo.UpdateSystemGuild(ctx.System.Id, ctx.Guild.Id, new() { Tag = newTag });

                await ctx.Reply($"{Emojis.Success} Garden server tag changed. Member names will now end with {newTag.AsCode()} when proxied in the current server '{ctx.Guild.Name}'.");

                if (!ctx.MessageContext.TagEnabled)
                    await ctx.Reply(setDisabledWarning);
            }

            async Task Clear()
            {
                await _repo.UpdateSystemGuild(ctx.System.Id, ctx.Guild.Id, new() { Tag = null });

                await ctx.Reply($"{Emojis.Success} Garden server tag cleared. Member names will now end with the global system tag, if there is one set.");

                if (!ctx.MessageContext.TagEnabled)
                    await ctx.Reply(setDisabledWarning);
            }

            async Task EnableDisable(bool newValue)
            {
                await _repo.UpdateSystemGuild(ctx.System.Id, ctx.Guild.Id, new() { TagEnabled = newValue });

                await ctx.Reply(PrintEnableDisableResult(newValue, newValue != ctx.MessageContext.TagEnabled));
            }

            string PrintEnableDisableResult(bool newValue, bool changedValue)
            {
                var opStr = newValue ? "enabled" : "disabled";
                var str = "";

                if (!changedValue)
                    str = $"{Emojis.Note} The system tag is already {opStr} in this server.";
                else
                    str = $"{Emojis.Success} Garden tag {opStr} in this server.";

                if (newValue == true)
                {
                    if (ctx.MessageContext.TagEnabled)
                        if (ctx.MessageContext.SystemGuildTag == null)
                            str += $" However, you do not have a system tag specific to this server. Messages will be proxied using your global system tag, if there is one set.";
                        else
                            str += $" Your current system tag in '{ctx.Guild.Name}' is {ctx.MessageContext.SystemGuildTag.AsCode()}.";
                    else
                    {
                        if (ctx.MessageContext.SystemGuildTag != null)
                            str += $" Member names will now end with the server-specific tag {ctx.MessageContext.SystemGuildTag.AsCode()} when proxied in the current server '{ctx.Guild.Name}'.";
                        else
                            str += $" Member names will now end with the global system tag when proxied in the current server, if there is one set.";
                    }
                }

                return str;
            }

            if (await ctx.MatchClear("your system's server tag"))
                await Clear();
            else if (ctx.Match("disable") || ctx.MatchFlag("disable"))
                await EnableDisable(false);
            else if (ctx.Match("enable") || ctx.MatchFlag("enable"))
                await EnableDisable(true);
            else if (ctx.MatchRaw())
                await Show(raw: true);
            else if (!ctx.HasNext(skipFlags: false))
                await Show();
            else
                await Set();
        }

        public async Task Avatar(Context ctx)
        {
            ctx.CheckSystem();

            async Task ClearIcon()
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { AvatarUrl = null });
                await ctx.Reply($"{Emojis.Success} Garden icon cleared.");
            }

            async Task SetIcon(ParsedImage img)
            {
                await AvatarUtils.VerifyAvatarOrThrow(_client, img.Url);

                await _repo.UpdateSystem(ctx.System.Id, new() { AvatarUrl = img.Url });

                var msg = img.Source switch
                {
                    AvatarSource.User => $"{Emojis.Success} Garden icon changed to {img.SourceUser?.Username}'s avatar!\n{Emojis.Warn} If {img.SourceUser?.Username} changes their avatar, the system icon will need to be re-set.",
                    AvatarSource.Url => $"{Emojis.Success} Garden icon changed to the image at the given URL.",
                    AvatarSource.Attachment => $"{Emojis.Success} Garden icon changed to attached image.\n{Emojis.Warn} If you delete the message containing the attachment, the system icon will stop working.",
                    _ => throw new ArgumentOutOfRangeException()
                };

                // The attachment's already right there, no need to preview it.
                var hasEmbed = img.Source != AvatarSource.Attachment;
                await (hasEmbed
                    ? ctx.Reply(msg, embed: new EmbedBuilder().Image(new(img.Url)).Build())
                    : ctx.Reply(msg));
            }

            async Task ShowIcon()
            {
                if ((ctx.System.AvatarUrl?.Trim() ?? "").Length > 0)
                {
                    var eb = new EmbedBuilder()
                        .Title("Garden icon")
                        .Image(new(ctx.System.AvatarUrl.TryGetCleanCdnUrl()))
                        .Description("To clear, use `pk;system icon clear`.");
                    await ctx.Reply(embed: eb.Build());
                }
                else
                    throw new PKSyntaxError("This system does not have an icon set. Set one by attaching an image to this command, or by passing an image URL or @mention.");
            }

            if (await ctx.MatchClear("your system's icon"))
                await ClearIcon();
            else if (await ctx.MatchImage() is { } img)
                await SetIcon(img);
            else
                await ShowIcon();
        }

        public async Task BannerImage(Context ctx)
        {
            ctx.CheckSystem();

            async Task ClearImage()
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { BannerImage = null });
                await ctx.Reply($"{Emojis.Success} Garden banner image cleared.");
            }

            async Task SetImage(ParsedImage img)
            {
                await AvatarUtils.VerifyAvatarOrThrow(_client, img.Url, isFullSizeImage: true);

                await _repo.UpdateSystem(ctx.System.Id, new() { BannerImage = img.Url });

                var msg = img.Source switch
                {
                    AvatarSource.Url => $"{Emojis.Success} Garden banner image changed to the image at the given URL.",
                    AvatarSource.Attachment => $"{Emojis.Success} Garden banner image changed to attached image.\n{Emojis.Warn} If you delete the message containing the attachment, the banner image will stop working.",
                    AvatarSource.User => throw new PKError("Cannot set a banner image to an user's avatar."),
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
                    throw new PKSyntaxError("This system does not have a banner image set. Set one by attaching an image to this command, or by passing an image URL or @mention.");
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
            ctx.CheckSystem();

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete your system? If so, reply to this message with your system's ID (`{ctx.System.Hid}`).\n**Note: this action is permanent.**");
            if (!await ctx.ConfirmWithReply(ctx.System.Hid))
                throw new PKError($"Garden deletion cancelled. Note that you must reply with your system ID (`{ctx.System.Hid}`) *verbatim*.");

            await _repo.DeleteSystem(ctx.System.Id);

            await ctx.Reply($"{Emojis.Success} Garden deleted.");
        }

        public async Task SystemProxy(Context ctx)
        {
            ctx.CheckSystem();

            var guild = ctx.MatchGuild() ?? ctx.Guild ??
                throw new PKError("You must run this command in a server or pass a server ID.");

            var gs = await _repo.GetSystemGuild(guild.Id, ctx.System.Id);

            string serverText;
            if (guild.Id == ctx.Guild?.Id)
                serverText = $"this server ({guild.Name.EscapeMarkdown()})";
            else
                serverText = $"the server {guild.Name.EscapeMarkdown()}";

            bool newValue;
            if (ctx.Match("on", "enabled", "true", "yes")) newValue = true;
            else if (ctx.Match("off", "disabled", "false", "no")) newValue = false;
            else if (ctx.HasNext()) throw new PKSyntaxError("You must pass either \"on\" or \"off\".");
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

        public async Task SystemTimezone(Context ctx)
        {
            if (ctx.System == null) throw Errors.NoSystemError;

            if (await ctx.MatchClear())
            {
                await _repo.UpdateSystem(ctx.System.Id, new() { UiTz = "UTC" });

                await ctx.Reply($"{Emojis.Success} Garden time zone cleared (set to UTC).");
                return;
            }

            var zoneStr = ctx.RemainderOrNull();
            if (zoneStr == null)
            {
                await ctx.Reply(
                    $"Your current system time zone is set to **{ctx.System.UiTz}**. It is currently **{SystemClock.Instance.GetCurrentInstant().FormatZoned(ctx.System)}** in that time zone. To change your system time zone, type `pk;s tz <zone>`.");
                return;
            }

            var zone = await FindTimeZone(ctx, zoneStr);
            if (zone == null) throw Errors.InvalidTimeZone(zoneStr);

            var currentTime = SystemClock.Instance.GetCurrentInstant().InZone(zone);
            var msg = $"This will change the system time zone to **{zone.Id}**. The current time is **{currentTime.FormatZoned()}**. Is this correct?";
            if (!await ctx.PromptYesNo(msg, "Change Timezone")) throw Errors.TimezoneChangeCancelled;

            await _repo.UpdateSystem(ctx.System.Id, new() { UiTz = zone.Id });

            await ctx.Reply($"Garden time zone changed to **{zone.Id}**.");
        }

        public async Task SystemPrivacy(Context ctx)
        {
            ctx.CheckSystem();

            Task PrintEmbed()
            {
                var eb = new EmbedBuilder()
                    .Title("Current privacy settings for your system")
                    .Field(new("Description", ctx.System.DescriptionPrivacy.Explanation()))
                    .Field(new("Member list", ctx.System.MemberListPrivacy.Explanation()))
                    .Field(new("Group list", ctx.System.GroupListPrivacy.Explanation()))
                    .Field(new("Current fronter(s)", ctx.System.FrontPrivacy.Explanation()))
                    .Field(new("Front/switch history", ctx.System.FrontHistoryPrivacy.Explanation()))
                    .Description("To edit privacy settings, use the command:\n`pk;system privacy <subject> <level>`\n\n- `subject` is one of `description`, `list`, `front`, `fronthistory`, `groups`, or `all` \n- `level` is either `public` or `private`.");
                return ctx.Reply(embed: eb.Build());
            }

            async Task SetLevel(SystemPrivacySubject subject, PrivacyLevel level)
            {
                await _repo.UpdateSystem(ctx.System.Id, new SystemPatch().WithPrivacy(subject, level));

                var levelExplanation = level switch
                {
                    PrivacyLevel.Public => "be able to query",
                    PrivacyLevel.Private => "*not* be able to query",
                    _ => ""
                };

                var subjectStr = subject switch
                {
                    SystemPrivacySubject.Description => "description",
                    SystemPrivacySubject.Front => "front",
                    SystemPrivacySubject.FrontHistory => "front history",
                    SystemPrivacySubject.MemberList => "member list",
                    SystemPrivacySubject.GroupList => "group list",
                    _ => ""
                };

                var msg = $"Garden {subjectStr} privacy has been set to **{level.LevelName()}**. Other accounts will now {levelExplanation} your system {subjectStr}.";
                await ctx.Reply($"{Emojis.Success} {msg}");
            }

            async Task SetAll(PrivacyLevel level)
            {
                await _repo.UpdateSystem(ctx.System.Id, new SystemPatch().WithAllPrivacy(level));

                var msg = level switch
                {
                    PrivacyLevel.Private => $"All system privacy settings have been set to **{level.LevelName()}**. Other accounts will now not be able to view your member list, group list, front history, or system description.",
                    PrivacyLevel.Public => $"All system privacy settings have been set to **{level.LevelName()}**. Other accounts will now be able to view everything.",
                    _ => ""
                };

                await ctx.Reply($"{Emojis.Success} {msg}");
            }

            if (!ctx.HasNext())
                await PrintEmbed();
            else if (ctx.Match("all"))
                await SetAll(ctx.PopPrivacyLevel());
            else
                await SetLevel(ctx.PopSystemPrivacySubject(), ctx.PopPrivacyLevel());
        }

        public async Task SystemPing(Context ctx)
        {
            ctx.CheckSystem();

            if (!ctx.HasNext())
            {
                if (ctx.System.PingsEnabled) { await ctx.Reply("Reaction pings are currently **enabled** for your system. To disable reaction pings, type `pk;s ping disable`."); }
                else { await ctx.Reply("Reaction pings are currently **disabled** for your system. To enable reaction pings, type `pk;s ping enable`."); }
            }
            else
            {
                if (ctx.Match("on", "enable"))
                {
                    await _repo.UpdateSystem(ctx.System.Id, new() { PingsEnabled = true });

                    await ctx.Reply("Reaction pings have now been enabled.");
                }
                if (ctx.Match("off", "disable"))
                {
                    await _repo.UpdateSystem(ctx.System.Id, new() { PingsEnabled = false });

                    await ctx.Reply("Reaction pings have now been disabled.");
                }
            }
        }

        public async Task<DateTimeZone> FindTimeZone(Context ctx, string zoneStr)
        {
            // First, if we're given a flag emoji, we extract the flag emoji code from it.
            zoneStr = Core.StringUtils.ExtractCountryFlag(zoneStr) ?? zoneStr;

            // Then, we find all *locations* matching either the given country code or the country name.
            var locations = TzdbDateTimeZoneSource.Default.Zone1970Locations;
            var matchingLocations = locations.Where(l => l.Countries.Any(c =>
                string.Equals(c.Code, zoneStr, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(c.Name, zoneStr, StringComparison.InvariantCultureIgnoreCase)));

            // Then, we find all (unique) time zone IDs that match.
            var matchingZones = matchingLocations.Select(l => DateTimeZoneProviders.Tzdb.GetZoneOrNull(l.ZoneId))
                .Distinct().ToList();

            // If the set of matching zones is empty (ie. we didn't find anything), we try a few other things.
            if (matchingZones.Count == 0)
            {
                // First, we try to just find the time zone given directly and return that.
                var givenZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneStr);
                if (givenZone != null) return givenZone;

                // If we didn't find anything there either, we try parsing the string as an offset, then
                // find all possible zones that match that offset. For an offset like UTC+2, this doesn't *quite*
                // work, since there are 57(!) matching zones (as of 2019-06-13) - but for less populated time zones
                // this could work nicely.
                var inputWithoutUtc = zoneStr.Replace("UTC", "").Replace("GMT", "");

                var res = OffsetPattern.CreateWithInvariantCulture("+H").Parse(inputWithoutUtc);
                if (!res.Success) res = OffsetPattern.CreateWithInvariantCulture("+H:mm").Parse(inputWithoutUtc);

                // If *this* didn't parse correctly, fuck it, bail.
                if (!res.Success) return null;
                var offset = res.Value;

                // To try to reduce the count, we go by locations from the 1970+ database instead of just the full database
                // This elides regions that have been identical since 1970, omitting small distinctions due to Ancient History(tm).
                var allZones = TzdbDateTimeZoneSource.Default.Zone1970Locations.Select(l => l.ZoneId).Distinct();
                matchingZones = allZones.Select(z => DateTimeZoneProviders.Tzdb.GetZoneOrNull(z))
                    .Where(z => z.GetUtcOffset(SystemClock.Instance.GetCurrentInstant()) == offset).ToList();
            }

            // If we have a list of viable time zones, we ask the user which is correct.

            // If we only have one, return that one.
            if (matchingZones.Count == 1)
                return matchingZones.First();

            // Otherwise, prompt and return!
            return await ctx.Choose("There were multiple matches for your time zone query. Please select the region that matches you the closest:", matchingZones,
                z =>
                {
                    if (TzdbDateTimeZoneSource.Default.Aliases.Contains(z.Id))
                        return $"**{z.Id}**, {string.Join(", ", TzdbDateTimeZoneSource.Default.Aliases[z.Id])}";

                    return $"**{z.Id}**";
                });
        }
    }
}