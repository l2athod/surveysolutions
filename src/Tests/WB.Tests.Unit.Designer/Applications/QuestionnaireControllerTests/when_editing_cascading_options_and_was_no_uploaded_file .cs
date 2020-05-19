using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WB.UI.Designer.Controllers;
using WB.UI.Shared.Web.Extensions;


namespace WB.Tests.Unit.Designer.Applications.QuestionnaireControllerTests
{
    internal class when_editing_cascading_options_and_was_no_uploaded_file : QuestionnaireControllerTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            controller = CreateQuestionnaireController();
            controller.questionWithOptionsViewModel = new QuestionnaireController.EditOptionsViewModel
            {
                IsCascading = true
            };
            BecauseOf();
        }

        private void BecauseOf() => result = (JsonResult)controller.EditOptions(null);

        [NUnit.Framework.Test] public void should_add_error_message_to_temp_data () =>
            ((List<string>)result.Value)[0].Should().Be("Choose tab-separated values file to upload, please");

        private static QuestionnaireController controller;
        private static JsonResult result;
    }
}
