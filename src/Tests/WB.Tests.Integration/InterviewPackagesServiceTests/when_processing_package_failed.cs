using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NHibernate;
using NUnit.Framework;
using ReflectionMagic;
using WB.Core.BoundedContexts.Headquarters;
using WB.Core.BoundedContexts.Headquarters.Implementation;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Core.BoundedContexts.Headquarters.Mappings;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.BoundedContexts.Headquarters.Views;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.Domain;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Infrastructure.Native.Storage;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using WB.Tests.Abc;
using WB.Tests.Abc.Storage;
using WB.Tests.Integration.PostgreSQLTests;

namespace WB.Tests.Integration.InterviewPackagesServiceTests
{
    internal class when_processing_package_failed : with_postgres_db
    {
        [OneTimeSetUp]
        public void context()
        {
            sessionFactory = IntegrationCreate.SessionFactory(ConnectionStringBuilder.ConnectionString,
                new[] { typeof(InterviewPackageMap), typeof(BrokenInterviewPackageMap) }, true);

            using var UnitOfWork = IntegrationCreate.UnitOfWork(sessionFactory);

            var origin = "hq";
            expectedException = new InterviewException("Some interview exception",
                InterviewDomainExceptionType.StatusIsNotOneOfExpected);

            var packagesStorage = new PostgresPlainStorageRepository<InterviewPackage>(UnitOfWork);
            var brokenPackagesStorage = new PostgresPlainStorageRepository<BrokenInterviewPackage>(UnitOfWork);

            var mockOfCommandService = new Mock<ICommandService>();
            mockOfCommandService.Setup(x => x.Execute(Moq.It.IsAny<SynchronizeInterviewEventsCommand>(), Moq.It.IsAny<string>()))
                .Throws(expectedException);

            var newtonJsonSerializer = new JsonAllTypesSerializer();

            var serviceLocatorNestedMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            serviceLocatorNestedMock.Setup(x => x.GetInstance<ICommandService>()).Returns(mockOfCommandService.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IJsonAllTypesSerializer>())
                .Returns(newtonJsonSerializer);

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IInterviewUniqueKeyGenerator>())
                .Returns(Mock.Of<IInterviewUniqueKeyGenerator>);

            var receivedPackageLogEntry = new Mock<IPlainStorageAccessor<ReceivedPackageLogEntry>>();
            receivedPackageLogEntry.Setup(x => x
                .Query(It.IsAny<Func<IQueryable<ReceivedPackageLogEntry>, List<ReceivedPackageLogEntry>>>()))
                .Returns(new List<ReceivedPackageLogEntry>());

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IPlainStorageAccessor<ReceivedPackageLogEntry>>())
                .Returns(receivedPackageLogEntry.Object);

            Guid interviewerId = Id.g10;
            Guid supervisorId = Id.g9;

            var users = new Mock<IUserRepository>();
            var hqUser = new HqUser() { WorkspaceProfile = new WorkspaceUserProfile() };
            hqUser.WorkspaceProfile.AsDynamic().SupervisorId = supervisorId;
            
            users.Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(hqUser));

            serviceLocatorNestedMock.Setup(x => x.GetInstance<IUserRepository>()).Returns(users.Object);
            serviceLocatorNestedMock.Setup(x => x.GetInstance<IUnitOfWork>()).Returns(UnitOfWork);
            var executor = new NoScopeInScopeExecutor(serviceLocatorNestedMock.Object);

            var interviewPackagesService = Create.Service.InterviewPackagesService(
                syncSettings: new SyncSettings(origin),
                logger: Mock.Of<ILogger>(),
                serializer: newtonJsonSerializer,
                interviewPackageStorage: packagesStorage,
                brokenInterviewPackageStorage: brokenPackagesStorage,
                commandService: mockOfCommandService.Object,
                uniqueKeyGenerator: Mock.Of<IInterviewUniqueKeyGenerator>(),
                interviews: new TestInMemoryWriter<InterviewSummary>(),
                sessionFactory: sessionFactory,
                inScopeExecutor: executor);


            expectedCommand = Create.Command.SynchronizeInterviewEventsCommand(
                interviewId: Guid.Parse("11111111111111111111111111111111"),
                questionnaireId: Guid.Parse("22222222222222222222222222222222"),
                questionnaireVersion: 111,
                userId: Guid.Parse("33333333333333333333333333333333"),
                interviewStatus: InterviewStatus.Completed,
                createdOnClient: true,
                synchronizedEvents:
                new IEvent[]
                {
                    Create.Event.InterviewOnClientCreated(Guid.NewGuid(), 111),
                    new InterviewerAssigned(Guid.NewGuid(), interviewerId, DateTimeOffset.Now),
                    new SupervisorAssigned(Guid.NewGuid(), supervisorId, DateTimeOffset.Now),
                    new DateTimeQuestionAnswered(Guid.NewGuid(), Guid.NewGuid(), new decimal[] {2, 5, 8},
                        DateTime.UtcNow, DateTime.Today),
                });

            expectedEventsString = newtonJsonSerializer.Serialize(expectedCommand.SynchronizedEvents.ToArray());

            interviewPackagesService.ProcessPackage(new InterviewPackage
            {
                InterviewId = expectedCommand.InterviewId,
                QuestionnaireId = expectedCommand.QuestionnaireId,
                QuestionnaireVersion = expectedCommand.QuestionnaireVersion,
                ResponsibleId = expectedCommand.UserId,
                InterviewStatus = expectedCommand.InterviewStatus,
                IsCensusInterview = expectedCommand.CreatedOnClient,
                Events = expectedEventsString
            });
        }

        [NUnit.Framework.Test]
        public void should_broken_packages_storage_contains_specified_interview()
        {
            using var UoW = IntegrationCreate.UnitOfWork(sessionFactory);
            var brokenPackagesStorageVerifier = new PostgresPlainStorageRepository<BrokenInterviewPackage>(UoW);

            var expectedPackage = brokenPackagesStorageVerifier.GetById(1);

            expectedPackage.IsCensusInterview.Should().Be(expectedCommand.CreatedOnClient);
            expectedPackage.InterviewStatus.Should().Be(expectedCommand.InterviewStatus);
            expectedPackage.ResponsibleId.Should().Be(expectedCommand.UserId);
            expectedPackage.InterviewId.Should().Be(expectedCommand.InterviewId);
            expectedPackage.QuestionnaireId.Should().Be(expectedCommand.QuestionnaireId);
            expectedPackage.QuestionnaireVersion.Should().Be(expectedCommand.QuestionnaireVersion);
            // expectedPackage.ExceptionType.Should().Be(expectedException.ExceptionType.ToString());
            // expectedPackage.ExceptionMessage.Should().Be(expectedException.Message);
            expectedPackage.Events.Should().Be(expectedEventsString);
            expectedPackage.PackageSize.Should().Be(expectedEventsString.Length);
        }

        private ISessionFactory sessionFactory;
        private SynchronizeInterviewEventsCommand expectedCommand;
        private InterviewException expectedException;
        private string expectedEventsString;
    }
}
