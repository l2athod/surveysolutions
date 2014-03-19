﻿using Moq;
using WB.Core.BoundedContexts.Supervisor.Implementation.Services;

namespace WB.Core.BoundedContexts.Supervisor.Tests.SupportedVersionProviderTests
{
    internal class SupportedVersionProviderTestContext
    {
        protected static SupportedVersionProvider CreateSupportedVersionProvider(ApplicationVersionSettings settings)
        {
            return new SupportedVersionProvider(settings);
        }
    }
}
