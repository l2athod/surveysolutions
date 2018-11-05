using FluentAssertions;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Services.HealthCheck;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.HealthCheck;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;


namespace WB.Tests.Unit.Applications.Headquarters.PublicApiTests
{
    internal class when_healthcheck_controller_getstatus : ApiTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            /*KP-4929    var databaseHealthCheck = Mock.Of<IDatabaseHealthCheck>(m => m.Check() == ConnectionHealthCheckResult.Happy());
            var eventStoreHealthCheck = Mock.Of<IEventStoreHealthCheck>(m => m.Check() == ConnectionHealthCheckResult.Happy());
            var brokenSyncPackagesStorage = Mock.Of<IBrokenSyncPackagesStorage>(m => m.GetListOfUnhandledPackages() == Enumerable.Empty<string>());
         var chunkReader = Mock.Of<IChunkReader>(m => m.GetNumberOfSyncPackagesWithBigSize() == 0);
            var folderPermissionChecker = Mock.Of<IFolderPermissionChecker>(m => m.Check() == new FolderPermissionCheckResult(null, null, null));*/
            healthCheckService = new Mock<IHealthCheckService>();
            healthCheckService.Setup(x => x.Check())
                .Returns(new HealthCheckResults(HealthCheckStatus.Happy,
                    EventStoreHealthCheckResult.Happy(), NumberOfUnhandledPackagesHealthCheckResult.Happy(0),
                    new FolderPermissionCheckResult(HealthCheckStatus.Happy, "", new string[0], new string[0]), ReadSideHealthCheckResult.Happy()));
            controller = CreateHealthCheckApiController(healthCheckService.Object
                /*KP-4929        databaseHealthCheck,
                      eventStoreHealthCheck,
                      brokenSyncPackagesStorage,
                     chunkReader,
                folderPermissionChecker*/);
            BecauseOf();
        }

        public void BecauseOf() 
        {
            result = controller.GetStatus();
        }

        [NUnit.Framework.Test] public void should_return_HealthCheckStatus () =>
            result.Should().BeOfType<HealthCheckStatus>();

        [NUnit.Framework.Test] public void should_return_Happy_status () =>
            result.Should().Be(HealthCheckStatus.Happy);

        [NUnit.Framework.Test] public void should_call_IHealthCheckService_Check_once () =>
            healthCheckService.Verify(x => x.Check(), Times.Once());

        private static HealthCheckStatus result;
        private static HealthCheckApiController controller;

        private static Mock<IHealthCheckService> healthCheckService;

    }
}