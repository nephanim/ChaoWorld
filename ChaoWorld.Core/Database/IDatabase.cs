using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public interface IDatabase
    {
        Task ApplyMigrations();
        Task<IChaoWorldConnection> Obtain();
        Task Execute(Func<IChaoWorldConnection, Task> func);
        Task<T> Execute<T>(Func<IChaoWorldConnection, Task<T>> func);
        IAsyncEnumerable<T> Execute<T>(Func<IChaoWorldConnection, IAsyncEnumerable<T>> func);
        Task<int> ExecuteQuery(Query q, string extraSql = "", [CallerMemberName] string queryName = "");
        Task<int> ExecuteQuery(IChaoWorldConnection? conn, Query q, string extraSql = "", [CallerMemberName] string queryName = "");
        Task<T> QueryFirst<T>(Query q, string extraSql = "", [CallerMemberName] string queryName = "");
        Task<T> QueryFirst<T>(IChaoWorldConnection? conn, Query q, string extraSql = "", [CallerMemberName] string queryName = "");
        Task<IEnumerable<T>> Query<T>(Query q, [CallerMemberName] string queryName = "");
        IAsyncEnumerable<T> QueryStream<T>(Query q, [CallerMemberName] string queryName = "");
        Task<T> QuerySingleProcedure<T>(string queryName, object param);
        Task<IEnumerable<T>> QueryProcedure<T>(string queryName, object param);
    }
}