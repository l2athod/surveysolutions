using System;
using System.Collections.Generic;

using FluentAssertions;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;

namespace WB.Tests.Integration.InterviewTests.LanguageTests
{
    internal class when_answering_numeric_question_with_invalid_answer_and_all_questions_were_invalid_before_answer : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            appDomainContext = AppDomainContext.Create();
            BecauseOf();
        }

        public void BecauseOf() =>
            result = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                SetUp.MockedServiceLocator();

                var questionA = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                var questionB = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

                var interview = SetupInterview(
                    appDomainContext.AssemblyLoadContext,
                    Abc.Create.Entity.QuestionnaireDocumentWithOneChapter(children: new[]
                    {
                        Abc.Create.Entity.Group(null, "Chapter X", null, null, false, new IComposite[]
                        {
                            Abc.Create.Entity.NumericIntegerQuestion(id: questionA, variable: "a", validationExpression: "a > 0"),
                            Abc.Create.Entity.NumericIntegerQuestion(id: questionB, variable: "b", validationExpression: "b > 0"),
                        }),
                    }),
                    events: new object[]
                    {
                        Abc.Create.Event.NumericIntegerQuestionAnswered(
                            questionA, null, -1, null, null
                        ),
                        Abc.Create.Event.NumericIntegerQuestionAnswered(
                            questionB, null, -2, null, null
                        ),

                        Abc.Create.Event.AnswersDeclaredInvalid(new Dictionary<Identity, IReadOnlyList<FailedValidationCondition>>()
                        {
                            {
                                Abc.Create.Entity.Identity(questionA),
                                new List<FailedValidationCondition>() {new FailedValidationCondition(0)}
                            },
                            {
                                Abc.Create.Entity.Identity(questionB),
                                new List<FailedValidationCondition>() {new FailedValidationCondition(0)}
                            }
                        })
                    });

            using (var eventContext = new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(Guid.NewGuid(), questionA, RosterVector.Empty, DateTime.Now, -3);

                    return new InvokeResult
                    {
                        AnswersDeclaredValidEventCount = eventContext.Count<AnswersDeclaredValid>(),
                        AnswersDeclaredInvalidEventCount = eventContext.Count<AnswersDeclaredInvalid>(),
                    };
                }
            });

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        }

        [NUnit.Framework.Test] public void should_not_raise_AnswersDeclaredValid_event () =>
            result.AnswersDeclaredValidEventCount.Should().Be(0);

        [NUnit.Framework.Test] public void should_raise_AnswersDeclaredInvalid_event () =>
            result.AnswersDeclaredInvalidEventCount.Should().Be(0);

        private static AppDomainContext appDomainContext;
        private static InvokeResult result;

        [Serializable]
        private class InvokeResult
        {
            public int AnswersDeclaredValidEventCount { get; set; }
            public int AnswersDeclaredInvalidEventCount { get; set; }
        }
    }
}
