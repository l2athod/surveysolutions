﻿using System;
using FluentAssertions;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels
{
    [TestOf(typeof(VariableViewModel))]
    public class VariableViewModelTests
    {
        [Test]
        public void When_init_and_variable_value_is_null_Then_text_should_contains_variable_name()
        {
            //arrange
            var variableIdentity = Identity.Create(Guid.Parse("11111111111111111111111111111111"), RosterVector.Empty);
            var questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(Create.Entity.Variable(variableIdentity.Id));
            var interview = SetUp.StatefulInterview(questionnaire);
            var viewModel = Create.ViewModel.VariableViewModel(
                questionnaire: Create.Entity.PlainQuestionnaire(questionnaire),
                interviewRepository: Create.Fake.StatefulInterviewRepositoryWith(interview));

            //act
            viewModel.Init("interviewid", variableIdentity, Create.Other.NavigationState());
            //assert
            viewModel.Text.Should().Be("v1 : <empty>");
        }
    }
}
