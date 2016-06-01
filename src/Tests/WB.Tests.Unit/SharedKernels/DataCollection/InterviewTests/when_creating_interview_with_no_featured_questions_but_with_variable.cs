﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using NSubstitute;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.QuestionnaireEntities;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_creating_interview_with_no_featured_questions_but_with_variable : InterviewTestsContext
    {
        Establish context = () =>
        {
            eventContext = new EventContext();

            questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            responsibleSupervisorId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA00");
            variableId=Guid.NewGuid();
            questionnaireVersion = 18;

            questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);

            QuestionnaireDocument questionnaire = Create.Entity.QuestionnaireDocument(id: questionnaireId,
                children: new IComposite[]
                {

                    Create.Entity.Variable(id:variableId, type: VariableType.Boolean, expression: "true")
                });

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId,
                 new PlainQuestionnaire(questionnaire, 18));

            var expressionState = Substitute.For<ILatestInterviewExpressionState>();
            expressionState.Clone().Returns(expressionState);
            expressionState.ProcessVariables()
                .Returns(
                    new VariableValueChanges(new Dictionary<Identity, object>()
                    {
                        {Create.Entity.Identity(variableId, RosterVector.Empty), true}
                    }));

            var interviewExpressionStatePrototypeProvider = Mock.Of<IInterviewExpressionStatePrototypeProvider>(_ =>
                _.GetExpressionState(Moq.It.IsAny<Guid>(), Moq.It.IsAny<long>()) == expressionState);

            interview = Create.AggregateRoot.Interview(questionnaireRepository: questionnaireRepository,
                expressionProcessorStatePrototypeProvider: interviewExpressionStatePrototypeProvider);
        };

        Because of = () =>
            interview.CreateInterviewOnClient(questionnaireIdentity, responsibleSupervisorId, DateTime.Now, userId);

        It should_raise_InterviewCreated_event = () =>
            eventContext.ShouldContainEvent<InterviewOnClientCreated>();

        It should_provide_questionnaire_id_in_InterviewCreated_event = () =>
            eventContext.GetEvent<InterviewOnClientCreated>()
                .QuestionnaireId.ShouldEqual(questionnaireId);

        It should_provide_questionnaire_verstion_in_InterviewCreated_event = () =>
            eventContext.GetEvent<InterviewOnClientCreated>()
                .QuestionnaireVersion.ShouldEqual(questionnaireVersion);

        It should_set_variable_value = () =>
            eventContext.GetEvent<VariablesChanged>().ChangedVariables[0].NewValue.ShouldEqual(true);

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        private static EventContext eventContext;
        private static Guid questionnaireId;
        private static long questionnaireVersion;
        private static Guid userId;
        private static Guid responsibleSupervisorId;
        private static Guid variableId;
        private static Interview interview;
        private static QuestionnaireIdentity questionnaireIdentity;
    }
}