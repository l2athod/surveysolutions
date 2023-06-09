using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Moq;
using NUnit.Framework;
using ReflectionMagic;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.Domain;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Infrastructure.Native.Storage;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.InterviewPackagesServiceTests
{
    class when_sync_package_with_new_interview
    {
        private static Mock<ICommandService> commandService;
        private static SynchronizeInterviewEventsCommand syncCommand = null;

        [SetUp]
        public void Setup()
        {
            commandService = new Mock<ICommandService>();
            commandService
                .Setup(x => x.Execute(It.IsAny<ICommand>(), It.IsAny<string>()))
                .Callback((ICommand c, string o) => { syncCommand = c as SynchronizeInterviewEventsCommand; });
        }

        [Test]
        public void When_interviewer_was_moved_to_another_team_after_interview_was_created()
        {
            
            var oldSupervisorId = Id.gE;
            var newSupervisorId = Id.gB;

            var newtonJsonSerializer = new JsonAllTypesSerializer();

            IComponentRegistration componentRegistration = new Mock<IComponentRegistration>().Object;
            var componentRegistry = new Mock<IComponentRegistry>();
            componentRegistry.Setup(x =>
                    x.TryGetRegistration(It.IsAny<Service>(), out componentRegistration))
                .Returns(true);

            var container = new Mock<ILifetimeScope>();
            container.Setup(x => x.BeginLifetimeScope(It.IsAny<string>())).Returns(container.Object);
            container.SetupGet(x => x.ComponentRegistry).Returns(componentRegistry.Object);

            var serviceLocatorNestedMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            serviceLocatorNestedMock.Setup(x => x.GetInstance<ICommandService>()).Returns(commandService.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IJsonAllTypesSerializer>())
                .Returns(newtonJsonSerializer);

            var packageStore = new Mock<IPlainStorageAccessor<ReceivedPackageLogEntry>>();
            packageStore.Setup(x => x.Query(It.IsAny<Func<IQueryable<ReceivedPackageLogEntry>, ReceivedPackageLogEntry>>())).Returns((ReceivedPackageLogEntry)null);

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IPlainStorageAccessor<ReceivedPackageLogEntry>>()).Returns(packageStore.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IInterviewUniqueKeyGenerator>())
                .Returns(Mock.Of<IInterviewUniqueKeyGenerator>(x => x.Get() == new InterviewKey(5533)));

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IQueryableReadSideRepositoryReader<InterviewSummary>>())
                .Returns(new TestInMemoryWriter<InterviewSummary>());

            var users = new Mock<IUserRepository>();
            var hqUser = new HqUser() {WorkspaceProfile = new WorkspaceUserProfile()};
            hqUser.WorkspaceProfile.AsDynamic().SupervisorId = newSupervisorId;

            users.Setup(x => x.FindById(It.IsAny<Guid>()))
                .Returns(hqUser);

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IUserRepository>()).Returns(users.Object);

            var executor = new NoScopeInScopeExecutor(serviceLocatorNestedMock.Object);
            var service = Create.Service.InterviewPackagesService(commandService: commandService.Object, inScopeExecutor: executor);

            // Act
            service.ProcessPackage(Create.Entity.InterviewPackage(Id.g1, Create.Event.SupervisorAssigned(Id.gC, oldSupervisorId)));
            

            // Assert
            Assert.That(syncCommand, Is.Not.Null);
            Assert.That(syncCommand.NewSupervisorId, Is.EqualTo(newSupervisorId));
        }

        [Test]
        public void When_interviewer_was_not_moved_to_another_team_after_interview_was_created()
        {
           
            var oldSupervisorId = Id.gB;

            var newtonJsonSerializer = new JsonAllTypesSerializer();

            IComponentRegistration componentRegistration = new Mock<IComponentRegistration>().Object;
            var componentRegistry = new Mock<IComponentRegistry>();
            componentRegistry.Setup(x =>
                    x.TryGetRegistration(It.IsAny<Service>(), out componentRegistration))
                .Returns(true);

            var container = new Mock<ILifetimeScope>();
            container.Setup(x => x.BeginLifetimeScope(It.IsAny<string>())).Returns(container.Object);
            container.SetupGet(x => x.ComponentRegistry).Returns(componentRegistry.Object);

            var serviceLocatorNestedMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            serviceLocatorNestedMock.Setup(x => x.GetInstance<ICommandService>()).Returns(commandService.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IJsonAllTypesSerializer>())
                .Returns(newtonJsonSerializer);

            var packageStore = new Mock<IPlainStorageAccessor<ReceivedPackageLogEntry>>();
            packageStore.Setup(x => x.Query(It.IsAny<Func<IQueryable<ReceivedPackageLogEntry>, ReceivedPackageLogEntry>>())).Returns((ReceivedPackageLogEntry)null);

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IPlainStorageAccessor<ReceivedPackageLogEntry>>()).Returns(packageStore.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IInterviewUniqueKeyGenerator>())
                .Returns(Mock.Of<IInterviewUniqueKeyGenerator>(x => x.Get() == new InterviewKey(5533)));

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IQueryableReadSideRepositoryReader<InterviewSummary>>())
                .Returns(new TestInMemoryWriter<InterviewSummary>());

            var users = new Mock<IUserRepository>();
            var hqUser = new HqUser() {WorkspaceProfile = new WorkspaceUserProfile()};
            hqUser.WorkspaceProfile.AsDynamic().SupervisorId = oldSupervisorId;

            users.Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(hqUser));

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IUserRepository>()).Returns(users.Object);

            var executor = new NoScopeInScopeExecutor(serviceLocatorNestedMock.Object);
            var service = Create.Service.InterviewPackagesService(commandService: commandService.Object, inScopeExecutor: executor);

            // Act
            service.ProcessPackage(Create.Entity.InterviewPackage(Id.g1,
                    Create.Event.SupervisorAssigned(Id.gC, oldSupervisorId)));

            
            // Assert
            Assert.That(syncCommand, Is.Not.Null);
            Assert.That(syncCommand.NewSupervisorId, Is.Null);
        }
    }
}
