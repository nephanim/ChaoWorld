using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Myriad.Builders;

using NodaTime;
using NodaTime.Text;
using NodaTime.TimeZones;

using ChaoWorld.Core;

namespace ChaoWorld.Bot
{
    public class GardenEdit
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly HttpClient _client;

        public GardenEdit(IDatabase db, ModelRepository repo, HttpClient client)
        {
            _db = db;
            _repo = repo;
            _client = client;
        }

        public async Task SetActiveChao(Context ctx, Core.Chao chao)
        {
            ctx.CheckOwnChao(chao);


        }

        public async Task Delete(Context ctx)
        {
            ctx.CheckGarden();

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete your garden? If so, reply to this message with your system's ID (`{ctx.Garden.Id}`).\n**Note: this action is permanent.**");
            if (!await ctx.ConfirmWithReply(ctx.Garden.Id.ToString()))
                throw new CWError($"Garden deletion cancelled. Note that you must reply with your garden ID (`{ctx.Garden.Id}`) *verbatim*.");

            await _repo.DeleteGarden(ctx.Garden.Id);

            await ctx.Reply($"{Emojis.Success} Garden deleted.");
        }
    }
}