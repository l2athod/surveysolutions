﻿using System;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.EventBus.Lite;

namespace WB.Core.SharedKernels.DataCollection.Events.User
{
    [Obsolete]
    public class UserLinkedToDevice : IEvent
    {
        public string DeviceId { get; set; }
    }
}