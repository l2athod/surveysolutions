using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts;
using WB.UI.Headquarters.Controllers;


namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PeriodicStatusReportTests
{
    internal class when_request_quantity_report_by_interviewers_for_user_in_hq_role
    {
        [NUnit.Framework.OneTimeSetUp]
        public void context()
        {
            var authorizedUser = Mock.Of<IAuthorizedUser>(x => x.IsHeadquarter == true);
            reportController = Web.Create.Controller.ReportsController(authorizedUser: authorizedUser);
            BecauseOf();
        }

        public void BecauseOf() =>
            result = reportController.QuantityByInterviewers(null) as ViewResult;

        [NUnit.Framework.Test]
        public void should_responsible_name_be_not_a_link() =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityByTeamMember.Should().Be(false);

        [NUnit.Framework.Test]
        public void should_go_back_to_supervisor_button_be_visible() =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityBySupervisors.Should().Be(true);

        [NUnit.Framework.Test]
        public void should_ReportName_be_Quantity() =>
           ((PeriodicStatusReportModel)result.Model).ReportName.Should().Be("Quantity");

        private static ReportsController reportController;
        private static ViewResult result;
    }
}
