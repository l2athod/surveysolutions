using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.Enumerator.StatefulInterviewTests
{
    internal class when_synchronizing_interview_with_no_failing_validation_conditions : StatefulInterviewTestsContext
    {
        private Establish context = () =>
        {
            Guid questionnaireId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            Guid integerQuestionId = Guid.Parse("00000000000000000000000000000001");
            RosterVector rosterVector = Create.Entity.RosterVector(1m, 0m);
            var fixedRosterIdentity = Identity.Create(Guid.Parse("11111111111111111111111111111111"), Create.Entity.RosterVector(1));
            var fixedNestedRosterIdentity = Identity.Create(Guid.Parse("22222222222222222222222222222222"), Create.Entity.RosterVector(1,0));
            questionIdentity = new Identity(integerQuestionId, rosterVector);

            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(id: questionnaireId,
                children: Create.Entity.FixedRoster(
                    rosterId: fixedRosterIdentity.Id,
                    fixedTitles: new[] {new FixedRosterTitle(1, "fixed")},
                    children: new IComposite[]
                    {
                        Create.Entity.FixedRoster(
                            rosterId: fixedNestedRosterIdentity.Id,
                            fixedTitles: new[] {new FixedRosterTitle(0, "nested fixed")},
                            children: new IComposite[]
                            {
                                Create.Entity.NumericIntegerQuestion(questionIdentity.Id)
                            })
                    }));

            IQuestionnaireStorage questionnaireRepository = Setup.QuestionnaireRepositoryWithOneQuestionnaire(Create.Entity.QuestionnaireIdentity(questionnaireId), questionnaire);

            interview = Create.AggregateRoot.StatefulInterview(questionnaireId: questionnaireId,
                questionnaireRepository: questionnaireRepository, shouldBeInitialized: false
                );

            var answersDtos = new[]
            {
                CreateAnsweredQuestionSynchronizationDto(integerQuestionId, rosterVector, 1),
            };

            var rosterInstances = new Dictionary<InterviewItemId, RosterSynchronizationDto[]>
            {
                {
                    Create.Entity.InterviewItemId(fixedRosterIdentity.Id, fixedRosterIdentity.RosterVector),
                    new[] {Create.Entity.RosterSynchronizationDto(fixedRosterIdentity.Id, fixedRosterIdentity.RosterVector.Shrink(), fixedRosterIdentity.RosterVector.Last())}
                },
                {
                    Create.Entity.InterviewItemId(fixedNestedRosterIdentity.Id, fixedNestedRosterIdentity.RosterVector),
                    new[] {Create.Entity.RosterSynchronizationDto(fixedNestedRosterIdentity.Id, fixedNestedRosterIdentity.RosterVector.Shrink(), fixedNestedRosterIdentity.RosterVector.Last())}
                }
            };
            synchronizationDto = Create.Entity.InterviewSynchronizationDto(questionnaireId: questionnaireId,
                userId: userId, answers: answersDtos, rosterGroupInstances: rosterInstances);

            command = Create.Command.Synchronize(userId, synchronizationDto);
        };

        Because of = () => interview.Synchronize(command);

        It should_return_empty_failed_condition_messages = () => interview.GetFailedValidationMessages(questionIdentity, "Error").Count().ShouldEqual(0);

        static InterviewSynchronizationDto synchronizationDto;
        static StatefulInterview interview;
        static readonly Guid userId = Guid.Parse("99999999999999999999999999999999");
        static Identity questionIdentity;
        private static SynchronizeInterviewCommand command;
    }
}