using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;
using WB.Core.SharedKernels.Enumerator.Implementation.Aggregates;

namespace WB.Tests.Unit.SharedKernels.Enumerator.StatefulInterviewTests
{
    internal class when_finding_referenced_answers_for_linked_question_on_roster_level_2_and_referenced_answers_are_on_roster_level_3 : StatefulInterviewTestsContext
    {
        Establish context = () =>
        {
            linkedQuestionRosterVector = new[] { 1m, 1m };
            var linkedQuestionRosters = new[] { referencedRoster1, referencedRoster2 };

            var referencedQuestionRosters = new[] { referencedRoster1, referencedRoster2, referencedRoster3 };

            IPlainQuestionnaireRepository questionnaireRepository = Setup.QuestionnaireRepositoryWithOneQuestionnaire(questionnaireId, _
                => _.HasQuestion(linkedQuestionId) == true
                && _.GetRosterLevelForQuestion(linkedQuestionId) == linkedQuestionRosters.Length
                && _.GetRostersFromTopToSpecifiedQuestion(linkedQuestionId) == linkedQuestionRosters
                && _.GetRosterSizeSourcesForEntity(linkedQuestionId) == linkedQuestionRosters
                && _.HasQuestion(referencedQuestionId) == true
                && _.GetRosterLevelForQuestion(referencedQuestionId) == referencedQuestionRosters.Length
                && _.GetRostersFromTopToSpecifiedQuestion(referencedQuestionId) == referencedQuestionRosters
                && _.GetRosterSizeSourcesForEntity(referencedQuestionId) == referencedQuestionRosters);

            interview = Create.StatefulInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            FillInterviewWithInstancesForThreeNestedRostersAndAnswersToTextQuestionInLastRoster(interview, referencedRoster1, referencedRoster2, referencedRoster3, referencedQuestionId);
        };

        Because of = () =>
            result = interview.FindAnswersOfReferencedQuestionForLinkedQuestion(referencedQuestionId, Create.Identity(linkedQuestionId, linkedQuestionRosterVector));

        It should_return_answers_with_roster_vector_starting_with_first_two_elements_of_linked_question_roster_vector = () =>
            result.Cast<TextAnswer>().Select(answer => answer.Answer)
                .ShouldContainOnly("1-1-1", "1-1-2");

        private static StatefulInterview interview;
        private static IEnumerable<BaseInterviewAnswer> result;
        private static Guid referencedQuestionId = Guid.Parse("55555555555555555555555555555555");
        private static Guid linkedQuestionId = Guid.Parse("11111111111111111111111111111111");
        private static decimal[] linkedQuestionRosterVector;
        private static Guid questionnaireId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid referencedRoster1 = Guid.Parse("00000000000000001111111111111111");
        private static Guid referencedRoster2 = Guid.Parse("00000000000000002222222222222222");
        private static Guid referencedRoster3 = Guid.Parse("00000000000000003333333333333333");
    }
}