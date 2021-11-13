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
        public static CWError NotOwnItemError => new CWError($"You can only run this command on your own item.");
        public static CWError NoGardenError => new CWError("You do not have a chao garden registered. To create one, type `!garden new`.");
        public static CWError ExistingGardenError => new CWError("You already have a chao garden registered. To view it, type `!garden`.");
        public static CWError StringTooLongError(string name, int length, int maxLength) => new CWError($"{name} too long ({length}/{maxLength} characters).");
        public static CWError ChaoDeleteCancelled => new CWError($"Your chao is happy to stay with you. {Emojis.ThumbsUp}");
        public static CWError AvatarServerError(HttpStatusCode statusCode) => new CWError($"Server responded with status code {(int)statusCode}, are you sure your link is working?");
        public static CWError AvatarFileSizeLimit(long size) => new CWError($"File size too large ({size.Bytes().ToString("#.#")} > {Limits.AvatarFileSizeLimit.Bytes().ToString("#.#")}), try shrinking or compressing the image.");
        public static CWError AvatarNotAnImage(string mimeType) => new CWError($"The given link does not point to an image{(mimeType != null ? $" ({mimeType})" : "")}. Make sure you're using a direct link (ending in .jpg, .png, .gif).");
        public static CWError AvatarDimensionsTooLarge(int width, int height) => new CWError($"Image too large ({width}x{height} > {Limits.AvatarDimensionLimit}x{Limits.AvatarDimensionLimit}), try resizing the image.");
        public static CWError AvatarInvalid => new CWError($"Could not read image file - perhaps it's corrupted or the wrong format. Try a different image.");
        public static CWError InvalidUrl(string url) => new CWError($"The given URL is invalid.");
        public static CWError UrlTooLong(string url) => new CWError($"The given URL is too long ({url.Length}/{Limits.MaxUriLength} characters).");
        public static CWError GenericCancelled() => new CWError("Operation cancelled.");
        public static CWError GiveItemCanceled() => new CWError("The item was rejected.");
        public static CWError AttachmentTooLarge => new CWError("ChaoWorld cannot proxy attachments over 8 megabytes (as webhooks aren't considered as having Discord Nitro) :(");
    }
}