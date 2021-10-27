using System.Threading;
using System.Threading.Tasks;

using Dapper;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public async Task UpdateStats()
        {
            await _db.Execute(conn => conn.ExecuteAsync("update info set garden_count = (select count(*) from gardens)"));
            await _db.Execute(conn => conn.ExecuteAsync("update info set chao_count = (select count(*) from chao)"));
        }

        public Task<Counts> GetStats()
            => _db.Execute(conn => conn.QuerySingleAsync<Counts>("select * from info"));

        public class Counts
        {
            public int GardenCount { get; }
            public int ChaoCount { get; }
        }
    }
}