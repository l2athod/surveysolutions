using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Ncqrs.Spec;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_answering_question_used_in_substitutions : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");

            QuestionnaireDocument questionnaire = CreateQuestionnaireDocumentWithOneChapter(new Group("top level fixed group")
            {
                Children = new List<IComposite>
                {
                    Create.Entity.TextQuestion(questionId: substitutedQuestionId, variable: "subst"),

                    Create.Entity.TextQuestion(questionId: questionA, text: "question A %subst%"),
                    Create.Entity.TextQuestion(questionId: questionB, text: "question B %subst%"),
                    Create.Entity.StaticText(publicKey: staticTextId, text: "static text %subst%"),
                }.ToReadOnlyCollection()
            });

            IQuestionnaireStorage questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, Create.Entity.PlainQuestionnaire(questionnaire, 1));

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);

            eventContext = new EventContext();
            BecauseOf();
        }

        public void BecauseOf() => 
            interview.AnswerTextQuestion(Guid.NewGuid(), substitutedQuestionId, Empty.RosterVector, DateTime.Now, "answer");

        [NUnit.Framework.Test] public void should_raise_substitution_changed_event_with_2_questions () =>
            eventContext.ShouldContainEvent<SubstitutionTitlesChanged>(x => x.Questions.Length == 2);

        [NUnit.Framework.Test] public void should_raise_substitution_changed_event_with_1_static_text () =>
            eventContext.ShouldContainEvent<SubstitutionTitlesChanged>(x => x.StaticTexts.Length == 1);

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
        }

        static Guid substitutedQuestionId = Guid.Parse("88888888888888888888888888888888");
        static Guid questionA = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        static Guid questionB = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        static Guid staticTextId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        static Interview interview;
        static EventContext eventContext;
    }
}

