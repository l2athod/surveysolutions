﻿using System;
using System.Collections.Generic;
using System.Linq;
using AppDomainToolkit;
using Machine.Specifications;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;

namespace WB.Tests.Integration.InterviewTests.LanguageTests
{
    internal class when_answering_on_question_validation_expression_throws_exception : InterviewTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Setup.MockedServiceLocator();

                var questionnaireId = Guid.Parse("00000000000000000000000000000000");
                var actorId = Guid.Parse("99999999999999999999999999999999");
                var question1Id = Guid.Parse("11111111111111111111111111111111");
                var question2Id = Guid.Parse("22222222222222222222222222222222");

                var questionnaireDocument = Abc.Create.Entity.QuestionnaireDocumentWithOneChapter(questionnaireId,
                    Abc.Create.Entity.NumericIntegerQuestion(question1Id, "q1"),
                    Abc.Create.Entity.NumericIntegerQuestion(question2Id, "q2", validationExpression: "1/q1 == 1")
                    );

                var interview = SetupInterview(questionnaireDocument, new List<object>
                {
                    Abc.Create.Event.NumericIntegerQuestionAnswered(
                        question1Id, null, 0, null, null
                    )
                });

                var result = new InvokeResults();

                using (var eventContext = new EventContext())
                {
                    interview.AnswerNumericIntegerQuestion(actorId, question2Id, new decimal[0], DateTime.Now, 5);

                    result.Questions2ShouldBeDeclaredInvalid = 
                        eventContext.AnyEvent<AnswersDeclaredInvalid>(x => x.Questions.Any(q => q.Id == question2Id));
                }

                return result;
            });

        It should_declare_second_question_as_invalid = () =>
            results.Questions2ShouldBeDeclaredInvalid.ShouldBeTrue();

        Cleanup stuff = () =>
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        };

        private static InvokeResults results;
        private static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> appDomainContext;

        [Serializable]
        internal class InvokeResults
        {
            public bool Questions2ShouldBeDeclaredInvalid { get; set; }
        } 
    }
}