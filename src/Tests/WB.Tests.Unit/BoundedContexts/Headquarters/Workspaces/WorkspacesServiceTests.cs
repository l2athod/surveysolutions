using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.BoundedContexts.Headquarters.Workspaces;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.Workspaces
{
    [TestFixture]
    public class WorkspacesServiceTests 
    {
        [Test]
        public void WorkspacesService_Should_return_only_enabled_services()
        {
            var enabledWorkspace = Create.Entity.Workspace();
            var disabledWorkspace = Create.Entity.Workspace();
            disabledWorkspace.Disable();
            
            var storage =  new TestPlainStorage<Workspace>();
            storage.Store(new []{enabledWorkspace, disabledWorkspace});

            // Act
            var service = Create.Service.WorkspacesService(storage);

            // Assert
            var list = service.GetEnabledWorkspaces().ToList();
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list.First().Name, Is.EqualTo(enabledWorkspace.Name));
        }

        [TestCase(UserRoles.Supervisor)]
        [TestCase(UserRoles.Interviewer)]
        public void should_not_allow_removing_user_with_pending_work_from_workspace(UserRoles role)
        {
            var interviewFactory = Mock.Of<IInterviewInformationFactory>(
                i => i.GetInProgressInterviewsForSupervisor(Id.gA) ==
                    new List<InterviewInformation>{ new InterviewInformation() } &&
                    i.GetInProgressInterviewsForInterviewer(Id.gA) == new List<InterviewInformation>{ new InterviewInformation() });

            var enabledWorkspace = Create.Entity.Workspace();
            var storage =  new TestPlainStorage<Workspace>();
            storage.Store(new []{enabledWorkspace});

            var assignmentsService =
                Mock.Of<IAssignmentsService>(a => a.GetAllAssignmentIds(Id.gA) == new List<Guid>());
            var serviceLocator = Mock.Of<IServiceLocator>(sl => sl.GetInstance<IInterviewInformationFactory>() == interviewFactory &&
                                                                sl.GetInstance<IAssignmentsService>() == assignmentsService);
            var service = Create.Service.WorkspacesService(storage,
                serviceLocator);

            // Act
            var hqUser = Create.Entity.HqUser(role: role, userId: Id.gA);
            hqUser.Workspaces.Add(new WorkspacesUsers(enabledWorkspace, hqUser, null));
            TestDelegate act = () => service.AssignWorkspaces(hqUser, new List<AssignUserWorkspace>());

            // Assert
            Assert.Throws<WorkspaceRemovalNotAllowedException>(act);
        }
    }
}
