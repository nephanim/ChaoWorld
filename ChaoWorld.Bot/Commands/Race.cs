using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Linq;

using Dapper;

using Myriad.Builders;

using Newtonsoft.Json.Linq;

using ChaoWorld.Core;
using System.Threading;

namespace ChaoWorld.Bot
{
    public class Race
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Race(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task NewRaceInstance(Context ctx, Core.Race race)
        {
            await using var conn = await _db.Obtain();

            // Create the instance
            var raceInstance = await _repo.CreateRaceInstance(race);

            // Send confirmation
            await ctx.Reply($"{Emojis.Megaphone} {race.Name} is now available. Use `!race {raceInstance.Id} chao {{id/name}}` to participate.");
        }

        public async Task EnterChaoInRace(Context ctx, Core.Chao chao, Core.RaceInstance raceInstance)
        {
            ctx.CheckOwnChao(chao); //You can only enter your own chao in a race...

            // TODO: Check whether we've reached the minimum number of chao for the race

            // If we've reached the minimum threshold, check how long we're supposed to wait before we start
            var race = await _repo.GetRaceByInstanceId(raceInstance.Id);
            var readyDelay = TimeSpan.FromMinutes(race.ReadyDelayMinutes);
            var startTimer = new Timer(_ =>
            {
                var __ = StartRaceAfterDelay(ctx, race);
            }, null, readyDelay, TimeSpan.FromMinutes(1));
        }

        public async Task StartRaceAfterDelay(Context ctx, Core.Race race)
        {
            // The ready delay period is over -- time to race!


        }

        /*
        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var system = await _repo.GetGarden(target.GardenId);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(system, target, ctx.Guild));
        }
        */
    }
}