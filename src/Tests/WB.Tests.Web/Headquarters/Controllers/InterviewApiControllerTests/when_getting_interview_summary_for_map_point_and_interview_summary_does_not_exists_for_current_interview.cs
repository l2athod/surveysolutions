using System;
using FluentAssertions;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Controllers.Api;

namespace WB.Tests.Web.Headquarters.Controllers.InterviewApiControllerTests
{
    internal class
        when_getting_interview_summary_for_map_point_and_interview_summary_does_not_exists_for_current_interview :
            InterviewApiControllerTestsContext
    {
        [NUnit.Framework.OneTimeSetUp]
        public void context()
        {
            var interviewSummaryViewFactoryMock = new Mock<IAllInterviewsFactory>();
            interviewSummaryViewFactoryMock.Setup(_ => _.Load(interviewId)).Returns(() => null);

            controller = CreateController(allInterviewsViewFactory: interviewSummaryViewFactoryMock.Object);
            BecauseOf();
        }

        public void BecauseOf() =>
            viewModel =
                controller.InterviewSummaryForMapPoint(new InterviewSummaryForMapPointViewModel()
                    {InterviewId = interviewId});

        [NUnit.Framework.Test]
        public void should_view_model_be_null() =>
            viewModel.Should().BeNull();

        private static InterviewApiController controller;
        private static InterviewSummaryForMapPointView viewModel;
        private static Guid interviewId = Guid.Parse("11111111111111111111111111111111");
    }
}
