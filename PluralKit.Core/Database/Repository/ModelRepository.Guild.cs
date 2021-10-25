using System.Threading.Tasks;

using SqlKata;

namespace ChaoWorld.Core
{
    public partial class ModelRepository
    {
        public Task<GuildConfig> GetGuild(ulong guild)
        {
            var query = new Query("servers").AsInsert(new { id = guild });
            // sqlkata doesn't support postgres on conflict, so we just hack it on here
            return _db.QueryFirst<GuildConfig>(query, "on conflict (id) do update set id = @$1 returning *");
        }

        public Task UpdateGuild(ulong guild, GuildPatch patch)
        {
            _logger.Information("Updated guild {GuildId}: {@GuildPatch}", guild, patch);
            var query = patch.Apply(new Query("servers").Where("id", guild));
            return _db.ExecuteQuery(query, extraSql: "returning *");
        }


        public Task<SystemGuildSettings> GetSystemGuild(ulong guild, GardenId system)
        {
            var query = new Query("system_guild").AsInsert(new
            {
                guild = guild,
                system = system
            });
            return _db.QueryFirst<SystemGuildSettings>(query,
                extraSql: "on conflict (guild, system) do update set guild = $1, system = $2 returning *"
            );
        }

        public Task UpdateSystemGuild(GardenId system, ulong guild, SystemGuildPatch patch)
        {
            _logger.Information("Updated {GardenId} in guild {GuildId}: {@SystemGuildPatch}", system, guild, patch);
            var query = patch.Apply(new Query("system_guild").Where("system", system).Where("guild", guild));
            return _db.ExecuteQuery(query, extraSql: "returning *");
        }


        public Task<MemberGuildSettings> GetMemberGuild(ulong guild, ChaoId chao)
        {
            var query = new Query("chao_guild").AsInsert(new
            {
                guild = guild,
                chao = chao
            });
            return _db.QueryFirst<MemberGuildSettings>(query,
                extraSql: "on conflict (guild, chao) do update set guild = $1, chao = $2 returning *"
            );
        }

        public Task UpdateMemberGuild(ChaoId chao, ulong guild, MemberGuildPatch patch)
        {
            _logger.Information("Updated {ChaoId} in guild {GuildId}: {@MemberGuildPatch}", chao, guild, patch);
            var query = patch.Apply(new Query("chao_guild").Where("chao", chao).Where("guild", guild));
            return _db.ExecuteQuery(query, extraSql: "returning *");
        }
    }
}