using System;
using System.Threading;
using System.Threading.Tasks;
using WB.Core.SharedKernels.Enumerator.Services.Synchronization;

namespace WB.Core.SharedKernels.Enumerator.Services.MapSynchronization
{
    public interface IMapSyncProvider
    {
        Task SynchronizeAsync(IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken);
    }
}
