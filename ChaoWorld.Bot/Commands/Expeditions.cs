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
using System.Collections.Generic;
using Myriad.Rest.Types;
using NodaTime;

namespace ChaoWorld.Bot
{
    public class Expeditions
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Expeditions(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task ViewExpeditionInstance(Context ctx, ExpeditionInstance target)
        {
            var race = await _repo.GetExpeditionById(target.ExpeditionId);
            await ctx.Reply(embed: await _embeds.CreateExpeditionEmbed(ctx, race, target));
        }

        public async Task EnterChaoInExpedition(Context ctx, Core.Chao chao, Expedition expedition)
        {
            ctx.CheckOwnChao(chao); //You can only enter your own chao in an expedition

            var activeInRace = await _repo.GetActiveRaceByChao(chao.Id.Value);
            var activeInTourney = await _repo.GetActiveTournamentByChao(chao.Id.Value);
            var activeInExpedition = await _repo.GetActiveExpeditionByChao(chao.Id.Value);
            var allowedChannels = await _repo.ReadBroadcastChannels();

            if (ctx.Channel.Id != allowedChannels.Expeditions)
            {
                await ctx.Reply($"{Emojis.Error} Please use <#{allowedChannels.Expeditions}> to join expeditions.");
            }
            else if (ctx.Garden.InstanceLimit <= 0)
            {
                await ctx.Reply($"{Emojis.Error} You have reached the daily instance limit. You can join expeditions again tomorrow.");
            }
            else if (activeInRace != null)
            {
                // There's a chao in this garden that's already participating in a race.
                var activeRace = await _repo.GetRaceByInstanceId(activeInRace.Id);
                await ctx.Reply($"{Emojis.Error} {chao.Name} is already participating in a {activeRace.Name} Race. Please support your chao in that race first!");
            }
            else if (activeInTourney != null)
            {
                // There's a chao in this garden that's already participating in a tournament.
                var tourney = await _repo.GetTournamentById(activeInTourney.TournamentId);
                await ctx.Reply($"{Emojis.Error} {chao.Name} is already participating in a {tourney.Name} Tournament. Please support your chao's tournament first!");
            }
            else if (activeInExpedition != null)
            {
                // This chao is already on another expedition...
                var activeExpedition = await _repo.GetExpeditionById(activeInExpedition.ExpeditionId);
                await ctx.Reply($"{Emojis.Error} {chao.Name} is already participating in a {activeExpedition.Name} Expedition. Please support your chao on that expedition first!");
            }
            else
            {
                var instance = await _repo.GetJoinableInstanceForExpeditionId(expedition.Id);
                var currentChaoCount = 0;
                if (instance != null)
                    currentChaoCount = await _repo.GetExpeditionInstanceChaoCount(instance.Id);
                if (instance == null || currentChaoCount >= expedition.MaximumChao)
                    instance = await _repo.CreateExpeditionInstance(expedition, ctx.Garden.Id.Value); // We couldn't find a joinable expedition or we would exceed its limits, so make a new one

                // The expedition we're referencing at this point is not full - join it
                await _repo.JoinChaoToExpeditionInstance(instance, chao);
                currentChaoCount++;
                await ctx.Reply($"{Emojis.Success} {chao.Name} has joined the {expedition.Name} Expedition. Do your best!");

                // See whether this chao joining puts us at the required threshold to start
                if (currentChaoCount >= expedition.MinimumChao && instance.State == ExpeditionInstance.ExpeditionStates.New)
                {
                    // We've reached the minimum threshold, and haven't begun preparing the expedition
                    instance.State = ExpeditionInstance.ExpeditionStates.InProgress;
                    await _repo.UpdateExpeditionInstance(instance);

                    await ctx.Reply($"{Emojis.Megaphone} The {expedition.Name} Expedition has started (0% complete).");
                }
            }
        }

        public async Task LeaveExpedition(Context ctx, ExpeditionInstance instance)
        {
            ctx.CheckGarden();

            /*
            var activeRace = await _repo.GetActiveExpeditionByGarden(ctx.Garden.Id.Value);
            if (activeRace != null)
            {
                if (activeRace.State == RaceInstance.RaceStates.New || activeRace.State == RaceInstance.RaceStates.Preparing)
                {
                    var raceChao = await _repo.GetRaceInstanceChao(activeRace);
                    var chao = raceChao.FirstOrDefault(x => x.GardenId == ctx.Garden.Id);
                    if (chao != null)
                    {
                        await _repo.RemoveChaoFromRaceInstance(activeRace, chao);
                    }
                    await ctx.Reply($"{Emojis.Success} You are no longer waiting for the race to start.");
                } else
                    await ctx.Reply($"{Emojis.Error} You can no longer withdraw from the race. Please wait for it to finish.");
            }
            else
                await ctx.Reply($"{Emojis.Error} None of your chao are currently participating in a race.");
            */
        }

        public async Task UpdatePingSettings(Context ctx)
        {
            var allowPings = false;
            if (ctx.Match("enable", "on", "yes", "1", "true", "accept", "allow"))
                allowPings = true;
            await _repo.UpdateExpeditionPingSetting(ctx.Author.Id, allowPings);
            if (allowPings)
                await ctx.Reply($"{Emojis.Success} Expedition pings are now enabled.");
            else
                await ctx.Reply($"{Emojis.Success} Expedition pings are now disabled.");
        }

        private int GetPrizeAmount(Expedition expedition)
        {
            // This will reward anywhere from 50% to 150% of the listed prize amount for a race
            return (int)(expedition.PrizeRings * (0.5 + new Random().NextDouble()));
        }
    }
}