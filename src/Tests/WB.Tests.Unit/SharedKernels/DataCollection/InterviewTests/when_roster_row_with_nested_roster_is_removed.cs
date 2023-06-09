using System;
using System.Linq;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_roster_row_with_nested_roster_is_removed : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

            rosterGroupId = Guid.Parse("11111111111111111111111111111111");
            parentRosterGroupId = Guid.Parse("21111111111111111111111111111111");
            questionWhichIncreasesRosterSizeId = Guid.Parse("22222222222222222222222222222222");
            questionInParentRosterId = Guid.Parse("31111111111111111111111111111111");

            var questionnaire = Create.Entity.PlainQuestionnaire(Create.Entity.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
                Create.Entity.NumericIntegerQuestion(id: questionWhichIncreasesRosterSizeId),

                Create.Entity.Roster(rosterId: parentRosterGroupId, rosterSizeQuestionId: questionWhichIncreasesRosterSizeId, children: new IComposite[]
                {
                    Create.Entity.NumericIntegerQuestion(id: questionInParentRosterId),
                    Create.Entity.Roster(rosterId: rosterGroupId, rosterSizeQuestionId: questionWhichIncreasesRosterSizeId),
                }),
            }));

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire);

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            interview.Apply(new NumericIntegerQuestionAnswered(userId, questionWhichIncreasesRosterSizeId, new decimal[0], DateTime.Now, 2));

            interview.Apply(Create.Event.RosterInstancesAdded(parentRosterGroupId, new decimal[0], 0, null));
            interview.Apply(Create.Event.RosterInstancesAdded(parentRosterGroupId, new decimal[0], 1, null));
            interview.Apply(Create.Event.RosterInstancesAdded(rosterGroupId, new decimal[] { 0 }, 0, null));
            interview.Apply(Create.Event.RosterInstancesAdded(rosterGroupId, new decimal[] { 1 }, 0, null));

            interview.Apply(new NumericIntegerQuestionAnswered(userId, questionInParentRosterId, new decimal[] {0}, DateTime.Now, 2));
            interview.Apply(new NumericIntegerQuestionAnswered(userId, questionInParentRosterId, new decimal[] { 1 }, DateTime.Now, 2));

            eventContext = new EventContext();
            BecauseOf();
        }

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
        }

        public void BecauseOf() =>
           interview.AnswerNumericIntegerQuestion(userId, questionWhichIncreasesRosterSizeId, new decimal[0], DateTime.Now, 1);

        [NUnit.Framework.Test] public void should_not_raise_RosterInstancesRemoved_event_for_first_row () =>
            eventContext.ShouldNotContainEvent<RosterInstancesRemoved>(@event
                => @event.Instances.Any(instance => instance.GroupId == parentRosterGroupId && instance.RosterInstanceId == 0 && instance.OuterRosterVector.Length == 0));

        [NUnit.Framework.Test] public void should_raise_RosterInstancesRemoved_event_for_second_row () =>
            eventContext.ShouldContainEvent<RosterInstancesRemoved>(@event
                => @event.Instances.Any(instance => instance.GroupId == parentRosterGroupId && instance.RosterInstanceId == 1 && instance.OuterRosterVector.Length == 0));

        [NUnit.Framework.Test] public void should_not_raise_RosterInstancesRemoved_of_nested_roster_event_for_first_row () =>
            eventContext.ShouldNotContainEvent<RosterInstancesRemoved>(@event
                => @event.Instances.Any(instance => instance.GroupId == rosterGroupId && instance.RosterInstanceId == 0 && instance.OuterRosterVector.SequenceEqual(new decimal[] { 0 })));

        [NUnit.Framework.Test] public void should_raise_RosterInstancesRemoved_of_nested_roster_event_for_second_row () =>
            eventContext.ShouldContainEvent<RosterInstancesRemoved>(@event
                => @event.Instances.Any(instance => instance.GroupId == rosterGroupId && instance.RosterInstanceId == 0 && instance.OuterRosterVector.SequenceEqual(new decimal[] { 1 })));

        [NUnit.Framework.Test] public void should_not_raise_RosterInstancesAdded_event () =>
            eventContext.ShouldNotContainEvent<RosterInstancesAdded>(@event
                => @event.Instances.Any(instance => instance.GroupId == rosterGroupId));

        [NUnit.Framework.Test] public void should_not_raise_AnswersRemoved_event_for_first_row () =>
            eventContext.ShouldNotContainEvent<AnswersRemoved>(@event
                => @event.Questions.Any(question => question.Id == questionInParentRosterId && question.RosterVector[0] == 0 && question.RosterVector.Length == 1));

        [NUnit.Framework.Test] public void should_raise_AnswersRemoved_event_for_second_row () =>
            eventContext.ShouldContainEvent<AnswersRemoved>(@event
                => @event.Questions.Any(question => question.Id == questionInParentRosterId && question.RosterVector[0] == 1 && question.RosterVector.Length == 1));

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid questionWhichIncreasesRosterSizeId;
        private static Guid rosterGroupId;
        private static Guid questionInParentRosterId;
        private static Guid parentRosterGroupId;
    }
}
