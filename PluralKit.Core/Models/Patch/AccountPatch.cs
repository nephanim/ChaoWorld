using SqlKata;

namespace ChaoWorld.Core
{
    public class AccountPatch: PatchObject
    {
        public Partial<bool> AllowAutoproxy { get; set; }

        public override Query Apply(Query q) => q.ApplyPatch(wrapper => wrapper
            .With("allow_autoproxy", AllowAutoproxy)
        );
    }
}