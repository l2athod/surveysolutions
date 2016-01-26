using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_linked_question_reference_on_question_of_not_supported_type : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            linkedQuestionId = Guid.Parse("10000000000000000000000000000000");
            notSupportedForLinkingQuestionId = Guid.Parse("13333333333333333333333333333333");
            questionnaire = CreateQuestionnaireDocument();

            questionnaire.Children.Add(new SingleQuestion()
            {
                PublicKey = notSupportedForLinkingQuestionId,
                StataExportCaption = "var1",
                QuestionType = QuestionType.SingleOption,
                Answers = { new Answer() { AnswerValue = "1", AnswerText = "opt 1" }, new Answer() { AnswerValue = "2", AnswerText = "opt 2" } }
            });

            questionnaire.Children.Add(new SingleQuestion()
            {
                PublicKey = linkedQuestionId,
                LinkedToQuestionId = notSupportedForLinkingQuestionId,
                StataExportCaption = "var2",
                Answers = { new Answer() { AnswerValue = "1", AnswerText = "opt 1" }, new Answer() { AnswerValue = "2", AnswerText = "opt 2" } }
            });
            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code__WB0012__ = () =>
            resultErrors.Single().Code.ShouldEqual("WB0012");

        It should_return_error_with_level_general = () =>
            resultErrors.Single().MessageLevel.ShouldEqual(VerificationMessageLevel.General);

        It should_return_error_with_two_references = () =>
            resultErrors.Single().References.Count().ShouldEqual(2);

        It should_return_first_error_reference_with_type_Question = () =>
            resultErrors.Single().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_first_error_reference_with_id_of_linkedQuestionId = () =>
            resultErrors.Single().References.First().Id.ShouldEqual(linkedQuestionId);

        It should_return_last_error_reference_with_type_Question = () =>
           resultErrors.Single().References.Last().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_last_error_reference_with_id_of_notSupportedForLinkingQuestionId = () =>
            resultErrors.Single().References.Last().Id.ShouldEqual(notSupportedForLinkingQuestionId);

        private static IEnumerable<QuestionnaireVerificationMessage> resultErrors;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static Guid linkedQuestionId;
        private static Guid notSupportedForLinkingQuestionId;
    }
}
