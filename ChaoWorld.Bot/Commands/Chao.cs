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
using Myriad.Extensions;

namespace ChaoWorld.Bot
{
    public class Chao
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly EmbedService _embeds;
        private readonly HttpClient _client;

        public Chao(EmbedService embeds, IDatabase db, ModelRepository repo, HttpClient client)
        {
            _embeds = embeds;
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task ViewChao(Context ctx, Core.Chao target)
        {
            var garden = await _repo.GetGarden(target.GardenId.Value);
            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(ctx, garden, target));
        }

        public async Task PetChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} is soothed and smiling.");
        }

        public async Task RockChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} relaxes and coos in your arms.");
        }

        public async Task CuddleChao(Context ctx, Core.Chao target)
        {
            await ctx.Reply($"{Emojis.Heart} {target.Name} falls asleep in your arms.");
        }

        public async Task ChaoRankings(Context ctx)
        {
            await ctx.RenderChaoRankedList(_db);
        }

        public async Task Breed(Context ctx, Core.Chao mother)
        {
            ctx.CheckGarden();
            ctx.CheckOwnChao(mother);

            if (mother.IsFertile && mother.CurrentAge >= 1)
            {
                if (await ctx.MatchChao() is { } father)
                {
                    if (father.GardenId.Value != 0)
                    {
                        if (father.CurrentAge >= 1)
                        {
                            if (father.GardenId != mother.GardenId)
                            {
                                // This is someone else's chao, so we need to get their permission first
                                var fatherGarden = await _repo.GetGarden(father.GardenId.Value);
                                var accounts = await _repo.GetGardenAccounts(fatherGarden.Id);
                                var targetAccount = await ctx.Cache.GetOrFetchUser(ctx.Rest, accounts.FirstOrDefault());

                                if (targetAccount != null)
                                {
                                    // Make sure the target wants to mate (not everybody wants kids)
                                    if (!await ctx.PromptYesNo($"{targetAccount.Mention()} Would you like to mate {father.Name} with {ctx.Author.Username}'s {mother.Name}? {ctx.Author.Username} will receive the offspring.", "Accept", user: targetAccount, matchFlag: false))
                                        throw Errors.MatingCanceled();
                                }
                                else
                                {
                                    throw Errors.MatingCannotFindUser();
                                }
                            }

                            // At this point we're breeding within our own garden or at least have permission from the owner of the other chao
                            mother.IsFertile = false; // Update this first to prevent exploits where endless babies are created for free
                            await _repo.UpdateChao(mother);

                            // Read genes and make the baby
                            var motherGenes = await _repo.GetChaoGenes(mother.Id.Value);
                            var fatherGenes = await _repo.GetChaoGenes(father.Id.Value);
                            var childGenes = new Core.ChaoGenes();
                            var child = childGenes.InitializeFromParents(motherGenes, fatherGenes);
                            child = await _repo.CreateChao(ctx.Garden.Id, child);
                            childGenes.ChaoId = child.Id.Value; // Make sure the child's ID is attached to its genes, the DB does not allow nulls
                            await _repo.CreateChaoGenes(childGenes);

                            await ctx.Reply(embed: await _embeds.CreateChaoEmbed(ctx, ctx.Garden, child));
                            await ctx.Reply($"{Emojis.Success} Successfully bred {mother.Name} with {father.Name}! Your chao (ID: `{child.Id}`) is currently unnamed. Use `!chao {child.Id} rename {{new name}}` to give it a name.");
                        }
                        else
                            await ctx.Reply($"{Emojis.Error} {father.Name} is not interested in mating right now. Maybe someone older...");
                    }
                    else
                        await ctx.Reply($"{Emojis.Error} Professor Chao is very particular about his chao and doesn't allow strangers to mate with them. Sorry!");
                }
                else
                    await ctx.Reply($"{Emojis.Error} Please specify which chao {mother.Name} should mate with.");
            }
            else
                await ctx.Reply($"{Emojis.Error} {mother.Name} is not interested in mating right now.");
        }
    }
}