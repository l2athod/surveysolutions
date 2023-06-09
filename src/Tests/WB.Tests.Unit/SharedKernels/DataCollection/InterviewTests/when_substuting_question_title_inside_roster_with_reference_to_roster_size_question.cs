using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_substuting_question_title_inside_roster_with_reference_to_roster_size_question : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            rosterSizeQuestionId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            questionA = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            questionB = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            QuestionnaireDocument questionnaire = CreateQuestionnaireDocumentWithOneChapter(new Group("root")
            {
                Children = new List<IComposite>()
                {
                    Create.Entity.NumericIntegerQuestion(id: rosterSizeQuestionId, variable: "subst"),
                    Create.Entity.Roster(rosterSizeSourceType: RosterSizeSourceType.Question, 
                        rosterSizeQuestionId: rosterSizeQuestionId, 
                        title: "roster",
                        children: 
                            new List<IComposite>
                            {
                                Create.Entity.TextQuestion(questionId: questionA, text: "this is %subst% A"),
                                Create.Entity.TextQuestion(questionId: questionB, text: "this is %subst% B")
                            })
                }.ToReadOnlyCollection()
            });

            Guid questionnaireId = Guid.Parse("ACCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId,
                Create.Entity.PlainQuestionnaire(questionnaire, 1));

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            events = new EventContext();
            BecauseOf();
        }

        public void BecauseOf() => 
            interview.AnswerNumericIntegerQuestion(Guid.NewGuid(), rosterSizeQuestionId, Empty.RosterVector, DateTime.Now, 2);

        [NUnit.Framework.Test] public void should_raise_titles_changed_for_new_roster_instances () => 
            events.ShouldContainEvent<SubstitutionTitlesChanged>(x => x.Questions.Length == 4);

        static EventContext events;
        static Interview interview;
        static Guid rosterSizeQuestionId;
        static Guid questionA;
        static Guid questionB;
    }
}
