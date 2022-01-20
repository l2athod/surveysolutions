using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.GenericSubdomains.Portable;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;

namespace WB.Tests.Unit.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_single_option_and_options_count_more_than_200 : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            int incrementer = 0;
            questionnaire = CreateQuestionnaireDocumentWithOneChapter(new Group("Group")
            {
                Children = new List<IComposite>
                {
                    Create.SingleOptionQuestion(
                    
                        singleOptionId,
                        isComboBox: false,
                        answers :
                            new List<Answer>(
                                new Answer[201].Select(
                                    answer =>
                                        new Answer()
                                        {
                                            AnswerValue = incrementer.ToString(),
                                            AnswerText = (incrementer++).ToString()
                                        }))
                    )
                }.ToReadOnlyCollection()
            });

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.GetAllErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_message_with_code__WB0075 () =>
            verificationMessages.ShouldContainError("WB0076");

        [NUnit.Framework.Test] public void should_return_message_with_level_general () =>
            verificationMessages.Single(e => e.Code == "WB0076").MessageLevel.Should().Be(VerificationMessageLevel.General);
        
        [NUnit.Framework.Test] public void should_return_message_with_1_references () =>
            verificationMessages.Single(e => e.Code == "WB0076").References.Count().Should().Be(1);

        [NUnit.Framework.Test] public void should_return_message_reference_with_type_Question () =>
            verificationMessages.Single(e => e.Code == "WB0076").References.First().Type.Should().Be(QuestionnaireVerificationReferenceType.Question);

        [NUnit.Framework.Test] public void should_return_message_reference_with_id_of_questionId () =>
            verificationMessages.Single(e => e.Code == "WB0076").References.First().Id.Should().Be(singleOptionId);

        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;

        private static Guid singleOptionId = Guid.Parse("10000000000000000000000000000000");
    }
}
