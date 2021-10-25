using System;
using System.Threading.Tasks;

using Dapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

using ChaoWorld.Core;

namespace ChaoWorld.API
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/m")]
    public class MemberController: ControllerBase
    {
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly IAuthorizationService _auth;

        public MemberController(IAuthorizationService auth, IDatabase db, ModelRepository repo)
        {
            _auth = auth;
            _db = db;
            _repo = repo;
        }

        [HttpGet("{hid}")]
        public async Task<ActionResult<JObject>> GetMember(string hid)
        {
            var member = await _repo.GetMemberByHid(hid);
            if (member == null) return NotFound("Member not found.");

            return Ok(member.ToJson(User.ContextFor(member), needsLegacyProxyTags: true));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<JObject>> PostMember([FromBody] JObject properties)
        {
            if (!properties.ContainsKey("name"))
                return BadRequest("Member name must be specified.");

            var systemId = User.CurrentSystem();
            var systemData = await _repo.GetSystem(systemId);

            await using var conn = await _db.Obtain();

            // Enforce per-system member limit
            var memberCount = await conn.QuerySingleAsync<int>("select count(*) from members where system = @System", new { System = systemId });
            var memberLimit = systemData?.MemberLimitOverride ?? Limits.MaxMemberCount;
            if (memberCount >= memberLimit)
                return BadRequest($"Member limit reached ({memberCount} / {memberLimit}).");

            await using var tx = await conn.BeginTransactionAsync();
            var member = await _repo.CreateMember(systemId, properties.Value<string>("name"), conn);

            MemberPatch patch;
            try
            {
                patch = MemberPatch.FromJSON(properties);
                patch.AssertIsValid();
            }
            catch (FieldTooLongError e)
            {
                await tx.RollbackAsync();
                return BadRequest(e.Message);
            }
            catch (ValidationError e)
            {
                await tx.RollbackAsync();
                return BadRequest($"Request field '{e.Message}' is invalid.");
            }

            member = await _repo.UpdateMember(member.Id, patch, conn);
            await tx.CommitAsync();
            return Ok(member.ToJson(User.ContextFor(member), needsLegacyProxyTags: true));
        }

        [HttpPatch("{hid}")]
        [Authorize]
        public async Task<ActionResult<JObject>> PatchMember(string hid, [FromBody] JObject changes)
        {
            var member = await _repo.GetMemberByHid(hid);
            if (member == null) return NotFound("Member not found.");

            var res = await _auth.AuthorizeAsync(User, member, "EditMember");
            if (!res.Succeeded) return Unauthorized($"Member '{hid}' is not part of your system.");

            MemberPatch patch;
            try
            {
                patch = MemberPatch.FromJSON(changes);
                patch.AssertIsValid();
            }
            catch (FieldTooLongError e)
            {
                return BadRequest(e.Message);
            }
            catch (ValidationError e)
            {
                return BadRequest($"Request field '{e.Message}' is invalid.");
            }

            var newMember = await _repo.UpdateMember(member.Id, patch);
            return Ok(newMember.ToJson(User.ContextFor(newMember), needsLegacyProxyTags: true));
        }

        [HttpDelete("{hid}")]
        [Authorize]
        public async Task<ActionResult> DeleteMember(string hid)
        {
            var member = await _repo.GetMemberByHid(hid);
            if (member == null) return NotFound("Member not found.");

            var res = await _auth.AuthorizeAsync(User, member, "EditMember");
            if (!res.Succeeded) return Unauthorized($"Member '{hid}' is not part of your system.");

            await _repo.DeleteMember(member.Id);
            return Ok();
        }
    }
}