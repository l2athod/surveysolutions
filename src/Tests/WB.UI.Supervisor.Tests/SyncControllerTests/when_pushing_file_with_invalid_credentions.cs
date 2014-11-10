﻿using System;
using System.Net;
using System.Net.Http;
using Machine.Specifications;
using Main.Core.View;
using Main.Core.View.User;
using Moq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using It = Machine.Specifications.It;

namespace WB.UI.Supervisor.Tests.SyncControllerTests
{
    [Ignore("no scence")]
    internal class when_pushing_file_with_invalid_credentions : SyncControllerTestContext
    {
        Establish context = () =>
        {
            var user = new UserView();
            var userFactory = Mock.Of<IViewFactory<UserViewInputModel, UserView>>(x => x.Load(Moq.It.IsAny<UserViewInputModel>()) == user);
            
            controller = CreateSyncController();
        };

        Because of = () =>
            result = controller.PostFile(Guid.NewGuid()).Result;

        It should_have_NotAcceptable_status_code = () =>
            result.StatusCode.ShouldEqual(HttpStatusCode.NotAcceptable);

        private static HttpResponseMessage result;
        private static InterviewerSyncController controller;
    }
}
