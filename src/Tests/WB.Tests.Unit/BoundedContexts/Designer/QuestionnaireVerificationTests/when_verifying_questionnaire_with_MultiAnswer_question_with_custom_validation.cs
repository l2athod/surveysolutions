using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities.Question;
using Moq;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.SharedKernels.ExpressionProcessor.Services;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_TextList_question_with_custom_validation : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            multiAnswerQuestionWithValidationId = Guid.Parse("10000000000000000000000000000000");
            textQuestionId = Guid.Parse("20000000000000000000000000000000");
            questionnaire = CreateQuestionnaireDocument();

            questionnaire.Children.Add(new TextListQuestion()
            {
                PublicKey = multiAnswerQuestionWithValidationId,
                StataExportCaption = "var1",
                ValidationExpression = "some expression",
                ValidationMessage = "some message"
            });

            questionnaire.Children.Add(new TextListQuestion()
            {
                StataExportCaption = "var2",
                PublicKey = textQuestionId
            });

            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code__WB0041 = () =>
            resultErrors.Single().Code.ShouldEqual("WB0041");

        It should_return_error_with_1_references = () =>
            resultErrors.Single().References.Count().ShouldEqual(1);

        It should_return_error_reference_with_type_Question = () =>
            resultErrors.Single().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_error_reference_with_id_of_multiAnswerQuestionWithValidationId = () =>
            resultErrors.Single().References.First().Id.ShouldEqual(multiAnswerQuestionWithValidationId);

        private static IEnumerable<QuestionnaireVerificationError> resultErrors;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static Guid multiAnswerQuestionWithValidationId;
        private static Guid textQuestionId;
    }
}