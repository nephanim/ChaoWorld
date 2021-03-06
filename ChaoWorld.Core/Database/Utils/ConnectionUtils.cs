using System.Collections.Generic;
using System.Data.Common;

using Dapper;

namespace ChaoWorld.Core
{
    public static class ConnectionUtils
    {
        public static async IAsyncEnumerable<T> QueryStreamAsync<T>(this IChaoWorldConnection conn, string sql, object param)
        {
            await using var reader = (DbDataReader)await conn.ExecuteReaderAsync(sql, param);
            var parser = reader.GetRowParser<T>();

            while (await reader.ReadAsync())
                yield return parser(reader);
        }
    }
}