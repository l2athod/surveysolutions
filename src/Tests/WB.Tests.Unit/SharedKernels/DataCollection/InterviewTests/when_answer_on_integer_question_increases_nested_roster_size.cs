﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using WB.Core.SharedKernels.DataCollection.Repositories;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_answer_on_integer_question_increases_nested_roster_size : InterviewTestsContext
    {
        Establish context = () =>
        {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

            rosterGroupId = Guid.Parse("11111111111111111111111111111111");
            parentRosterGroupId = Guid.Parse("21111111111111111111111111111111");
            questionWhichIncreasesRosterSizeId = Guid.Parse("22222222222222222222222222222222");


            var questionnaire = Mock.Of<IQuestionnaire>(_

                                                        => _.HasQuestion(questionWhichIncreasesRosterSizeId) == true
                                                        && _.GetQuestionType(questionWhichIncreasesRosterSizeId) == QuestionType.Numeric
                                                        && _.IsQuestionInteger(questionWhichIncreasesRosterSizeId) == true
                                                        && _.GetRosterGroupsByRosterSizeQuestion(questionWhichIncreasesRosterSizeId) == new [] { rosterGroupId }

                                                        && _.HasGroup(rosterGroupId) == true
                                                        && _.GetRosterLevelForGroup(rosterGroupId) == 2
                                                        && _.GetRosterLevelForGroup(parentRosterGroupId) ==1
                                                        //&& _.GetGroupAndUnderlyingGroupsWithNotEmptyCustomEnablementConditions(rosterGroupId) == new [] { parentRosterGroupId, rosterGroupId }
                                                        && _.GetRostersFromTopToSpecifiedGroup(rosterGroupId) == new [] { parentRosterGroupId, rosterGroupId }
                                                        && _.GetRostersFromTopToSpecifiedGroup(parentRosterGroupId) == new[] { parentRosterGroupId }
                                                        && _.GetRostersFromTopToSpecifiedQuestion(questionWhichIncreasesRosterSizeId) == new[] { parentRosterGroupId });

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire);

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);
            interview.Apply(Create.Event.RosterInstancesAdded(parentRosterGroupId, new decimal[0], 0, null));
            eventContext = new EventContext();
        };

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        Because of = () =>
           interview.AnswerNumericIntegerQuestion(userId, questionWhichIncreasesRosterSizeId, new decimal[] { 0 }, DateTime.Now, 1);

        It should_raise_RosterInstancesAdded_event = () =>
            eventContext.ShouldContainEvent<RosterInstancesAdded>(@event
                => @event.Instances.Any(instance => instance.GroupId == rosterGroupId && instance.RosterInstanceId == 0 && instance.OuterRosterVector.Length == 1 && instance.OuterRosterVector[0] == 0));

        It should_not_raise_RosterInstancesRemoved_event = () =>
            eventContext.ShouldNotContainEvent<RosterInstancesRemoved>(@event
                => @event.Instances.Any(instance => instance.GroupId == rosterGroupId));

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid questionWhichIncreasesRosterSizeId;
        private static Guid rosterGroupId;
        private static Guid parentRosterGroupId;
    }
}
