using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using ChaoWorld.Core;

namespace ChaoWorld.API
{
    public class SystemOwnerHandler: AuthorizationHandler<OwnSystemRequirement, Garden>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       OwnSystemRequirement requirement, Garden resource)
        {
            if (!context.User.Identity.IsAuthenticated) return Task.CompletedTask;
            if (resource.Id == context.User.CurrentSystem())
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}