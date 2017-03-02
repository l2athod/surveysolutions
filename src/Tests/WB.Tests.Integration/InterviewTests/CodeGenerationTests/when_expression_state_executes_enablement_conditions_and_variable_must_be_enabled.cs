﻿using System;
using AppDomainToolkit;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.SharedKernels.DataCollection.V9;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Tests.Abc;

namespace WB.Tests.Integration.InterviewTests.CodeGenerationTests
{
    internal class when_expression_state_executes_enablement_conditions_and_variable_must_be_enabled : CodeGenerationTestsContext
    {
        Establish context = () =>
        {
            appDomainContext = AppDomainContext.Create();
        };

        Because of = () =>
            results = Execute.InStandaloneAppDomain(appDomainContext.Domain, () =>
            {
                Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
                Guid variableId = Guid.Parse("11111111111111111111111111111112");
                Guid questionId = Guid.Parse("21111111111111111111111111111112");
                Guid groupId = Guid.Parse("31111111111111111111111111111112");

                AssemblyContext.SetupServiceLocator();

                QuestionnaireDocument questionnaireDocument = Create.Entity.QuestionnaireDocumentWithOneChapter(questionnaireId,
                    children: new IComposite[]
                    {
                        Create.Entity.Group(groupId, children: new IComposite[]
                        {
                            Create.Entity.TextQuestion(questionId: questionId, variable: "txt"),
                            Create.Entity.Variable(variableId, VariableType.LongInteger, "v1", "txt.Length")
                        })
                    });
                IInterviewExpressionStateV9 state =
                    GetInterviewExpressionState(questionnaireDocument, version: 16) as
                        IInterviewExpressionStateV9;

                state.UpdateTextAnswer(questionId, new decimal[0], "Nastya");
                state.DisableVariables(new[] { Create.Identity(variableId) });
                state.DisableGroups(new[] { Create.Identity(groupId) });
                state.SaveAllCurrentStatesAsPrevious();

                var enablementConditions = state.ProcessEnablementConditions();

                var variables = state.ProcessVariables();

                return new InvokeResults()
                {
                    IntVariableResult = (long?)variables.ChangedVariableValues[Abc.Create.Identity(variableId)],
                    IsVariableEnabled = enablementConditions.VariablesToBeEnabled.Contains(Abc.Create.Identity(variableId))
                };
            });

        It should_result_of_the_variable_be_equal_to_length_of_answer_on_text_question = () =>
             results.IntVariableResult.ShouldEqual(6);

        It should_variable_id_be_returned_as_enabled = () =>
           results.IsVariableEnabled.ShouldBeTrue();

        Cleanup stuff = () =>
        {
            appDomainContext.Dispose();
            appDomainContext = null;
        };

        private static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> appDomainContext;
        private static InvokeResults results;

        [Serializable]
        public class InvokeResults
        {
            public long? IntVariableResult { get; set; }
            public bool IsVariableEnabled { get; set; }
        }
    }
}