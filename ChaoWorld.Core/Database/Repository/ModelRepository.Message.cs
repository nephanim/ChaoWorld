using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task AddMessage(PKMessage msg)
        {
            var query = new Query("messages").AsInsert(new
            {
                mid = msg.Mid,
                guild = msg.Guild,
                channel = msg.Channel,
                chao = msg.Chao,
                sender = msg.Sender,
                original_mid = msg.OriginalMid,
            });
            _logger.Debug("Stored message {@StoredMessage} in channel {Channel}", msg, msg.Channel);

            // "on conflict do nothing" in the (pretty rare) case of duplicate events coming in from Discord, which would lead to a DB error before
            return _db.ExecuteQuery(query, extraSql: "on conflict do nothing");
        }

        // todo: add a Mapper to QuerySingle and move this to SqlKata
        public async Task<FullMessage?> GetMessage(IChaoWorldConnection conn, ulong id)
        {
            FullMessage Mapper(PKMessage msg, Chao chao, Garden system) =>
                new FullMessage { Message = msg, System = system, Chao = chao };

            var result = await conn.QueryAsync<PKMessage, Chao, Garden, FullMessage>(
                "select messages.*, chao.*, systems.* from messages, chao, systems where (mid = @Id or original_mid = @Id) and messages.chao = chao.id and systems.id = chao.system",
                Mapper, new { Id = id });
            return result.FirstOrDefault();
        }

        public async Task DeleteMessage(ulong id)
        {
            var query = new Query("messages").AsDelete().Where("mid", id);
            var rowCount = await _db.ExecuteQuery(query);
            if (rowCount > 0)
                _logger.Information("Deleted message {MessageId} from database", id);
        }

        public async Task DeleteMessagesBulk(IReadOnlyCollection<ulong> ids)
        {
            // Npgsql doesn't support ulongs in general - we hacked around it for plain ulongs but tbh not worth it for collections of ulong
            // Hence we map them to single longs, which *are* supported (this is ok since they're Technically (tm) stored as signed longs in the db anyway)
            var query = new Query("messages").AsDelete().WhereIn("mid", ids.Select(id => (long)id).ToArray());
            var rowCount = await _db.ExecuteQuery(query);
            if (rowCount > 0)
                _logger.Information("Bulk deleted messages ({FoundCount} found) from database: {MessageIds}", rowCount,
                    ids);
        }

        public Task<PKMessage?> GetLastMessage(ulong guildId, ulong channelId, ulong accountId)
        {
            // Want to index scan on the (guild, sender, mid) index so need the additional constraint
            var query = new Query("messages")
                .Where("guild", guildId)
                .Where("channel", channelId)
                .Where("sender", accountId)
                .OrderByDesc("mid")
                .Limit(1);

            return _db.QueryFirst<PKMessage?>(query);
        }
    }

    public class PKMessage
    {
        public ulong Mid { get; set; }
        public ulong? Guild { get; set; } // null value means "no data" (ie. from before this field being added)
        public ulong Channel { get; set; }
        public ChaoId Chao { get; set; }
        public ulong Sender { get; set; }
        public ulong? OriginalMid { get; set; }
    }

    public class FullMessage
    {
        public PKMessage Message;
        public Chao Chao;
        public Garden System;
    }
}