﻿using System.Linq;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Domain;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.Workspace;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.SharedKernels.Enumerator.Implementation.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IPlainStorage<WorkspaceView> workspaceRepository;
        private readonly SqliteSettings settings;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IInScopeExecutor executeInWorkspaceService;
        private readonly ILogger logger;

        public WorkspaceService(IPlainStorage<WorkspaceView> workspaceRepository,
            SqliteSettings settings,
            IFileSystemAccessor fileSystemAccessor,
            IInScopeExecutor executeInWorkspaceService,
            ILogger logger
            )
        {
            this.workspaceRepository = workspaceRepository;
            this.settings = settings;
            this.fileSystemAccessor = fileSystemAccessor;
            this.executeInWorkspaceService = executeInWorkspaceService;
            this.logger = logger;
        }

        public void Save(WorkspaceView[] workspaces)
        {
            var currentWorkspaces = workspaceRepository.LoadAll();
            var removedWorkspaces = currentWorkspaces.Where(w => workspaces.All(nw => nw.Name != w.Name)).ToList();

            if (removedWorkspaces.Count > 0)
            {
                logger.Warn($"WorkspaceService: need to remove {removedWorkspaces.Count} workspaces");
                
                foreach (var removedWorkspace in removedWorkspaces)
                {
                    logger.Warn($"WorkspaceService: removing {removedWorkspace.Name} workspace");

                    var workspaceDirectory = fileSystemAccessor.CombinePath(settings.PathToRootDirectory, removedWorkspace.Name);
                    if (fileSystemAccessor.IsDirectoryExists(workspaceDirectory))
                    {
                        executeInWorkspaceService.Execute(serviceProvider =>
                        {
                            var interviewViewRepository = serviceProvider.GetInstance<IPlainStorage<InterviewView>>();
                            var interviewsCount = interviewViewRepository.Count();
                            if (interviewsCount > 0)
                            {
                                var assignmentsStorage = serviceProvider.GetInstance<IAssignmentDocumentsStorage>();
                                assignmentsStorage.RemoveAll();  
                                logger.Warn($"WorkspaceService: removed assignments in {removedWorkspace.Name} workspace, because exists {interviewsCount} interviews");
                            }
                            else
                            {
                                fileSystemAccessor.DeleteDirectory(workspaceDirectory);
                                workspaceRepository.Remove(removedWorkspaces);
                                logger.Warn($"WorkspaceService: removed data of {removedWorkspace.Name} workspace");
                            }
                        }, removedWorkspace.Name);
                    }
                }
            }

            workspaceRepository.Store(workspaces);
        }

        public WorkspaceView[] GetAll()
        {
            return workspaceRepository
                .Where(s => s.Disabled == false)
                .ToArray();
        }

        public WorkspaceView GetByName(string workspace)
        {
            return workspaceRepository.GetById(workspace);
        }
    }
}
