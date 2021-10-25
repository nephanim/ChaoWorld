#nullable enable

using NodaTime;

namespace ChaoWorld.Core
{
    /// <summary>
    /// Model for the `message_context` PL/pgSQL function in `functions.sql`
    /// </summary>
    public class MessageContext
    {
        public GardenId? GardenId { get; }

        //public ulong? LastMessage { get; }
    }
}