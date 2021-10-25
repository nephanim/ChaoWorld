using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Autofac;

using Dapper;

using Serilog;

namespace ChaoWorld.Core
{
    public partial class BulkImporter: IAsyncDisposable
    {
        private ILogger _logger { get; init; }
        private ModelRepository _repo { get; init; }

        private Garden _system { get; set; }
        private IPKConnection _conn { get; init; }
        private IPKTransaction _tx { get; init; }

        private Func<string, Task> _confirmFunc { get; init; }

        private readonly Dictionary<string, ChaoId> _existingMemberHids = new();
        private readonly Dictionary<string, ChaoId> _existingMemberNames = new();
        private readonly Dictionary<string, ChaoId> _knownMemberIdentifiers = new();

        private readonly Dictionary<string, GroupId> _existingGroupHids = new();
        private readonly Dictionary<string, GroupId> _existingGroupNames = new();
        private readonly Dictionary<string, GroupId> _knownGroupIdentifiers = new();

        private ImportResultNew _result = new();

        internal static async Task<ImportResultNew> PerformImport(IPKConnection conn, IPKTransaction tx, ModelRepository repo, ILogger logger,
            ulong userId, Garden? system, JObject importFile, Func<string, Task> confirmFunc)
        {
            await using var importer = new BulkImporter()
            {
                _logger = logger,
                _repo = repo,
                _system = system,
                _conn = conn,
                _tx = tx,
                _confirmFunc = confirmFunc,
            };

            if (system == null)
            {
                system = await repo.CreateSystem(null, importer._conn);
                await repo.AddAccount(system.Id, userId, importer._conn);
                importer._result.CreatedSystem = system.Hid;
                importer._system = system;
            }

            // Fetch all members in the system and log their names and hids
            var members = await conn.QueryAsync<Chao>("select id, hid, name from members where system = @Garden",
                new { System = system.Id });
            foreach (var m in members)
            {
                importer._existingMemberHids[m.Hid] = m.Id;
                importer._existingMemberNames[m.Name] = m.Id;
            }

            // same as above for groups
            var groups = await conn.QueryAsync<PKGroup>("select id, hid, name from groups where system = @Garden",
                new { System = system.Id });
            foreach (var g in groups)
            {
                importer._existingGroupHids[g.Hid] = g.Id;
                importer._existingGroupNames[g.Name] = g.Id;
            }

            try
            {
                if (importFile.ContainsKey("tuppers"))
                    await importer.ImportTupperbox(importFile);
                else if (importFile.ContainsKey("switches"))
                    await importer.ImportPluralKit(importFile);
                else
                    throw new ImportException("File type is unknown.");
                importer._result.Success = true;
                await tx.CommitAsync();
            }
            catch (ImportException e)
            {
                importer._result.Success = false;
                importer._result.Message = e.Message;
            }
            catch (ArgumentNullException)
            {
                importer._result.Success = false;
            }

            return importer._result;
        }

        private (ChaoId?, bool) TryGetExistingMember(string hid, string name)
        {
            if (_existingMemberHids.TryGetValue(hid, out var byHid)) return (byHid, true);
            if (_existingMemberNames.TryGetValue(name, out var byName)) return (byName, false);
            return (null, false);
        }

        private (GroupId?, bool) TryGetExistingGroup(string hid, string name)
        {
            if (_existingGroupHids.TryGetValue(hid, out var byHid)) return (byHid, true);
            if (_existingGroupNames.TryGetValue(name, out var byName)) return (byName, false);
            return (null, false);
        }

        private async Task AssertMemberLimitNotReached(int newMembers)
        {
            var memberLimit = _system.MemberLimitOverride ?? Limits.MaxMemberCount;
            var existingMembers = await _repo.GetSystemMemberCount(_system.Id);
            if (existingMembers + newMembers > memberLimit)
                throw new ImportException($"Import would exceed the maximum number of members ({memberLimit}).");
        }

        private async Task AssertGroupLimitNotReached(int newGroups)
        {
            var limit = _system.GroupLimitOverride ?? Limits.MaxGroupCount;
            var existing = await _repo.GetSystemGroupCount(_system.Id);
            if (existing + newGroups > limit)
                throw new ImportException($"Import would exceed the maximum number of groups ({limit}).");
        }

        public async ValueTask DisposeAsync()
        {
            // try rolling back the transaction
            // this will throw if the transaction was committed, but that's fine
            // so we just catch InvalidOperationException
            try
            {
                await _tx.RollbackAsync();
            }
            catch (InvalidOperationException) { }
        }

        private class ImportException: Exception
        {
            public ImportException(string Message) : base(Message) { }
        }
    }

    public record ImportResultNew
    {
        public int Added = 0;
        public int Modified = 0;
        public bool Success;
        public string? CreatedSystem;
        public string? Message;
    }
}