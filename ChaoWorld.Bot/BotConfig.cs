namespace ChaoWorld.Bot
{
    public class BotConfig
    {
        public static readonly string[] DefaultPrefixes = { "!" };

        public string Token { get; set; }
        public ulong? ClientId { get; set; }

        // ASP.NET configuration merges arrays with defaults, so we leave this field nullable
        // and fall back to the separate default array at the use site :)
        // This does bind [] as null (therefore default) instead of an empty array, but I can live w/ that. 
        public string[] Prefixes { get; set; }

        public int? MaxShardConcurrency { get; set; }

        public ulong? AdminRole { get; set; }

        public ClusterSettings? Cluster { get; set; }

        public string? GatewayQueueUrl { get; set; }

        public record ClusterSettings
        {
            public string NodeName { get; set; }
            public int TotalShards { get; set; }
            public int TotalNodes { get; set; }
        }
    }
}