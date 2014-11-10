﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_that_has_roster_with_variable_name_equal_to_question_variable_name : QuestionnaireVerifierTestsContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaireDocument(new IComposite[]
            {
                new Group
                {
                    PublicKey = rosterId,
                    IsRoster = true,
                    RosterFixedTitles = new[] { "1", "2" },
                    RosterSizeSource = RosterSizeSourceType.FixedTitles,
                    VariableName = nonUniqueVariableName,
                    Children = new List<IComposite>() { new TextListQuestion() { PublicKey = Guid.NewGuid(), StataExportCaption = "var1" } }
                },
                new TextQuestion() { PublicKey = questionId, QuestionType = QuestionType.Text, StataExportCaption = nonUniqueVariableName, QuestionText = "text question"},
            });

            verifier = CreateQuestionnaireVerifier();
        };

        Because of = () =>
            resultErrors = verifier.Verify(questionnaire);

        It should_return_1_error = () =>
            resultErrors.Count().ShouldEqual(1);

        It should_return_error_with_code_WB0093 = () =>
            resultErrors.First().Code.ShouldEqual("WB0093");

        It should_return_error_with_two_references = () =>
            resultErrors.First().References.Count().ShouldEqual(2);

        It should_return_error_with_first_references_with_Group_type = () =>
            resultErrors.First().References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Group);

        It should_return_error_with_first_references_with_id_equals_rosterId = () =>
            resultErrors.First().References.First().Id.ShouldEqual(rosterId);

        It should_return_error_with_second_references_with_Question_type = () =>
         resultErrors.First().References.Last().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        It should_return_error_with_second_references_with_id_equals_questionId = () =>
          resultErrors.First().References.Last().Id.ShouldEqual(questionId);

        private static IEnumerable<QuestionnaireVerificationError> resultErrors;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static Guid rosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static Guid questionId = Guid.Parse("11111111111111111111111111111111");

        private static string nonUniqueVariableName = "variable";
    }
}
