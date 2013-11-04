using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.QuestionnaireVerification.Implementation.Services;
using WB.Core.SharedKernels.QuestionnaireVerification.ValueObjects;

namespace WB.Core.SharedKernels.QuestionnaireVerification.Tests.QuestionnaireVerifierTests
{
    internal class when_verifying_questionnaire_with_question_that_has_substitutions_references_with_deeper_propagation_level : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            questionWithSubstitutionsId = Guid.Parse("10000000000000000000000000000000");
            underDeeperPropagationLevelQuestionId = Guid.Parse("12222222222222222222222222222222");
            var autoPropagatedGroup = Guid.Parse("13333333333333333333333333333333");
            questionnaire = CreateQuestionnaireDocument();

            questionnaire.Children.Add(new AutoPropagateQuestion
            {
                PublicKey = Guid.NewGuid(),
                Triggers = new List<Guid> { autoPropagatedGroup }
            });

            var autopropagatedGroup = new Group() { PublicKey = autoPropagatedGroup, Propagated = Propagate.AutoPropagated };

            autopropagatedGroup.Children.Add(new NumericQuestion()
            {
                PublicKey = underDeeperPropagationLevelQuestionId,
                StataExportCaption = underDeeperPropagationLevelQuestionVariableName
            });
            questionnaire.Children.Add(autopropagatedGroup);
            questionnaire.Children.Add(new SingleQuestion()
            {
                PublicKey = questionWithSubstitutionsId,
                QuestionText = string.Format("hello %{0}%", underDeeperPropagationLevelQuestionVariableName)
            });

            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code__WB0019 = () =>
            resultErrors.Single().Code.ShouldEqual("WB0019");

        It should_return_error_with_two_references = () =>
            resultErrors.Single().References.Count().ShouldEqual(2);

        It should_return_first_error_reference_with_type_Question = () =>
            resultErrors.Single().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_first_error_reference_with_id_of_underDeeperPropagationLevelQuestionId = () =>
            resultErrors.Single().References.First().Id.ShouldEqual(questionWithSubstitutionsId);

        It should_return_last_error_reference_with_type_Question = () =>
            resultErrors.Single().References.Last().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_last_error_reference_with_id_of_underDeeperPropagationLevelQuestionVariableName = () =>
            resultErrors.Single().References.Last().Id.ShouldEqual(underDeeperPropagationLevelQuestionId);

        private static IEnumerable<QuestionnaireVerificationError> resultErrors;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static Guid questionWithSubstitutionsId;
        private static Guid underDeeperPropagationLevelQuestionId;
        private const string underDeeperPropagationLevelQuestionVariableName = "i_am_deeper_ddddd_deeper";
    }
}
