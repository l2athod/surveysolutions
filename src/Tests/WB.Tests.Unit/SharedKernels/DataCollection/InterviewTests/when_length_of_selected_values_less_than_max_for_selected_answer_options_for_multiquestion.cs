using System;
using System.Linq;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_length_of_selected_values_less_than_max_for_selected_answer_options_for_multiquestion : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

            validatingQuestionId = Guid.Parse("11111111111111111111111111111111");

            var questionnaire = Create.Entity.PlainQuestionnaire(Create.Entity.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
                Create.Entity.MultipleOptionsQuestion(questionId: validatingQuestionId, maxAllowedAnswers: 3, answers: new [] { 1, 2, 3, 4 }),
            }));

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire);

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            eventContext = new EventContext();
            BecauseOf();
        }

        private void BecauseOf() => interview.AnswerMultipleOptionsQuestion(userId, validatingQuestionId, new decimal[] { }, DateTime.Now, selectedValues);

        [NUnit.Framework.Test] public void should_raise_MultipleOptionsQuestionAnswered_event () =>
            eventContext.ShouldContainEvent<MultipleOptionsQuestionAnswered>(@event
                => @event.QuestionId == validatingQuestionId && @event.SelectedValues.Select(x => (int) x).SequenceEqual(selectedValues));

        private static Guid validatingQuestionId;
        private static int[] selectedValues = { 1, 2, 3 };
        private static Interview interview;
        private static Guid userId;
        private static EventContext eventContext;
    }
}
