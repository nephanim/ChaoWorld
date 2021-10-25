using System;
using System.Collections.Generic;

using SqlKata;

namespace ChaoWorld.Core
{
    internal class QueryPatchWrapper
    {
        private Dictionary<string, object> _dict = new();

        public QueryPatchWrapper With<T>(string columnName, Partial<T> partialValue)
        {
            if (partialValue.IsPresent)
                _dict.Add(columnName, partialValue);

            return this;
        }

        public Query ToQuery(Query q) => q.AsUpdate(_dict);
    }

    internal static class SqlKataExtensions
    {
        internal static Query ApplyPatch(this Query query, Func<QueryPatchWrapper, QueryPatchWrapper> func)
            => func(new QueryPatchWrapper()).ToQuery(query);
    }
}