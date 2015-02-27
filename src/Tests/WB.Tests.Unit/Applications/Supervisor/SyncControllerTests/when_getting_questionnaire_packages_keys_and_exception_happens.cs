using System;
using System.Net;
using System.Net.Http;

using Machine.Specifications;

using Main.Core.Entities.SubEntities;

using Moq;

using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using WB.Core.SharedKernels.SurveyManagement.Web.Resources;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;
using WB.Core.Synchronization;
using WB.Core.Synchronization.SyncStorage;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Applications.Supervisor.SyncControllerTests
{
    internal class when_getting_questionnaire_packages_keys_and_exception_happens : SyncControllerTestContext
    {
        Establish context = () =>
        {
            var userLight = new UserLight { Id = userId, Name = "test" };
            var globalInfo = Mock.Of<IGlobalInfoProvider>(x => x.GetCurrentUser() == userLight);

            request = CreateSyncItemsMetaContainerRequest(lastSyncedPackageId, deviceId);

            var syncManagerMock = new Mock<ISyncManager>();
            syncManagerMock
                .Setup(x => x.GetQuestionnairePackageIdsWithOrder(userId, deviceId, lastSyncedPackageId))
                .Throws<SyncPackageNotFoundException>();
            
            var jsonUtilsMock = new Mock<IJsonUtils>();
            jsonUtilsMock
                .Setup(x => x.Serialize(Moq.It.IsAny<RestErrorDescription>()))
                .Returns(errorMessage)
                .Callback((RestErrorDescription error) => errorDescription = error);

            controller = CreateSyncController(syncManager: syncManagerMock.Object, jsonUtils:jsonUtilsMock.Object, globalInfo: globalInfo);
        };

        Because of = () =>
            result = controller.GetQuestionnairePackageIds(request);

        It should_return_response_with_status_NotFound = () =>
            result.StatusCode.ShouldEqual(HttpStatusCode.NotFound);

        It should_return_response_with_ = () =>
            result.Content.ReadAsStringAsync().Result.ShouldContain(errorMessage);

        It should_return_error_message_with_code_ServerError = () =>
            errorDescription.Message.ShouldEqual(InterviewerSyncStrings.ServerError);

        private static HttpResponseMessage result;

        private static InterviewerSyncController controller;
        private static string androidId = "Android";
        private static Guid userId = Guid.Parse("11111111111111111111111111111111");
        private static Guid deviceId = androidId.ToGuid();
        private static string lastSyncedPackageId = "some package";
        private static SyncItemsMetaContainerRequest request;
        private static string errorMessage = "error";
        private static RestErrorDescription errorDescription;
    }
}