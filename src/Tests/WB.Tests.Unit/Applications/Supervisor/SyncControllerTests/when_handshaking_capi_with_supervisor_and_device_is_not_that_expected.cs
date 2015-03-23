using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;

using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using WB.Core.SharedKernels.SurveyManagement.Web.Models.User;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Applications.Supervisor.SyncControllerTests
{
    internal class when_handshaking_capi_with_supervisor_and_device_is_not_that_expected : SyncControllerTestContext
    {
        Establish context = () =>
        {
            var userLight = new UserLight() { Name = "test" };
            var globalInfo = Mock.Of<IGlobalInfoProvider>(x => x.GetCurrentUser() == userLight);

            var user = new UserWebView
                       {
                           DeviceId = "Expected device id"
                       };
            var userFactory = Mock.Of<IUserWebViewFactory>(x => x.Load(Moq.It.IsAny<UserWebViewInputModel>()) == user);
            var syncVersionProvider = Mock.Of<ISyncProtocolVersionProvider>(x => x.GetProtocolVersion() == supervisorVersion);

            controller = CreateSyncController(viewFactory: userFactory, syncVersionProvider: syncVersionProvider, globalInfo: globalInfo);
        };

        Because of = () =>
           exception = Catch.Exception(()=> controller.GetHandshakePackage(new HandshakePackageRequest
                                               {
                                                   AndroidId = androidId,
                                                   ClientId = Guid.NewGuid(),
                                                   ClientRegistrationId = Guid.NewGuid(),
                                                   Version = capiVersion,
                                                   ShouldDeviceBeLinkedToUser = false
                                               }));

        It should_return_http_response_exception = () =>
            exception.ShouldBeOfExactType<HttpResponseException>();

        It should_return_exception_with_NotAcceptable_status_code = () =>
            ((HttpResponseException)exception).Response.StatusCode.ShouldEqual(HttpStatusCode.NotAcceptable);

        private static InterviewerSyncController controller;
        private static int capiVersion = 13;
        private static int supervisorVersion = 13;
        private static string androidId = "Android";
        private static Exception exception;
    }
}