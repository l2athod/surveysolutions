using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.View;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.Infrastructure.ReadSide;
using WB.UI.Designer.Api;
using WB.UI.Designer.Code;
using WB.UI.Designer.Models;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Applications.Designer.QuestionnaireApiControllerTests
{
    internal class when_verifying_questionnaire_with_21_verification_errors : QuestionnaireApiControllerTestContext
    {
        Establish context = () =>
        {
            questionnaireDocument = CreateQuestionnaireDocument(new[]
            {
                new Group()
                {
                    PublicKey = new Guid(),
                    Children =
                        new IComposite[101].Select(_ => new TextQuestion() {PublicKey = new Guid()}).ToList<IComposite>()
                }
            });
            var questionnaireView = CreateQuestionnaireView(questionnaireDocument);

            verificationErrors = CreateQuestionnaireVerificationErrors(questionnaireDocument.Find<IComposite>(_ => true));

            var questionnaireViewFactory = Mock.Of<IViewFactory<QuestionnaireViewInputModel, QuestionnaireView>>(x => x.Load(Moq.It.IsAny<QuestionnaireViewInputModel>()) == questionnaireView);
            verifierMock = new Mock<IQuestionnaireVerifier>();

            verifierMock
                .Setup(x => x.Verify(questionnaireDocument))
                .Returns(verificationErrors);

            controller = CreateQuestionnaireController(
                questionnaireViewFactory: questionnaireViewFactory, 
                questionnaireVerifier: verifierMock.Object,
                verificationErrorsMapper: new VerificationErrorsMapper());
        };

        Because of = () =>
            result = controller.Verify(questionnaireId);

        It should_returned_errors_contains_specified_errors_count = () =>
            result.Errors.Sum(error => error.References.Count).ShouldEqual(QuestionnaireController.MaxVerificationErrors);

        It should_return_original_errors_count_before_grouping_in_result = () =>
           result.ErrorsCount.ShouldEqual(QuestionnaireController.MaxVerificationErrors);

        private static QuestionnaireDocument questionnaireDocument; 
        private static Mock<IQuestionnaireVerifier> verifierMock ;
        private static QuestionnaireVerificationError[] verificationErrors;
        private static QuestionnaireController controller;
        private static VerificationErrors result;
        private static Guid questionnaireId = Guid.Parse("22222222222222222222222222222222");
    }
}