using System.Threading.Tasks;

using NodaTime;

using ChaoWorld.Core;

using Serilog;

namespace ChaoWorld.Bot
{
    public class CommandMessageService
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IClock _clock;
        private readonly ILogger _logger;

        public CommandMessageService(IDatabase db, ModelRepository repo, IClock clock, ILogger logger)
        {
            _db = db;
            _repo = repo;
            _clock = clock;
            _logger = logger.ForContext<CommandMessageService>();
        }

        public async Task RegisterMessage(ulong messageId, ulong channelId, ulong authorId)
        {
            _logger.Debug("Registering command response {MessageId} from author {AuthorId} in {ChannelId}", messageId, authorId, channelId);
            await _repo.SaveCommandMessage(messageId, channelId, authorId);
        }

        public async Task<CommandMessage?> GetCommandMessage(ulong messageId)
        {
            return await _repo.GetCommandMessage(messageId);
        }
    }
}