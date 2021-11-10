using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Humanizer;

using Myriad.Builders;

using NodaTime;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public static class ContextListExt
    {
        public static ChaoListOptions ParseChaoListOptions(this Context ctx)
        {
            var p = new ChaoListOptions();

            // Short or long list? (parse this first, as it can potentially take a positional argument)
            var isFull = ctx.Match("f", "full", "big", "details", "long") || ctx.MatchFlag("f", "full");
            p.Type = isFull ? ListType.Long : ListType.Short;

            // Search query
            if (ctx.HasNext())
                p.Search = ctx.RemainderOrNull();

            // Sort property (default is by name, but adding a flag anyway, 'cause why not)
            if (ctx.MatchFlag("by-name", "bn")) p.SortProperty = SortProperty.Name;
            if (ctx.MatchFlag("by-created", "bc", "bcd")) p.SortProperty = SortProperty.CreationDate;
            if (ctx.MatchFlag("random")) p.SortProperty = SortProperty.Random;

            // Sort reverse?
            if (ctx.MatchFlag("r", "rev", "reverse"))
                p.Reverse = true;

            // Additional fields to include in the search results
            if (ctx.MatchFlag("with-created", "wc"))
                p.IncludeCreated = true;

            // Always show the sort property, too
            if (p.SortProperty == SortProperty.CreationDate) p.IncludeCreated = true;

            // Done!
            return p;
        }

        public static async Task RenderChaoList(this Context ctx, IDatabase db, GardenId garden, string embedTitle, string color, ChaoListOptions opts)
        {
            // We take an IDatabase instead of a IChaoWorldConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var chao = (await db.Execute(conn => conn.QueryChaoList(garden, opts.ToQueryOptions())))
                .SortByChaoListOptions(opts)
                .ToList();

            var itemsPerPage = opts.Type == ListType.Short ? 20 : 5;
            await ctx.Paginate(chao.ToAsyncEnumerable(), chao.Count, itemsPerPage, embedTitle, color, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<ListedChao> page)
            {
                // Add a global footer with the filter/sort string + result count
                eb.Footer(new($"{opts.CreateFilterString()}. {"result".ToQuantity(chao.Count)}."));

                // Then call the specific renderers
                if (opts.Type == ListType.Short)
                    ShortRenderer(eb, page);
                else
                    LongRenderer(eb, page);

                return Task.CompletedTask;
            }

            void ShortRenderer(EmbedBuilder eb, IEnumerable<ListedChao> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{m.Id}`] **{m.Name}** - {m.Appearance} ({m.SwimGrade}{m.FlyGrade}{m.RunGrade}{m.PowerGrade}{m.StaminaGrade})";

                    switch (opts.SortProperty)
                    {
                        case SortProperty.CreationDate:
                            {
                                ret += $"(created at <t:{m.CreatedOn.ToUnixTimeSeconds()}>)";
                                break;
                            }
                        default:
                            {
                                if (opts.IncludeCreated)
                                    ret += $"(created at <t:{m.CreatedOn.ToUnixTimeSeconds()}>)";
                                break;
                            }
                    }
                    return ret;
                }));
            }

            void LongRenderer(EmbedBuilder eb, IEnumerable<ListedChao> page)
            {
                var zone = DateTimeZone.Utc;
                foreach (var m in page)
                {
                    var profile = new StringBuilder($"**ID**: {m.Id}");
                    profile.Append($"\n**Age**: {m.CurrentAge}");
                    profile.Append($"\n**Reincarnations**: {m.Reincarnations}");
                    profile.Append($"\n**Type**: {m.Appearance}");
                    profile.Append($"\n**Stat Grades**: {m.SwimGrade}{m.FlyGrade}{m.RunGrade}{m.PowerGrade}{m.StaminaGrade}");
                    if (opts.IncludeCreated || opts.SortProperty == SortProperty.CreationDate)
                        profile.Append($"\n**Created on**: {m.CreatedOn.FormatZoned(zone)}");

                    eb.Field(new(m.Name, profile.ToString().Truncate(1024)));
                }
            }
        }

        public static async Task RenderRaceList(this Context ctx, IDatabase db, bool includeCompletedRaces, bool includeIncompleteRaces, string title, string search)
        {
            // We take an IDatabase instead of a IChaoWorldConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var races = (await db.Execute(conn => conn.QueryRaceList(includeCompletedRaces, includeIncompleteRaces, search)))
                .OrderBy(x => x.State).ThenByDescending(x => x.CompletedOn).ThenBy(x => x.Difficulty).ThenBy(x => x.Name)
                .ToList();

            var itemsPerPage = 20; //Maybe do a long list in the future too
            await ctx.Paginate(races.ToAsyncEnumerable(), races.Count, itemsPerPage, title, null, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<ListedRace> page)
            {
                // Add a global footer with the filter/sort string + result count
                eb.Footer(new($"{"result".ToQuantity(races.Count)}"));
                eb.Thumbnail(new("https://nephanim.com/chao/resources/misc/chaorace.jpg"));

                // Then call the specific renderers
                ShortRenderer(eb, page);
                return Task.CompletedTask;
            }

            void ShortRenderer(EmbedBuilder eb, IEnumerable<ListedRace> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{m.Id}`] **{m.Name}** {Core.Race.GetDifficultyString(m.Difficulty)} ({m.State.GetDescription()})";
                    if (m.WinnerChaoId.HasValue && m.CompletedOn.HasValue)
                        ret += $" • Winner: {m.WinnerName} • <t:{m.CompletedOn.Value.ToUnixTimeSeconds()}>";
                    return ret;
                }));
            }
        }

        public static async Task RenderTournamentList(this Context ctx, IDatabase db, bool includeCompleted, bool includeIncomplete, string title, string search)
        {
            // We take an IDatabase instead of a IChaoWorldConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var instances = (await db.Execute(conn => conn.QueryTournamentList(includeCompleted, includeIncomplete, search)))
                .OrderBy(x => x.State).ThenByDescending(x => x.CompletedOn).ThenBy(x => x.TournamentId)
                .ToList();

            var itemsPerPage = 20; //Maybe do a long list in the future too
            await ctx.Paginate(instances.ToAsyncEnumerable(), instances.Count, itemsPerPage, title, null, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<ListedTournament> page)
            {
                // Add a global footer with the filter/sort string + result count
                eb.Footer(new($"{"result".ToQuantity(instances.Count)}"));
                eb.Thumbnail(new(MiscUtils.GenerateThumbnailForTournament()));

                // Then call the specific renderers
                ShortRenderer(eb, page);
                return Task.CompletedTask;
            }

            void ShortRenderer(EmbedBuilder eb, IEnumerable<ListedTournament> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{m.Id}`] **{m.Name}** ({m.State.GetDescription()})";
                    if (m.WinnerChaoId.HasValue && m.CompletedOn.HasValue)
                        ret += $" • Winner: {m.WinnerName} • <t:{m.CompletedOn.Value.ToUnixTimeSeconds()}>";
                    return ret;
                }));
            }
        }

        public static async Task RenderInventory(this Context ctx, IDatabase db, Core.Item.ItemCategories[] includeItemCategories, Core.Item.ItemTypes[] includeItemTypes, string title)
        {
            // We take an IDatabase instead of a IChaoWorldConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var items = (await db.Execute(conn => conn.QueryItemList(ctx.Garden.Id.Value, includeItemCategories, includeItemTypes)))
                .OrderBy(x => x.ItemType.GetDescription())
                .ToList();

            var itemsPerPage = 25; //Maybe do a long list in the future too
            await ctx.Paginate(items.ToAsyncEnumerable(), items.Count, itemsPerPage, title, null, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<Core.Item> page)
            {
                // Add a global footer with the filter/sort string + result count
                eb.Footer(new($"{"result".ToQuantity(items.Count)}"));

                // Then call the specific renderers
                ShortRenderer(eb, page);
                return Task.CompletedTask;
            }

            void ShortRenderer(EmbedBuilder eb, IEnumerable<Core.Item> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{m.Id}`] {m.ItemType.GetDescription()} x{m.Quantity}";
                    return ret;
                }));
            }
        }

        public static async Task RenderMarketList(this Context ctx, IDatabase db)
        {
            // We take an IDatabase instead of a IChaoWorldConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var items = (await db.Execute(conn => conn.QueryMarketList())).ToList();

            var itemsPerPage = 25; //Maybe do a long list in the future too
            await ctx.Paginate(items.ToAsyncEnumerable(), items.Count, itemsPerPage, "Black Market", null, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<MarketItem> page)
            {
                // Add a global footer with the result count and time until refresh
                var minutesUntilRefresh = (int)System.Math.Ceiling(60 - System.DateTime.Now.TimeOfDay.TotalMinutes % 60);
                eb.Footer(new($"{"result".ToQuantity(items.Count)} - Refreshes in {minutesUntilRefresh} minute(s)"));
                eb.Thumbnail(new("https://nephanim.com/chao/resources/misc/blackmarket.jpg"));

                // Then call the specific renderers
                ShortRenderer(eb, page);
                return Task.CompletedTask;
            }

            void ShortRenderer(EmbedBuilder eb, IEnumerable<MarketItem> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{(int)m.ItemType}`] {m.ItemType.GetDescription()} x{m.Quantity} ({string.Format("{0:n0}", m.Price)} rings ea.)";
                    return ret;
                }));
            }
        }
    }
}