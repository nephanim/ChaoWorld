using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Humanizer;
using NodaTime;
using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    /// <summary>
    /// An exception class representing user-facing errors caused when parsing and executing commands.
    /// </summary>
    public class CWError: Exception
    {
        public CWError(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// A subclass of <see cref="PKError"/> that represent command syntax errors, meaning they'll have their command
    /// usages printed in the message.
    /// </summary>
    public class CWSyntaxError: CWError
    {
        public CWSyntaxError(string message) : base(message)
        {
        }
    }

    public static class Errors
    {
        // TODO: is returning constructed errors and throwing them at call site a good idea, or should these be methods that insta-throw instead?
        // or should we just like... go back to inlining them? at least for the one-time-use commands

        public static CWError NotOwnGardenError => new CWError($"You can only run this command on your own garden.");
        public static CWError NotOwnChaoError => new CWError($"You can only run this command on your own chao.");
        public static CWError NotOwnGroupError => new CWError($"You can only run this command on your own group.");
        public static CWError NoGardenError => new CWError("You do not have a garden registered with Chao World. To create one, type `pk;system new`.");
        public static CWError ExistingGardenError => new CWError("You already have a garden registered with Chao World. To view it, type `pk;system`. If you'd like to delete your system and start anew, type `pk;system delete`, or if you'd like to unlink this account from it, type `pk;unlink`.");
        public static CWError MissingChaoError => new CWSyntaxError("You need to specify a chao to run this command on.");

        public static CWError StringTooLongError(string name, int length, int maxLength) => new CWError($"{name} too long ({length}/{maxLength} characters).");

        public static CWError MemberLimitReachedError(int limit) => new CWError($"Garden has reached the maximum number of members ({limit}). Please delete unused members first in order to create new ones.");

        public static CWError InvalidColorError(string color) => new CWError($"\"{color}\" is not a valid color. Color must be in 6-digit RGB hex format (eg. #ff0000).");
        public static CWError BirthdayParseError(string birthday) => new CWError($"\"{birthday}\" could not be parsed as a valid date. Try a format like \"2016-12-24\" or \"May 3 1996\".");
        public static CWError ProxyMustHaveText => new CWSyntaxError("Example proxy message must contain the string 'text'.");
        public static CWError ProxyMultipleText => new CWSyntaxError("Example proxy message must contain the string 'text' exactly once.");

        public static CWError MemberDeleteCancelled => new CWError($"Member deletion cancelled. Stay safe! {Emojis.ThumbsUp}");
        public static CWError AvatarServerError(HttpStatusCode statusCode) => new CWError($"Server responded with status code {(int)statusCode}, are you sure your link is working?");
        public static CWError AvatarFileSizeLimit(long size) => new CWError($"File size too large ({size.Bytes().ToString("#.#")} > {Limits.AvatarFileSizeLimit.Bytes().ToString("#.#")}), try shrinking or compressing the image.");
        public static CWError AvatarNotAnImage(string mimeType) => new CWError($"The given link does not point to an image{(mimeType != null ? $" ({mimeType})" : "")}. Make sure you're using a direct link (ending in .jpg, .png, .gif).");
        public static CWError AvatarDimensionsTooLarge(int width, int height) => new CWError($"Image too large ({width}x{height} > {Limits.AvatarDimensionLimit}x{Limits.AvatarDimensionLimit}), try resizing the image.");
        public static CWError AvatarInvalid => new CWError($"Could not read image file - perhaps it's corrupted or the wrong format. Try a different image.");
        public static CWError UserHasNoAvatar => new CWError("The given user has no avatar set.");
        public static CWError InvalidUrl(string url) => new CWError($"The given URL is invalid.");
        public static CWError UrlTooLong(string url) => new CWError($"The given URL is too long ({url.Length}/{Limits.MaxUriLength} characters).");

        public static CWError AccountAlreadyLinked => new CWError("That account is already linked to your system.");
        public static CWError AccountNotLinked => new CWError("That account isn't linked to your system.");
        public static CWError AccountInOtherSystem(Garden system) => new CWError($"The mentioned account is already linked to another system (see `pk;system {system.Hid}`).");
        public static CWError UnlinkingLastAccount => new CWError("Since this is the only account linked to this system, you cannot unlink it (as that would leave your system account-less). If you would like to delete your system, use `pk;system delete`.");
        public static CWError MemberLinkCancelled => new CWError("Member link cancelled.");
        public static CWError MemberUnlinkCancelled => new CWError("Member unlink cancelled.");

        public static CWError SameSwitch(ICollection<Chao> members, LookupContext ctx)
        {
            if (members.Count == 0) return new CWError("There's already no one in front.");
            if (members.Count == 1) return new CWError($"Member {members.First().NameFor(ctx)} is already fronting.");
            return new CWError($"Members {string.Join(", ", members.Select(m => m.NameFor(ctx)))} are already fronting.");
        }

        public static CWError DuplicateSwitchMembers => new CWError("Duplicate members in member list.");
        public static CWError SwitchMemberNotInSystem => new CWError("One or more switch members aren't in your own system.");

        public static CWError InvalidDateTime(string str) => new CWError($"Could not parse '{str}' as a valid date/time. Try using a syntax such as \"May 21, 12:30 PM\" or \"3d12h\" (ie. 3 days, 12 hours ago).");
        public static CWError SwitchTimeInFuture => new CWError("Can't move switch to a time in the future.");
        public static CWError NoRegisteredSwitches => new CWError("There are no registered switches for this system.");

        public static CWError SwitchMoveBeforeSecondLast(ZonedDateTime time) => new CWError($"Can't move switch to before last switch time ({time.FormatZoned()}), as it would cause conflicts.");
        public static CWError SwitchMoveCancelled => new CWError("Switch move cancelled.");
        public static CWError SwitchEditCancelled => new CWError("Switch edit cancelled.");
        public static CWError SwitchDeleteCancelled => new CWError("Switch deletion cancelled.");
        public static CWError TimezoneParseError(string timezone) => new CWError($"Could not parse timezone offset {timezone}. Offset must be a value like 'UTC+5' or 'GMT-4:30'.");

        public static CWError InvalidTimeZone(string zoneStr) => new CWError($"Invalid time zone ID '{zoneStr}'. To find your time zone ID, use the following website: <https://xske.github.io/tz>");
        public static CWError TimezoneChangeCancelled => new CWError("Time zone change cancelled.");
        public static CWError AmbiguousTimeZone(string zoneStr, int count) => new CWError($"The time zone query '{zoneStr}' resulted in **{count}** different time zone regions. Try being more specific - e.g. pass an exact time zone specifier from the following website: <https://xske.github.io/tz>");
        public static CWError NoImportFilePassed => new CWError("You must either pass an URL to a file as a command parameter, or as an attachment to the message containing the command.");
        public static CWError InvalidImportFile => new CWError("Imported data file invalid. Make sure this is a .json file directly exported from ChaoWorld or Tupperbox.");
        public static CWError ImportCancelled => new CWError("Import cancelled.");
        public static CWError MessageNotFound(ulong id) => new CWError($"Message with ID '{id}' not found. Are you sure it's a message proxied by ChaoWorld?");

        public static CWError DurationParseError(string durationStr) => new CWError($"Could not parse {durationStr.AsCode()} as a valid duration. Try a format such as `30d`, `1d3h` or `20m30s`.");
        public static CWError FrontPercentTimeInFuture => new CWError("Cannot get the front percent between now and a time in the future.");

        public static CWError GuildNotFound(ulong guildId) => new CWError($"Guild with ID `{guildId}` not found, or I cannot access it. Note that you must be a member of the guild you are querying.");

        public static CWError DisplayNameTooLong(string displayName, int maxLength) => new CWError(
            $"Display name too long ({displayName.Length} > {maxLength} characters). Use a shorter display name, or shorten your system tag.");
        public static CWError ProxyNameTooShort(string name) => new CWError($"The webhook's name, {name.AsCode()}, is shorter than two characters, and thus cannot be proxied. Please change the member name or use a longer system tag.");
        public static CWError ProxyNameTooLong(string name) => new CWError($"The webhook's name, {name.AsCode()}, is too long ({name.Length} > {Limits.MaxProxyNameLength} characters), and thus cannot be proxied. Please change the member name, display name or server display name, or use a shorter system tag.");

        public static CWError ProxyTagAlreadyExists(ProxyTag tagToAdd, Chao member) => new CWError($"That member already has the proxy tag {tagToAdd.ProxyString.AsCode()}. The member currently has these tags: {member.ProxyTagsString()}");
        public static CWError ProxyTagDoesNotExist(ProxyTag tagToRemove, Chao member) => new CWError($"That member does not have the proxy tag {tagToRemove.ProxyString.AsCode()}. The member currently has these tags: {member.ProxyTagsString()}");
        public static CWError LegacyAlreadyHasProxyTag(ProxyTag requested, Chao member) => new CWError($"This member already has more than one proxy tag set: {member.ProxyTagsString()}\nConsider using the {$"pk;member {member.Reference()} proxy add {requested.ProxyString}".AsCode()} command instead.");
        public static CWError EmptyProxyTags(Chao member) => new CWError($"The example proxy `text` is equivalent to having no proxy tags at all, since there are no symbols or brackets on either end. If you'd like to clear your proxy tags, use `pk;member {member.Reference()} proxy clear`.");

        public static CWError GenericCancelled() => new CWError("Operation cancelled.");

        public static CWError AttachmentTooLarge => new CWError("ChaoWorld cannot proxy attachments over 8 megabytes (as webhooks aren't considered as having Discord Nitro) :(");
        public static CWError LookupNotAllowed => new CWError("You do not have permission to access this information.");
        public static CWError ChannelNotFound(string channelString) => new CWError($"Channel \"{channelString}\" not found or is not in this server.");
    }
}