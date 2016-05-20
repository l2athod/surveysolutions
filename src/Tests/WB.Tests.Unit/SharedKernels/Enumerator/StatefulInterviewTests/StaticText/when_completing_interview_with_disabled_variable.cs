﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Implementation.Aggregates;

namespace WB.Tests.Unit.SharedKernels.Enumerator.StatefulInterviewTests.StaticText
{
    internal class when_completing_interview_with_disabled_variable : StatefulInterviewTestsContext
    {
        Establish context = () =>
        {
            variableIdentity = Create.Other.Identity(Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"), RosterVector.Empty);

            var questionnaireId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            var questionnaire = Create.Other.PlainQuestionnaire(Create.Other.QuestionnaireDocument(questionnaireId,
                Create.Other.Group(children: new List<IComposite>()
                {
                    Create.Other.Variable(variableIdentity.Id)
                })));

            var plainQuestionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire);
            statefulInterview = Create.Other.StatefulInterview(questionnaireRepository: plainQuestionnaireRepository);

            statefulInterview.Apply(Create.Event.InterviewStatusChanged(status: InterviewStatus.InterviewerAssigned));
            statefulInterview.Apply(Create.Event.VariablesChanged(Create.Event.ChangedVariable(variableIdentity,"a")));
            statefulInterview.Apply(Create.Event.VariablesDisabled(variableIdentity));

            eventContext = new EventContext();
        };

        Because of = () => statefulInterview.Complete(Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD"), "", DateTime.Now);

        It should_raize_variable_disabled_event = () => eventContext.ShouldContainEvent<VariablesDisabled>(v=>v.Variables[0]==variableIdentity);
        It should_not_raize_variable_enabled_event = () => eventContext.ShouldNotContainEvent<VariablesEnabled>();
        It should_raize_variable_changed_event = () => eventContext.ShouldContainEvent<VariablesChanged>(v => v.ChangedVariables[0].Identity == variableIdentity);

        static StatefulInterview statefulInterview;
        static Identity variableIdentity;
        static EventContext eventContext;
    }
}