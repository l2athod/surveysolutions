using System;
using System.Threading;

namespace WB.Core.BoundedContexts.Interviewer.Services
{
    public interface ISyncBgService
    {
        SyncProgressDto StartSync();

        SyncProgressDto CurrentProgress { get; }
    }
}