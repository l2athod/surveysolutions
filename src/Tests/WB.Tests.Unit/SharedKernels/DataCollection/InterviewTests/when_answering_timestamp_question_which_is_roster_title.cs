using System;
using FluentAssertions;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_answering_timestamp_question_which_is_roster_title : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFF1111111111");
            var questionnaireId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDD0000000000");
            
            timestampQuestionId = Guid.Parse("11111111111111111111111111111111");
            rosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

            var rosterSizeQuestionId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

            var questionnaireRoster = Create.Entity.Roster(rosterSizeSourceType: RosterSizeSourceType.Question,
                rosterTitleQuestionId: timestampQuestionId, rosterSizeQuestionId: rosterSizeQuestionId, rosterId: rosterId, 
                children: new []{
                    Create.Entity.DateTimeQuestion(questionId: timestampQuestionId, isTimestamp: true)}
            );

            var questionnaireRepository = Create.Fake.QuestionnaireRepositoryWithOneQuestionnaire(questionnaireId,
                Create.Entity.PlainQuestionnaire(CreateQuestionnaireDocumentWithOneChapter(new IComposite[]
                {
                    Create.Entity.NumericIntegerQuestion(id: rosterSizeQuestionId),
                    questionnaireRoster
                })));

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);
            interview.AnswerNumericIntegerQuestion(
                Create.Command.AnswerNumericIntegerQuestionCommand(interview.EventSourceId, userId, rosterSizeQuestionId, 1));

            eventContext = new EventContext();
            BecauseOf();
        }

        public void BecauseOf() =>
            interview.AnswerDateTimeQuestion(userId, timestampQuestionId, new RosterVector(new []{0m}), DateTime.Now, answerOnDateTimeQuestion);

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
        }

        [NUnit.Framework.Test] public void should_raise_DateTimeQuestionAnswered_event () =>
            eventContext.ShouldContainEvent<DateTimeQuestionAnswered>();

        [NUnit.Framework.Test] public void should_set_answer_on_timestamp_question_in_specified_format_for_RosterInstancesTitleChanged_event () =>
            eventContext.GetEvent<RosterInstancesTitleChanged>().ChangedInstances[0].Title.Should().Be(answerOnDateTimeQuestion.ToString(DateTimeFormat.DateWithTimeFormat));

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid timestampQuestionId;
        private static Guid rosterId;
        private static DateTime answerOnDateTimeQuestion = new DateTime(2016, 06, 16, 12, 13, 14);
    }
}
