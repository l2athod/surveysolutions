using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.Enumerator.StatefulInterviewTests
{
    internal class when_restoring_interview_state_from_sync_package_for_questionnaire_with_numeric_rosters : StatefulInterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {

            var fixedRosterIdentity = Identity.Create(Guid.Parse("11111111111111111111111111111111"),
                Create.Entity.RosterVector(1));
            var fixedNestedRosterIdentity = Identity.Create(Guid.Parse("22222222222222222222222222222222"), rosterVector);
            var fixedNestedNestedRosterIdentity = Identity.Create(Guid.Parse("33333333333333333333333333333333"),
                Create.Entity.RosterVector(1, 0, 3));

            IQuestionnaireStorage questionnaireRepository = Create.Fake.QuestionnaireRepositoryWithOneQuestionnaire(
            Create.Entity.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
                Create.Entity.NumericIntegerQuestion(integerQuestionId),
                Create.Entity.NumericRoster(roster1Id, rosterSizeQuestionId:integerQuestionId, rosterTitleQuestionId: decimalQuestionId, children: new IComposite[]
                {
                    Create.Entity.NumericRealQuestion(decimalQuestionId)
                }),
                Create.Entity.NumericRoster(roster2Id, rosterSizeQuestionId:integerQuestionId, rosterTitleQuestionId: textQuestionId, children: new IComposite[]
                {
                    Create.Entity.TextQuestion(textQuestionId)
                }),
                Create.Entity.NumericRoster(roster3Id, rosterSizeQuestionId:integerQuestionId, rosterTitleQuestionId: singleOptionQuestionId, children: new IComposite[]
                {
                    Create.Entity.SingleOptionQuestion(singleOptionQuestionId, answers: Create.Entity.Options(100, 200, 300).ToList())
                }),
                Create.Entity.NumericRoster(roster4Id, rosterSizeQuestionId:integerQuestionId, rosterTitleQuestionId: dateTimeQuestionId, children: new IComposite[]
                {
                    Create.Entity.DateTimeQuestion(dateTimeQuestionId)
                })
            }));

            interview = Create.AggregateRoot.StatefulInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            var answersDtos = new[]
            {
                CreateAnsweredQuestionSynchronizationDto(integerQuestionId, RosterVector.Empty, 1),
                CreateAnsweredQuestionSynchronizationDto(decimalQuestionId, rosterVector, 10.5),
                CreateAnsweredQuestionSynchronizationDto(dateTimeQuestionId, rosterVector, new DateTime(2005, 11, 30)),
                CreateAnsweredQuestionSynchronizationDto(singleOptionQuestionId, rosterVector, 100),
                CreateAnsweredQuestionSynchronizationDto(textQuestionId, rosterVector, "hello, world")
            };

            synchronizationDto = Create.Entity.InterviewSynchronizationDto(questionnaireId: questionnaireId, userId: userId, answers: answersDtos);

            eventContext = new EventContext();
            BecauseOf();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            eventContext.Dispose();
            eventContext = null;
        }

        private void BecauseOf() => interview.Synchronize(Create.Command.Synchronize(userId, synchronizationDto));

        [NUnit.Framework.Test] public void should_set_roster_title_to_roster1 () =>
            interview.GetRosterTitle(Identity.Create(roster1Id, rosterVector)).Should().Be("10.5");

        [NUnit.Framework.Test] public void should_set_roster_title_to_roster2 () =>
            interview.GetRosterTitle(Identity.Create(roster2Id, rosterVector)).Should().Be("hello, world");

        [NUnit.Framework.Test] public void should_set_roster_title_to_roster3 () =>
            interview.GetRosterTitle(Identity.Create(roster3Id, rosterVector)).Should().Be("Option 100");

        [NUnit.Framework.Test] public void should_set_roster_title_to_roster4 () =>
            interview.GetRosterTitle(Identity.Create(roster4Id, rosterVector)).Should().Be("2005-11-30");

        private static EventContext eventContext;
        private static InterviewSynchronizationDto synchronizationDto;
        private static StatefulInterview interview;
        private static readonly Guid integerQuestionId = Guid.Parse("00000000000000000000000000000001");
        private static readonly Guid decimalQuestionId = Guid.Parse("00000000000000000000000000000002");
        private static readonly Guid dateTimeQuestionId = Guid.Parse("00000000000000000000000000000003");
        private static readonly Guid singleOptionQuestionId = Guid.Parse("00000000000000000000000000000004");
        private static readonly Guid textQuestionId = Guid.Parse("00000000000000000000000000000005");
        private static readonly decimal[] rosterVector = new decimal[] { 0m };
        private static readonly Guid questionnaireId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static readonly Guid userId =   Guid.Parse("99999999999999999999999999999999");
        private static readonly Guid roster1Id = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static readonly Guid roster2Id = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        private static readonly Guid roster3Id = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static readonly Guid roster4Id = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
    }
}
