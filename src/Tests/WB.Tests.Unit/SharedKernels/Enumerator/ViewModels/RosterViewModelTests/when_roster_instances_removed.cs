﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.RosterViewModelTests
{
    [TestFixture]
    internal class when_roster_instances_removed : RosterViewModelTests
    {
        [Test]
        public async Task should_remove_view_models()
        {
            var rosterId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            var chapterId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            var textListQuestionId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            var interviewerId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");


            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(chapterId,
                Create.Entity.TextListQuestion(textListQuestionId),
                Create.Entity.Roster(rosterId, rosterSizeSourceType: RosterSizeSourceType.Question, rosterSizeQuestionId: textListQuestionId));

            var interview = Setup.StatefulInterview(questionnaire);
            interview.AnswerTextListQuestion(interviewerId, textListQuestionId, RosterVector.Empty, DateTime.UtcNow,
                new[]
                {
                    new Tuple<decimal, string>(1, "option 1"),
                    new Tuple<decimal, string>(5, "option 5"),
                });

            var statefulInterviewRepository = Create.Fake.StatefulInterviewRepositoryWith(interview);
            var viewModel = this.CreateViewModel(interviewRepository: statefulInterviewRepository);
            var navigationState = Create.Other.NavigationState(statefulInterviewRepository);

            await navigationState.NavigateTo(Create.Entity.NavigationIdentity(Identity.Create(chapterId, RosterVector.Empty)));
            viewModel.Init(null, Create.Entity.Identity(rosterId), navigationState);

            interview.AnswerTextListQuestion(interviewerId, textListQuestionId, RosterVector.Empty, DateTime.UtcNow,
                new[]
                {
                    new Tuple<decimal, string>(5, "option 5"),
                });

            viewModel.Handle(Create.Event.RosterInstancesRemoved(rosterId, new [] { Create.Entity.RosterVector(1)}));

            Assert.That(viewModel.RosterInstances.Select(x => x.Identity).ToArray(),
                Is.EquivalentTo(new[] {Identity.Create(rosterId, Create.Entity.RosterVector(5))}));
        }
    }

    [TestFixture]
    internal class when_reordering_roster_instances : RosterViewModelTests
    {
        [Test]
        public async Task should_remove_view_models()
        {
            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(Id.gA, children: new IComposite[]
            {
                Create.Entity.YesNoQuestion(questionId: Id.g1, answers: new[]{ 1, 2, 3 }, ordered: true),
                Create.Entity.Roster(rosterId: Id.g2, rosterSizeSourceType: RosterSizeSourceType.Question, rosterSizeQuestionId: Id.g1),
            });

            var interview = Setup.StatefulInterview(questionnaire);
            interview.AnswerYesNoQuestion(Create.Command.AnswerYesNoQuestion(questionId: Id.g1,
                answeredOptions: new[]
                {
                    Create.Entity.AnsweredYesNoOption(1, true),
                    Create.Entity.AnsweredYesNoOption(2, true),
                    Create.Entity.AnsweredYesNoOption(3, true),
                }));

            var statefulInterviewRepository = Create.Fake.StatefulInterviewRepositoryWith(interview);
            var questionnaireStorage = Setup.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire);

            var viewModel = this.CreateViewModel(
                statefulInterviewRepository, 
                questionnaireRepository: questionnaireStorage);

            var navigationState = Create.Other.NavigationState(statefulInterviewRepository);

            await navigationState.NavigateTo(Create.Entity.NavigationIdentity(Identity.Create(Id.gA, RosterVector.Empty)));

            viewModel.Init(null, Create.Identity(Id.g2), navigationState);
          
            interview.AnswerYesNoQuestion(Create.Command.AnswerYesNoQuestion(
                questionId: Id.g1,
                answeredOptions: new[]
                {
                    Create.Entity.AnsweredYesNoOption(value: 2, answer: true),
                    Create.Entity.AnsweredYesNoOption(value: 3, answer: true),
                    Create.Entity.AnsweredYesNoOption(value: 1, answer: true),
                }));

            viewModel.Handle(Create.Event.YesNoQuestionAnswered(Id.g1, new []
            {
                Create.Entity.AnsweredYesNoOption(2, true),
                Create.Entity.AnsweredYesNoOption(3, true),
                Create.Entity.AnsweredYesNoOption(1, true),
            }));

            var rosters = viewModel.RosterInstances.Select(x => x.Identity).ToArray();
            Assert.That(rosters[0].RosterVector.Last(), Is.EqualTo(2));
            Assert.That(rosters[1].RosterVector.Last(), Is.EqualTo(3));
            Assert.That(rosters[2].RosterVector.Last(), Is.EqualTo(1));
        }
    }
}
