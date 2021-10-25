using System.Threading.Tasks;

using Myriad.Gateway;

namespace ChaoWorld.Bot
{
    public interface IEventHandler<in T> where T : IGatewayEvent
    {
        Task Handle(Shard shard, T evt);

        ulong? ErrorChannelFor(T evt) => null;
    }
}