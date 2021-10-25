using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

using ChaoWorld.Core;

namespace ChaoWorld.API
{
    public class SystemPrivacyHandler: AuthorizationHandler<PrivacyRequirement<Garden>, Garden>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       PrivacyRequirement<Garden> requirement, Garden resource)
        {
            var level = requirement.Mapper(resource);
            var ctx = context.User.ContextFor(resource);
            if (level.CanAccess(ctx))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}