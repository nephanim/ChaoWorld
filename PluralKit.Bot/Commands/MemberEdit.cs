using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Net.Http;

using Myriad.Builders;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class MemberEdit
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public MemberEdit(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task Name(Context ctx, Chao target)
        {
            ctx.CheckGarden().CheckOwnMember(target);

            var newName = ctx.RemainderOrNull() ?? throw new CWSyntaxError("You must pass a new name for the member.");

            // Hard name length cap
            if (newName.Length > Limits.MaxMemberNameLength)
                throw Errors.StringTooLongError("Member name", newName.Length, Limits.MaxMemberNameLength);

            // Warn if there's already a member by this name
            var existingMember = await _repo.GetMemberByName(ctx.System.Id, newName);
            if (existingMember != null && existingMember.Id != target.Id)
            {
                var msg = $"{Emojis.Warn} You already have a member in your system with the name \"{existingMember.Name}\" (`{existingMember.Hid}`). Do you want to rename this member to that name too?";
                if (!await ctx.PromptYesNo(msg, "Rename")) throw new CWError("Member renaming cancelled.");
            }

            // Rename the member
            var patch = new GardenPatch { Name = Partial<string>.Present(newName) };
            await _repo.UpdateMember(target.Id, patch);

            await ctx.Reply($"{Emojis.Success} Member renamed.");
            if (newName.Contains(" ")) await ctx.Reply($"{Emojis.Note} Note that this member's name now contains spaces. You will need to surround it with \"double quotes\" when using commands referring to it.");
            if (target.DisplayName != null) await ctx.Reply($"{Emojis.Note} Note that this member has a display name set ({target.DisplayName}), and will be proxied using that name instead.");

            if (ctx.Guild != null)
            {
                var memberGuildConfig = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);
                if (memberGuildConfig.DisplayName != null)
                    await ctx.Reply($"{Emojis.Note} Note that this member has a server name set ({memberGuildConfig.DisplayName}) in this server ({ctx.Guild.Name}), and will be proxied using that name here.");
            }
        }

        public async Task Description(Context ctx, Chao target)
        {
            var noDescriptionSetMessage = "This member does not have a description set.";
            if (ctx.System?.Id == target.Garden)
                noDescriptionSetMessage += $" To set one, type `pk;member {target.Reference()} description <description>`.";

            if (ctx.MatchRaw())
            {
                if (target.Description == null)
                    await ctx.Reply(noDescriptionSetMessage);
                else
                    await ctx.Reply($"```\n{target.Description}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                if (target.Description == null)
                    await ctx.Reply(noDescriptionSetMessage);
                else
                    await ctx.Reply(embed: new EmbedBuilder()
                        .Title("Member description")
                        .Description(target.Description)
                        .Field(new("\u200B", $"To print the description with formatting, type `pk;member {target.Reference()} description -raw`."
                                    + (ctx.System?.Id == target.Garden ? $" To clear it, type `pk;member {target.Reference()} description -clear`." : "")))
                        .Build());
                return;
            }

            ctx.CheckOwnMember(target);

            if (await ctx.MatchClear("this member's description"))
            {
                var patch = new GardenPatch { Description = Partial<string>.Null() };
                await _repo.UpdateMember(target.Id, patch);
                await ctx.Reply($"{Emojis.Success} Member description cleared.");
            }
            else
            {
                var description = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();
                if (description.IsLongerThan(Limits.MaxDescriptionLength))
                    throw Errors.StringTooLongError("Description", description.Length, Limits.MaxDescriptionLength);

                var patch = new GardenPatch { Description = Partial<string>.Present(description) };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply($"{Emojis.Success} Member description changed.");
            }
        }

        public async Task Pronouns(Context ctx, Chao target)
        {
            var noPronounsSetMessage = "This member does not have pronouns set.";
            if (ctx.System?.Id == target.Garden)
                noPronounsSetMessage += $"To set some, type `pk;member {target.Reference()} pronouns <pronouns>`.";

            if (ctx.MatchRaw())
            {
                if (target.Pronouns == null)
                    await ctx.Reply(noPronounsSetMessage);
                else
                    await ctx.Reply($"```\n{target.Pronouns}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                if (target.Pronouns == null)
                    await ctx.Reply(noPronounsSetMessage);
                else
                    await ctx.Reply($"**{target.Name}**'s pronouns are **{target.Pronouns}**.\nTo print the pronouns with formatting, type `pk;member {target.Reference()} pronouns -raw`."
                        + (ctx.System?.Id == target.Garden ? $" To clear them, type `pk;member {target.Reference()} pronouns -clear`." : ""));
                return;
            }

            ctx.CheckOwnMember(target);

            if (await ctx.MatchClear("this member's pronouns"))
            {
                var patch = new GardenPatch { Pronouns = Partial<string>.Null() };
                await _repo.UpdateMember(target.Id, patch);
                await ctx.Reply($"{Emojis.Success} Member pronouns cleared.");
            }
            else
            {
                var pronouns = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();
                if (pronouns.IsLongerThan(Limits.MaxPronounsLength))
                    throw Errors.StringTooLongError("Pronouns", pronouns.Length, Limits.MaxPronounsLength);

                var patch = new GardenPatch { Pronouns = Partial<string>.Present(pronouns) };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply($"{Emojis.Success} Member pronouns changed.");
            }
        }

        public async Task BannerImage(Context ctx, Chao target)
        {
            ctx.CheckOwnMember(target);

            async Task ClearBannerImage()
            {
                await _repo.UpdateMember(target.Id, new() { BannerImage = null });
                await ctx.Reply($"{Emojis.Success} Member banner image cleared.");
            }

            async Task SetBannerImage(ParsedImage img)
            {
                await AvatarUtils.VerifyAvatarOrThrow(_client, img.Url, isFullSizeImage: true);

                await _repo.UpdateMember(target.Id, new() { BannerImage = img.Url });

                var msg = img.Source switch
                {
                    AvatarSource.Url => $"{Emojis.Success} Member banner image changed to the image at the given URL.",
                    AvatarSource.Attachment => $"{Emojis.Success} Member banner image changed to attached image.\n{Emojis.Warn} If you delete the message containing the attachment, the banner image will stop working.",
                    AvatarSource.User => throw new CWError("Cannot set a banner image to an user's avatar."),
                    _ => throw new ArgumentOutOfRangeException()
                };

                // The attachment's already right there, no need to preview it.
                var hasEmbed = img.Source != AvatarSource.Attachment;
                await (hasEmbed
                    ? ctx.Reply(msg, embed: new EmbedBuilder().Image(new(img.Url)).Build())
                    : ctx.Reply(msg));
            }

            async Task ShowBannerImage()
            {
                if ((target.BannerImage?.Trim() ?? "").Length > 0)
                {
                    var eb = new EmbedBuilder()
                        .Title($"{target.Name}'s banner image")
                        .Image(new(target.BannerImage))
                        .Description($"To clear, use `pk;member {target.Hid} banner clear`.");
                    await ctx.Reply(embed: eb.Build());
                }
                else
                    throw new CWSyntaxError("This member does not have a banner image set. Set one by attaching an image to this command, or by passing an image URL or @mention.");
            }

            if (await ctx.MatchClear("this member's banner image"))
                await ClearBannerImage();
            else if (await ctx.MatchImage() is { } img)
                await SetBannerImage(img);
            else
                await ShowBannerImage();
        }

        public async Task Color(Context ctx, Chao target)
        {
            var color = ctx.RemainderOrNull();
            if (await ctx.MatchClear())
            {
                ctx.CheckOwnMember(target);

                var patch = new GardenPatch { Color = Partial<string>.Null() };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply($"{Emojis.Success} Member color cleared.");
            }
            else if (!ctx.HasNext())
            {
                // if (!target.ColorPrivacy.CanAccess(ctx.LookupContextFor(target.Garden)))
                //     throw Errors.LookupNotAllowed;

                if (target.Color == null)
                    if (ctx.System?.Id == target.Garden)
                        await ctx.Reply(
                            $"This member does not have a color set. To set one, type `pk;member {target.Reference()} color <color>`.");
                    else
                        await ctx.Reply("This member does not have a color set.");
                else
                    await ctx.Reply(embed: new EmbedBuilder()
                        .Title("Member color")
                        .Color(target.Color.ToDiscordColor())
                        .Thumbnail(new($"https://fakeimg.pl/256x256/{target.Color}/?text=%20"))
                        .Description($"This member's color is **#{target.Color}**."
                                         + (ctx.System?.Id == target.Garden ? $" To clear it, type `pk;member {target.Reference()} color -clear`." : ""))
                        .Build());
            }
            else
            {
                ctx.CheckOwnMember(target);

                if (color.StartsWith("#")) color = color.Substring(1);
                if (!Regex.IsMatch(color, "^[0-9a-fA-F]{6}$")) throw Errors.InvalidColorError(color);

                var patch = new GardenPatch { Color = Partial<string>.Present(color.ToLowerInvariant()) };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply(embed: new EmbedBuilder()
                    .Title($"{Emojis.Success} Member color changed.")
                    .Color(color.ToDiscordColor())
                    .Thumbnail(new($"https://fakeimg.pl/256x256/{color}/?text=%20"))
                    .Build());
            }
        }
        public async Task Birthday(Context ctx, Chao target)
        {
            if (await ctx.MatchClear("this member's birthday"))
            {
                ctx.CheckOwnMember(target);

                var patch = new GardenPatch { Birthday = Partial<LocalDate?>.Null() };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply($"{Emojis.Success} Member birthdate cleared.");
            }
            else if (!ctx.HasNext())
            {
                if (target.Birthday == null)
                    await ctx.Reply("This member does not have a birthdate set."
                        + (ctx.System?.Id == target.Garden ? $" To set one, type `pk;member {target.Reference()} birthdate <birthdate>`." : ""));
                else
                    await ctx.Reply($"This member's birthdate is **{target.BirthdayString}**."
                                    + (ctx.System?.Id == target.Garden ? $" To clear it, type `pk;member {target.Reference()} birthdate -clear`." : ""));
            }
            else
            {
                ctx.CheckOwnMember(target);

                var birthdayStr = ctx.RemainderOrNull();
                var birthday = DateUtils.ParseDate(birthdayStr, true);
                if (birthday == null) throw Errors.BirthdayParseError(birthdayStr);

                var patch = new GardenPatch { Birthday = Partial<LocalDate?>.Present(birthday) };
                await _repo.UpdateMember(target.Id, patch);

                await ctx.Reply($"{Emojis.Success} Member birthdate changed.");
            }
        }

        private async Task<EmbedBuilder> CreateMemberNameInfoEmbed(Context ctx, Chao target)
        {
            MemberGuildSettings memberGuildConfig = null;
            if (ctx.Guild != null)
                memberGuildConfig = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);

            var eb = new EmbedBuilder()
                .Title($"Member names")
                .Footer(new($"Member ID: {target.Hid} | Active name in bold. Server name overrides display name, which overrides base name."));

            if (target.DisplayName == null && memberGuildConfig?.DisplayName == null)
                eb.Field(new("Name", $"**{target.Name}**"));
            else
                eb.Field(new("Name", target.Name));

            if (target.DisplayName != null && memberGuildConfig?.DisplayName == null)
                eb.Field(new("Display Name", $"**{target.DisplayName}**"));
            else
                eb.Field(new("Display Name", target.DisplayName ?? "*(none)*"));

            if (ctx.Guild != null)
            {
                if (memberGuildConfig?.DisplayName != null)
                    eb.Field(new($"Server Name (in {ctx.Guild.Name})", $"**{memberGuildConfig.DisplayName}**"));
                else
                    eb.Field(new($"Server Name (in {ctx.Guild.Name})", memberGuildConfig?.DisplayName ?? "*(none)*"));
            }

            return eb;
        }

        public async Task DisplayName(Context ctx, Chao target)
        {
            async Task PrintSuccess(string text)
            {
                var successStr = text;
                if (ctx.Guild != null)
                {
                    var memberGuildConfig = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);
                    if (memberGuildConfig.DisplayName != null)
                        successStr += $" However, this member has a server name set in this server ({ctx.Guild.Name}), and will be proxied using that name, \"{memberGuildConfig.DisplayName}\", here.";
                }

                await ctx.Reply(successStr);
            }

            var noDisplayNameSetMessage = "This member does not have a display name set.";
            if (ctx.System?.Id == target.Garden)
                noDisplayNameSetMessage += $" To set one, type `pk;member {target.Reference()} displayname <display name>`.";

            // No perms check, display name isn't covered by member privacy

            if (ctx.MatchRaw())
            {
                if (target.DisplayName == null)
                    await ctx.Reply(noDisplayNameSetMessage);
                else
                    await ctx.Reply($"```\n{target.DisplayName}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                var eb = await CreateMemberNameInfoEmbed(ctx, target);
                if (ctx.System?.Id == target.Garden)
                    eb.Description($"To change display name, type `pk;member {target.Reference()} displayname <display name>`."
                        + $"To clear it, type `pk;member {target.Reference()} displayname -clear`."
                        + $"To print the raw display name, type `pk;member {target.Reference()} displayname -raw`.");
                await ctx.Reply(embed: eb.Build());
                return;
            }

            ctx.CheckOwnMember(target);

            if (await ctx.MatchClear("this member's display name"))
            {
                var patch = new GardenPatch { DisplayName = Partial<string>.Null() };
                await _repo.UpdateMember(target.Id, patch);

                await PrintSuccess($"{Emojis.Success} Member display name cleared. This member will now be proxied using their member name \"{target.Name}\".");
            }
            else
            {
                var newDisplayName = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();

                var patch = new GardenPatch { DisplayName = Partial<string>.Present(newDisplayName) };
                await _repo.UpdateMember(target.Id, patch);

                await PrintSuccess($"{Emojis.Success} Member display name changed. This member will now be proxied using the name \"{newDisplayName}\".");
            }
        }

        public async Task ServerName(Context ctx, Chao target)
        {
            ctx.CheckGuildContext();

            var noServerNameSetMessage = "This member does not have a server name set.";
            if (ctx.System?.Id == target.Garden)
                noServerNameSetMessage += $" To set one, type `pk;member {target.Reference()} servername <server name>`.";

            // No perms check, display name isn't covered by member privacy
            var memberGuildConfig = await _repo.GetMemberGuild(ctx.Guild.Id, target.Id);

            if (ctx.MatchRaw())
            {

                if (memberGuildConfig.DisplayName == null)
                    await ctx.Reply(noServerNameSetMessage);
                else
                    await ctx.Reply($"```\n{memberGuildConfig.DisplayName}\n```");
                return;
            }
            if (!ctx.HasNext(false))
            {
                var eb = await CreateMemberNameInfoEmbed(ctx, target);
                if (ctx.System?.Id == target.Garden)
                    eb.Description($"To change server name, type `pk;member {target.Reference()} servername <server name>`.\nTo clear it, type `pk;member {target.Reference()} servername -clear`.\nTo print the raw server name, type `pk;member {target.Reference()} servername -raw`.");
                await ctx.Reply(embed: eb.Build());
                return;
            }

            ctx.CheckOwnMember(target);

            if (await ctx.MatchClear("this member's server name"))
            {
                await _repo.UpdateMemberGuild(target.Id, ctx.Guild.Id, new() { DisplayName = null });

                if (target.DisplayName != null)
                    await ctx.Reply($"{Emojis.Success} Member server name cleared. This member will now be proxied using their global display name \"{target.DisplayName}\" in this server ({ctx.Guild.Name}).");
                else
                    await ctx.Reply($"{Emojis.Success} Member server name cleared. This member will now be proxied using their member name \"{target.Name}\" in this server ({ctx.Guild.Name}).");
            }
            else
            {
                var newServerName = ctx.RemainderOrNull(skipFlags: false).NormalizeLineEndSpacing();

                await _repo.UpdateMemberGuild(target.Id, ctx.Guild.Id, new() { DisplayName = newServerName });

                await ctx.Reply($"{Emojis.Success} Member server name changed. This member will now be proxied using the name \"{newServerName}\" in this server ({ctx.Guild.Name}).");
            }
        }

        public async Task Delete(Context ctx, Chao target)
        {
            ctx.CheckGarden().CheckOwnMember(target);

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete \"{target.Name}\"? If so, reply to this message with the member's ID (`{target.Hid}`). __***This cannot be undone!***__");
            if (!await ctx.ConfirmWithReply(target.Hid)) throw Errors.MemberDeleteCancelled;

            await _repo.DeleteMember(target.Id);

            await ctx.Reply($"{Emojis.Success} Member deleted.");
        }
    }
}