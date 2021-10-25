using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NodaTime;

using ChaoWorld.Core;

#nullable enable
namespace ChaoWorld.Bot
{
    public class ChaoListOptions
    {
        public SortProperty SortProperty { get; set; } = SortProperty.Name;
        public bool Reverse { get; set; }
        public string? Search { get; set; }
        public bool SearchDescription { get; set; }

        public ListType Type { get; set; }
        public bool IncludeMessageCount { get; set; }
        public bool IncludeLastSwitch { get; set; }
        public bool IncludeLastMessage { get; set; }
        public bool IncludeCreated { get; set; }
        public bool IncludeAvatar { get; set; }
        public bool IncludePronouns { get; set; }

        public string CreateFilterString()
        {
            var str = new StringBuilder();
            str.Append("Sorting ");
            if (SortProperty != SortProperty.Random) str.Append("by ");
            str.Append(SortProperty switch
            {
                SortProperty.Name => "chao name",
                SortProperty.CreationDate => "creation date",
                SortProperty.Random => "randomly",
                _ => new ArgumentOutOfRangeException($"Couldn't find readable string for sort property {SortProperty}")
            });

            if (Search != null)
            {
                str.Append($", searching for \"{Search}\"");
                if (SearchDescription) str.Append(" (including description)");
            }

            return str.ToString();
        }

        public DatabaseViewsExt.ChaoListQueryOptions ToQueryOptions() =>
            new DatabaseViewsExt.ChaoListQueryOptions
            {
                Search = Search,
                SearchDescription = SearchDescription
            };
    }

    public static class ChaoListOptionsExt
    {
        public static IEnumerable<ListedChao> SortByChaoListOptions(this IEnumerable<ListedChao> input, ChaoListOptions opts)
        {
            IComparer<T> ReverseMaybe<T>(IComparer<T> c) =>
                opts.Reverse ? Comparer<T>.Create((a, b) => c.Compare(b, a)) : c;

            var randGen = new global::System.Random();

            var culture = StringComparer.InvariantCultureIgnoreCase;
            return (opts.SortProperty switch
            {
                // As for the OrderByDescending HasValue calls: https://www.jerriepelser.com/blog/orderby-with-null-values/
                // We want nulls last no matter what, even if orders are reversed
                SortProperty.Name => input.OrderBy(m => m.Name, ReverseMaybe(culture)),
                SortProperty.CreationDate => input.OrderBy(m => m.Created, ReverseMaybe(Comparer<Instant>.Default)),
                SortProperty.Random => input
                    .OrderBy(m => randGen.Next()),
                _ => throw new ArgumentOutOfRangeException($"Unknown sort property {opts.SortProperty}")
            })
                // Lastly, add a by-name fallback order for collisions (generally hits w/ lots of null values)
                .ThenBy(m => m.Name, culture);
        }
    }

    public enum SortProperty
    {
        Name,
        CreationDate,
        Random
    }

    public enum ListType
    {
        Short,
        Long
    }
}