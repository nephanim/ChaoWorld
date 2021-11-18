using System.Threading;
using System.Threading.Tasks;

using Dapper;

using SqlKata;
using ChaoWorld.Core.Models;
using System.Collections.Generic;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public async Task<BroadcastChannels> ReadBroadcastChannels()
        {
            return await _db.Execute(conn => conn.QuerySingleAsync<BroadcastChannels>("select * from broadcastchannels"));
        }
    }
}