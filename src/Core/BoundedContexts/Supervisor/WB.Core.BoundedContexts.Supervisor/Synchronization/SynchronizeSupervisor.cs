﻿using System.Threading.Tasks;
using WB.Core.BoundedContexts.Supervisor.Services;
using WB.Core.BoundedContexts.Supervisor.Views;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.Enumerator.Implementation.Services.Synchronization.Steps;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.Synchronization;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.BoundedContexts.Supervisor.Synchronization
{
    public class SynchronizeSupervisor : SynchronizationStep
    {
        private readonly ISupervisorSynchronizationService supevisorSynchronizationService;
        private readonly ISupervisorPrincipal principal;
        private readonly IPlainStorage<SupervisorIdentity> supervisorsPlainStorage;

        public SynchronizeSupervisor(int sortOrder, 
            ISupervisorSynchronizationService synchronizationService, 
            ILogger logger, 
            ISupervisorPrincipal principal, 
            IPlainStorage<SupervisorIdentity> supervisorsPlainStorage) : 
                base(sortOrder, synchronizationService, logger)
        {
            this.supevisorSynchronizationService = synchronizationService;
            this.principal = principal;
            this.supervisorsPlainStorage = supervisorsPlainStorage;
        }

        public override async Task ExecuteAsync()
        {
            Context.Progress.Report(new SyncProgressInfo
            {
                Title = EnumeratorUIResources.Synchronization_Of_Supervisor_details,
                Statistics = Context.Statistics,
                Status = SynchronizationStatus.Download
            });

            var localSupervisor = this.supervisorsPlainStorage.FirstOrDefault();
            var supervisor = await supevisorSynchronizationService.GetSupervisorAsync(token: Context.CancellationToken);

            if (localSupervisor.Email != supervisor.Email)
            {
                localSupervisor.Email = supervisor.Email;
                this.supervisorsPlainStorage.Store(localSupervisor);

                principal.SignInWithHash(localSupervisor.Name, localSupervisor.PasswordHash, true);
            }
        }
    }
}
