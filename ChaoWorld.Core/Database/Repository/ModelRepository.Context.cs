using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<MessageContext> GetMessageContext(ulong account, ulong guild, ulong channel)
            =>
                    _db.QuerySingleProcedure<MessageContext>("message_context", new
                    {
                        account_id = account,
                        guild_id = guild,
                        channel_id = channel
            });

    }
}