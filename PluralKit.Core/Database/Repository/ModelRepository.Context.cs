using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<MessageContext> GetMessageContext(ulong account, ulong guild, ulong channel)
            => _db.QuerySingleProcedure<MessageContext>("message_context", new
            {
                account_id = account,
                guild_id = guild,
                channel_id = channel
            });

        public Task<IEnumerable<ProxyChao>> GetProxyChao(ulong account, ulong guild)
            => _db.QueryProcedure<ProxyChao>("proxy_chao", new
            {
                account_id = account,
                guild_id = guild
            });
    }
}