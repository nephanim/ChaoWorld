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

        public static CWError NotOwnChaoError => new CWError($"You can only run this command on your own chao.");
        public static CWError NoGardenError => new CWError("You do not have a chao garden registered. To create one, type `!garden new`.");
        public static CWError ExistingGardenError => new CWError("You already have a chao garden registered. To view it, type `!garden`.");
        public static CWError MissingChaoError => new CWSyntaxError("You need to specify a chao to run this command on.");

        public static CWError StringTooLongError(string name, int length, int maxLength) => new CWError($"{name} too long ({length}/{maxLength} characters).");

        public static CWError ChaoLimitReachedError(int limit) => new CWError($"Garden has reached the maximum number of chao ({limit}). Please get rid of unused chao first in order to create new ones.");

        public static CWError InvalidColorError(string color) => new CWError($"\"{color}\" is not a valid color. Color must be in 6-digit RGB hex format (eg. #ff0000).");

        public static CWError ChaoDeleteCancelled => new CWError($"Your chao is happy to stay with you. {Emojis.ThumbsUp}");
        public static CWError AvatarServerError(HttpStatusCode statusCode) => new CWError($"Server responded with status code {(int)statusCode}, are you sure your link is working?");
        public static CWError AvatarFileSizeLimit(long size) => new CWError($"File size too large ({size.Bytes().ToString("#.#")} > {Limits.AvatarFileSizeLimit.Bytes().ToString("#.#")}), try shrinking or compressing the image.");
        public static CWError AvatarNotAnImage(string mimeType) => new CWError($"The given link does not point to an image{(mimeType != null ? $" ({mimeType})" : "")}. Make sure you're using a direct link (ending in .jpg, .png, .gif).");
        public static CWError AvatarDimensionsTooLarge(int width, int height) => new CWError($"Image too large ({width}x{height} > {Limits.AvatarDimensionLimit}x{Limits.AvatarDimensionLimit}), try resizing the image.");
        public static CWError AvatarInvalid => new CWError($"Could not read image file - perhaps it's corrupted or the wrong format. Try a different image.");
        public static CWError UserHasNoAvatar => new CWError("The given user has no avatar set.");
        public static CWError InvalidUrl(string url) => new CWError($"The given URL is invalid.");
        public static CWError UrlTooLong(string url) => new CWError($"The given URL is too long ({url.Length}/{Limits.MaxUriLength} characters).");

        public static CWError AccountAlreadyLinked => new CWError("That account is already linked to your garden.");
        public static CWError AccountNotLinked => new CWError("That account isn't linked to your garden.");
        public static CWError AccountInOtherSystem(Core.Garden system) => new CWError($"The mentioned account is already linked to another garden (see `!garden {system.Id}`).");
        public static CWError UnlinkingLastAccount => new CWError("Since this is the only account linked to this system, you cannot unlink it (as that would leave your system account-less). If you would like to delete your system, use `!system delete`.");
        public static CWError ChaoLinkCancelled => new CWError("Chao link cancelled.");
        public static CWError ChaoUnlinkCancelled => new CWError("Chao unlink cancelled.");

        public static CWError InvalidDateTime(string str) => new CWError($"Could not parse '{str}' as a valid date/time. Try using a syntax such as \"May 21, 12:30 PM\" or \"3d12h\" (ie. 3 days, 12 hours ago).");

        public static CWError TimezoneParseError(string timezone) => new CWError($"Could not parse timezone offset {timezone}. Offset must be a value like 'UTC+5' or 'GMT-4:30'.");

        public static CWError InvalidTimeZone(string zoneStr) => new CWError($"Invalid time zone ID '{zoneStr}'. To find your time zone ID, use the following website: <https://xske.github.io/tz>");
        public static CWError TimezoneChangeCancelled => new CWError("Time zone change cancelled.");
        public static CWError AmbiguousTimeZone(string zoneStr, int count) => new CWError($"The time zone query '{zoneStr}' resulted in **{count}** different time zone regions. Try being more specific - e.g. pass an exact time zone specifier from the following website: <https://xske.github.io/tz>");
        public static CWError MessageNotFound(ulong id) => new CWError($"Message with ID '{id}' not found. Are you sure it's a message proxied by ChaoWorld?");

        public static CWError DurationParseError(string durationStr) => new CWError($"Could not parse {durationStr.AsCode()} as a valid duration. Try a format such as `30d`, `1d3h` or `20m30s`.");

        public static CWError GuildNotFound(ulong guildId) => new CWError($"Guild with ID `{guildId}` not found, or I cannot access it. Note that you must be a chao of the guild you are querying.");

        public static CWError DisplayNameTooLong(string displayName, int maxLength) => new CWError(
            $"Display name too long ({displayName.Length} > {maxLength} characters). Use a shorter display name, or shorten your system tag.");

        public static CWError GenericCancelled() => new CWError("Operation cancelled.");

        public static CWError AttachmentTooLarge => new CWError("ChaoWorld cannot proxy attachments over 8 megabytes (as webhooks aren't considered as having Discord Nitro) :(");
        public static CWError LookupNotAllowed => new CWError("You do not have permission to access this information.");
        public static CWError ChannelNotFound(string channelString) => new CWError($"Channel \"{channelString}\" not found or is not in this server.");
    }
}