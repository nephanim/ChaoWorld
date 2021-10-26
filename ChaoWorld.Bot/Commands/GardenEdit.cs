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

        public async Task Delete(Context ctx)
        {
            ctx.CheckGarden();

            await ctx.Reply($"{Emojis.Warn} Are you sure you want to delete your system? If so, reply to this message with your system's ID (`{ctx.Garden.Id}`).\n**Note: this action is permanent.**");
            if (!await ctx.ConfirmWithReply(ctx.Garden.Id.ToString()))
                throw new CWError($"Garden deletion cancelled. Note that you must reply with your system ID (`{ctx.Garden.Id}`) *verbatim*.");

            await _repo.DeleteGarden(ctx.Garden.Id);

            await ctx.Reply($"{Emojis.Success} Garden deleted.");
        }

        public async Task SystemProxy(Context ctx)
        {
            ctx.CheckGarden();

            var guild = ctx.MatchGuild() ?? ctx.Guild ??
                throw new CWError("You must run this command in a server or pass a server ID.");

            var gs = await _repo.GetSystemGuild(guild.Id, ctx.Garden.Id);

            string serverText;
            if (guild.Id == ctx.Guild?.Id)
                serverText = $"this server ({guild.Name.EscapeMarkdown()})";
            else
                serverText = $"the server {guild.Name.EscapeMarkdown()}";

            bool newValue;
            if (ctx.Match("on", "enabled", "true", "yes")) newValue = true;
            else if (ctx.Match("off", "disabled", "false", "no")) newValue = false;
            else if (ctx.HasNext()) throw new CWSyntaxError("You must pass either \"on\" or \"off\".");
            else
            {
                if (gs.ProxyEnabled)
                    await ctx.Reply($"Proxying in {serverText} is currently **enabled** for your system. To disable it, type `!system proxy off`.");
                else
                    await ctx.Reply($"Proxying in {serverText} is currently **disabled** for your system. To enable it, type `!system proxy on`.");
                return;
            }

            await _repo.UpdateSystemGuild(ctx.Garden.Id, guild.Id, new() { ProxyEnabled = newValue });

            if (newValue)
                await ctx.Reply($"Message proxying in {serverText} is now **enabled** for your system.");
            else
                await ctx.Reply($"Message proxying in {serverText} is now **disabled** for your system.");
        }
    }
}