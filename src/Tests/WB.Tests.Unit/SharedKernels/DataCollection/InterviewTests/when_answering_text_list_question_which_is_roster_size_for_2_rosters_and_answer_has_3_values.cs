using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Tests.Abc;


namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_answering_text_list_question_which_is_roster_size_for_2_rosters_and_answer_has_3_values : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");

            var questionnaire = Create.Entity.PlainQuestionnaire(Create.Entity.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
                Create.Entity.TextListQuestion(questionId: textListQuestionId),
                Create.Entity.Roster(rosterId: rosterAId, rosterSizeQuestionId: textListQuestionId),
                Create.Entity.Roster(rosterId: rosterBId, rosterSizeQuestionId: textListQuestionId),
            }));

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire);

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            eventContext = new EventContext();
            BecauseOf();
        }

        public void BecauseOf() =>
            interview.AnswerTextListQuestion(
                userId, textListQuestionId, emptyRosterVector, DateTime.Now,
                new []
                {
                    new Tuple<decimal, string>(1, "Answer 1"),
                    new Tuple<decimal, string>(2, "Answer 2"),
                    new Tuple<decimal, string>(3, "Answer 3"),
                });

        [NUnit.Framework.Test] public void should_raise_MultipleOptionsQuestionAnswered_event () =>
            eventContext.ShouldContainEvent<TextListQuestionAnswered>();

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_6_instances () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count().Should().Be(6);

        [NUnit.Framework.Test] public void should_not_raise_RosterInstancesRemoved_event () =>
            eventContext.ShouldNotContainEvent<RosterInstancesRemoved>();

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_3_instances_where_GroupId_equals_to_rosterAId () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.GroupId == rosterAId).Should().Be(3);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_3_instances_where_GroupId_equals_to_rosterBId () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.GroupId == rosterBId).Should().Be(3);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_roster_instance_id_equals_to_1 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.RosterInstanceId == 1).Should().Be(2);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_roster_instance_id_equals_to_2 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.RosterInstanceId == 2).Should().Be(2);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_roster_instance_id_equals_to_3 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.RosterInstanceId == 3).Should().Be(2);

        [NUnit.Framework.Test] public void should_set_empty_outer_roster_vector_to_all_instances_in_RosterInstancesAdded_event () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances
                .Should().OnlyContain(instance => instance.OuterRosterVector.Length == 0);

        [NUnit.Framework.Test] public void should_set_not_null_in_sort_index_to_all_instances_in_RosterInstancesAdded_event () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances
                .Should().OnlyContain(instance => instance.SortIndex != null);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_rsort_index_equals_to_0 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.SortIndex == 0).Should().Be(2);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_rsort_index_equals_to_1 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.SortIndex == 1).Should().Be(2);

        [NUnit.Framework.Test] public void should_raise_RosterInstancesAdded_event_with_2_instances_where_rsort_index_equals_to_2 () =>
            eventContext.GetEvent<RosterInstancesAdded>().Instances.Count(instance => instance.SortIndex == 2).Should().Be(2);

        [NUnit.Framework.Test] public void should_raise_1_RosterRowsTitleChanged_events () =>
            eventContext.ShouldContainEvents<RosterInstancesTitleChanged>(count: 1);

        [NUnit.Framework.Test] public void should_raise_RosterRowsTitleChanged_event_with_2_roster_instance_id_equals_to_1 () =>
            eventContext.ShouldContainEvent<RosterInstancesTitleChanged>(
                @event => @event.ChangedInstances.Count(row => row.RosterInstance.RosterInstanceId == 1) == 2);

        [NUnit.Framework.Test] public void should_raise_RosterRowsTitleChanged_event_with_2_roster_instance_id_equals_to_2 () =>
             eventContext.ShouldContainEvent<RosterInstancesTitleChanged>(
                @event => @event.ChangedInstances.Count(row => row.RosterInstance.RosterInstanceId == 2) == 2);

        [NUnit.Framework.Test] public void should_raise_RosterRowsTitleChanged_event_with_2_roster_instance_id_equals_to_3 () =>
             eventContext.ShouldContainEvent<RosterInstancesTitleChanged>(
                @event => @event.ChangedInstances.Count(row => row.RosterInstance.RosterInstanceId == 3) == 2);

        [NUnit.Framework.Test] public void should_set_2_affected_roster_ids_in_RosterRowsTitleChanged_events ()
        {
            var array = eventContext.GetEvents<RosterInstancesTitleChanged>()
                .SelectMany(@event => @event.ChangedInstances.Select(r => r.RosterInstance.GroupId)).ToArray();
            Assert.That(array, Does.Contain(rosterAId).And.Contain(rosterBId));
        }

        [NUnit.Framework.Test] public void should_set_empty_outer_roster_vector_to_all_RosterRowTitleChanged_events () =>
            eventContext.GetEvents<RosterInstancesTitleChanged>()
                .Should().OnlyContain(@event => @event.ChangedInstances.All(x => x.RosterInstance.OuterRosterVector.SequenceEqual(emptyRosterVector)));

        [NUnit.Framework.Test] public void should_set_title_to__Answer_3__in_all_RosterRowTitleChanged_events_with_roster_instance_id_equals_to_1 () => 
            eventContext.GetSingleEvent<RosterInstancesTitleChanged>()
                        .ChangedInstances
                        .Where(x => x.RosterInstance.RosterInstanceId == 1)
                        .Select(x => x.Title)
                        .Should().BeEquivalentTo("Answer 1", "Answer 1");

        [NUnit.Framework.Test] public void should_set_title_to__Answer_3__in_all_RosterRowTitleChanged_events_with_roster_instance_id_equals_to_2 () =>
            eventContext.GetSingleEvent<RosterInstancesTitleChanged>()
                        .ChangedInstances
                        .Where(x => x.RosterInstance.RosterInstanceId == 2)
                        .Select(x => x.Title)
                        .Should().BeEquivalentTo("Answer 2", "Answer 2");

        [NUnit.Framework.Test] public void should_set_title_to__Answer_3__in_all_RosterRowTitleChanged_events_with_roster_instance_id_equals_to_3 () =>
            eventContext.GetSingleEvent<RosterInstancesTitleChanged>()
                        .ChangedInstances
                        .Where(x => x.RosterInstance.RosterInstanceId == 3)
                        .Select(x => x.Title)
                        .Should().BeEquivalentTo("Answer 3", "Answer 3");

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
        }

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid propagatedQuestionId = Guid.Parse("22222222222222222222222222222222");
        private static Guid userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
        private static Guid textListQuestionId = Guid.Parse("11111111111111111111111111111111");
        private static decimal[] emptyRosterVector = new decimal[] { };
        private static Guid rosterAId = Guid.Parse("00000000000000003333333333333333");
        private static Guid rosterBId = Guid.Parse("00000000000000004444444444444444");
    }
}
