﻿using System;
using Machine.Specifications;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;

namespace WB.Tests.Integration.InterviewTests.LinkedQuestions
{
    [TestOf(typeof(Interview))]
    internal class when_applying_linked_options_changed_event_and_linked_question_from_removed_roster
    {
        [Test]
        public void should_not_throw_exception()
        {
            var userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFF1111111111");
            var textListQuestionId = Guid.Parse("11111111111111111111111111111111");
            var textListRosterId =   Guid.Parse("22222222222222222222222222222222");
            var singleLinkedToListRosterId = Guid.Parse("33333333333333333333333333333333");

            var questionnaireDocument = Create.QuestionnaireDocumentWithOneChapter(children: new IComposite[]
            {
               Abc.Create.Entity.TextListQuestion(questionId: textListQuestionId, variable: null,
                   enablementCondition: null, validationExpression: null),
               Abc.Create.Entity.Roster(textListRosterId, rosterSizeSourceType: RosterSizeSourceType.Question, rosterSizeQuestionId: textListQuestionId, children:new []
               {
                   Create.SingleOptionQuestion(singleLinkedToLislinkedToRosterIddToRosterId: textListRosterId)
               }) 
            });

            var interview = Create.StatefulInterview(questionnaireDocument);

            interview.AnswerTextListQuestion(userId: userId, questionId: textListQuestionId,
                answerTime: DateTime.Now, rosterVector: new decimal[0],
                answers: new[] {new Tuple<decimal, string>(1, "a"), new Tuple<decimal, string>(2, "b"), new Tuple<decimal, string>(3, "c") });

            interview.AnswerTextListQuestion(userId: userId, questionId: textListQuestionId,
                answerTime: DateTime.Now, rosterVector: new decimal[0],
                answers: new[] { new Tuple<decimal, string>(1, "a"), new Tuple<decimal, string>(3, "c") });

            Assert.DoesNotThrow(() => interview.Apply(Abc.Create.Event.LinkedOptionsChanged(new [] {
                new ChangedLinkedOptions(Identity.Create(singleLinkedToListRosterId, Create.RosterVector(1)), new[] { Create.RosterVector(1), Create.RosterVector(3) }),
                new ChangedLinkedOptions(Identity.Create(singleLinkedToListRosterId, Create.RosterVector(2)), new[] { Create.RosterVector(1), Create.RosterVector(3) }),
                new ChangedLinkedOptions(Identity.Create(singleLinkedToListRosterId, Create.RosterVector(3)), new[] { Create.RosterVector(1), Create.RosterVector(3) }),
            })));
        }
    }
}