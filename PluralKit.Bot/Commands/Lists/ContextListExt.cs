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
        public static MemberListOptions ParseMemberListOptions(this Context ctx)
        {
            var p = new MemberListOptions();

            // Short or long list? (parse this first, as it can potentially take a positional argument)
            var isFull = ctx.Match("f", "full", "big", "details", "long") || ctx.MatchFlag("f", "full");
            p.Type = isFull ? ListType.Long : ListType.Short;

            // Search query
            if (ctx.HasNext())
                p.Search = ctx.RemainderOrNull();

            // Include description in search?
            if (ctx.MatchFlag("search-description", "filter-description", "in-description", "sd", "description", "desc"))
                p.SearchDescription = true;

            // Sort property (default is by name, but adding a flag anyway, 'cause why not)
            if (ctx.MatchFlag("by-name", "bn")) p.SortProperty = SortProperty.Name;
            if (ctx.MatchFlag("by-created", "bc", "bcd")) p.SortProperty = SortProperty.CreationDate;
            if (ctx.MatchFlag("random")) p.SortProperty = SortProperty.Random;

            // Sort reverse?
            if (ctx.MatchFlag("r", "rev", "reverse"))
                p.Reverse = true;

            // Additional fields to include in the search results
            if (ctx.MatchFlag("with-last-switch", "with-last-fronted", "with-last-front", "wls", "wlf"))
                p.IncludeLastSwitch = true;
            if (ctx.MatchFlag("with-last-message", "with-last-proxy", "wlm", "wlp"))
                p.IncludeLastMessage = true;
            if (ctx.MatchFlag("with-message-count", "wmc"))
                p.IncludeMessageCount = true;
            if (ctx.MatchFlag("with-created", "wc"))
                p.IncludeCreated = true;
            if (ctx.MatchFlag("with-avatar", "with-image", "wa", "wi", "ia", "ii", "img"))
                p.IncludeAvatar = true;
            if (ctx.MatchFlag("with-pronouns", "wp"))
                p.IncludePronouns = true;

            // Always show the sort property, too
            if (p.SortProperty == SortProperty.CreationDate) p.IncludeCreated = true;

            // Done!
            return p;
        }

        public static async Task RenderMemberList(this Context ctx, IDatabase db, GardenId system, string embedTitle, string color, MemberListOptions opts)
        {
            // We take an IDatabase instead of a IPKConnection so we don't keep the handle open for the entire runtime
            // We wanna release it as soon as the chao list is actually *fetched*, instead of potentially minutes later (paginate timeout)
            var chao = (await db.Execute(conn => conn.QueryMemberList(system, opts.ToQueryOptions())))
                .SortByMemberListOptions(opts)
                .ToList();

            var itemsPerPage = opts.Type == ListType.Short ? 25 : 5;
            await ctx.Paginate(chao.ToAsyncEnumerable(), chao.Count, itemsPerPage, embedTitle, color, Renderer);

            // Base renderer, dispatches based on type
            Task Renderer(EmbedBuilder eb, IEnumerable<ListedMember> page)
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

            void ShortRenderer(EmbedBuilder eb, IEnumerable<ListedMember> page)
            {
                // We may end up over the description character limit
                // so run it through a helper that "makes it work" :)
                eb.WithSimpleLineContent(page.Select(m =>
                {
                    var ret = $"[`{m.Hid}`] **{m.Name}** ";

                    switch (opts.SortProperty)
                    {
                        case SortProperty.CreationDate:
                            {
                                ret += $"(created at <t:{m.Created.ToUnixTimeSeconds()}>)";
                                break;
                            }
                        default:
                            {
                                if (opts.IncludeCreated)
                                    ret += $"(created at <t:{m.Created.ToUnixTimeSeconds()}>)";
                                break;
                            }
                    }
                    return ret;
                }));
            }

            void LongRenderer(EmbedBuilder eb, IEnumerable<ListedMember> page)
            {
                var zone = ctx.System?.Zone ?? DateTimeZone.Utc;
                foreach (var m in page)
                {
                    var profile = new StringBuilder($"**ID**: {m.Hid}");

                    if (m.DisplayName != null)
                        profile.Append($"\n**Display name**: {m.DisplayName}");

                    if (m.ProxyTags.Count > 0)
                        profile.Append($"\n**Proxy tags**: {m.ProxyTagsString()}");

                    if (opts.IncludeCreated || opts.SortProperty == SortProperty.CreationDate)
                        profile.Append($"\n**Created on:** {m.Created.FormatZoned(zone)}");

                    eb.Field(new(m.Name, profile.ToString().Truncate(1024)));
                }
            }
        }
    }
}