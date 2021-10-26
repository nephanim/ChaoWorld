using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Myriad.Builders;
using Myriad.Cache;
using Myriad.Extensions;
using Myriad.Rest;
using Myriad.Rest.Exceptions;
using Myriad.Types;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class EmbedService
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IDiscordCache _cache;
        private readonly DiscordApiClient _rest;

        public EmbedService(IDatabase db, ModelRepository repo, IDiscordCache cache, DiscordApiClient rest)
        {
            _db = db;
            _repo = repo;
            _cache = cache;
            _rest = rest;
        }

        private Task<(ulong Id, User? User)[]> GetUsers(IEnumerable<ulong> ids)
        {
            async Task<(ulong Id, User? User)> Inner(ulong id)
            {
                var user = await _cache.GetOrFetchUser(_rest, id);
                return (id, user);
            }

            return Task.WhenAll(ids.Select(Inner));
        }

        public async Task<Embed> CreateGardenEmbed(Context cctx, Core.Garden garden)
        {

            // Fetch/render info for all accounts simultaneously
            var accounts = await _repo.GetGardenAccounts(garden.Id);
            var users = (await GetUsers(accounts)).Select(x => x.User?.NameAndMention() ?? $"(deleted account {x.Id})");

            var chaoCount = await _repo.GetGardenChaoCount(garden.Id);

            var eb = new EmbedBuilder()
                .Title($"{cctx.Author.Username}'s Garden")
                .Footer(new($"Garden ID: {garden.Id} | Created on {garden.CreatedOn}"));

            eb.Field(new("Rings", garden.RingBalance.ToString(), true));

            if (chaoCount > 0)
                eb.Field(new($"Chao ({chaoCount})", $"(see `!garden {garden.Id} list` or `!garden {garden.Id} list full`)", true));
            else
                eb.Field(new($"Chao ({chaoCount})", "Add one with `!chao new`!", true));

            return eb.Build();
        }

        public async Task<Embed> CreateChaoEmbed(Core.Garden garden, Core.Chao chao, Guild guild)
        {
            var name = chao.Name;

            var eb = new EmbedBuilder()
                .Author(new(name))
                .Description(chao.Appearance)
                .Footer(new(
                    $"Garden ID: {garden.Id} | Chao ID: {chao.Id} {$"| Created on {chao.CreatedOn.FormatZoned(DateTimeZone.Utc)}"}"));

            eb.Field(new("Age", chao.Age.ToString()));
            eb.Field(new("Reincarnations", chao.Reincarnations.ToString()));
            eb.Field(new($"Swim (Lv.{chao.SwimLevel:D2})", $"Grade {chao.SwimGrade} - {chao.SwimProgress:D4}/1000 ({chao.SwimValue:D4})"));
            eb.Field(new($"Fly (Lv.{chao.FlyLevel:D2})", $"Grade {chao.FlyGrade} - {chao.FlyProgress:D4}/1000 ({chao.FlyValue:D4})"));
            eb.Field(new($"Run (Lv.{chao.RunLevel:D2})", $"Grade {chao.RunGrade} - {chao.RunProgress:D4}/1000 ({chao.RunValue:D4})"));
            eb.Field(new($"Power (Lv.{chao.PowerLevel:D2})", $"Grade {chao.PowerGrade} - {chao.PowerProgress:D4}/1000 ({chao.PowerValue:D4})"));
            eb.Field(new($"Stamina (Lv.{chao.StaminaLevel:D2})", $"Grade {chao.StaminaGrade} - {chao.StaminaProgress:D4}/1000 ({chao.StaminaValue:D4})"));
            eb.Field(new($"Intelligence (Lv.{chao.IntelligenceLevel:D2})", $"Grade {chao.IntelligenceGrade} - {chao.IntelligenceProgress:D4}/1000 ({chao.IntelligenceValue:D4})"));
            eb.Field(new($"Luck (Lv.{chao.LuckLevel:D2})", $"Grade {chao.LuckGrade} - {chao.LuckProgress:D4}/1000 ({chao.LuckValue:D4})"));

            return eb.Build();
        }
    }
}