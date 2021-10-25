using System;

using Microsoft.AspNetCore.Authorization;

using ChaoWorld.Core;

namespace ChaoWorld.API
{
    public class PrivacyRequirement<T>: IAuthorizationRequirement
    {
        public readonly Func<T, PrivacyLevel> Mapper;

        public PrivacyRequirement(Func<T, PrivacyLevel> mapper)
        {
            Mapper = mapper;
        }
    }
}