﻿using Machine.Specifications;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;
using WB.Core.SharedKernels.Enumerator.Models.Questionnaire.Questions;
using WB.Core.SharedKernels.Enumerator.Services;

namespace WB.Tests.Unit.BoundedContexts.QuestionnaireTester.Services.AnswerToStringServiceTests
{
    internal class when_passing_single_option_question : AnswerToStringServiceTestsContext
    {
        Establish context = () =>
        {
            answerToStringService = CreateAnswerToStringService();
            singleOptionAnswer = CreateSingleOptionAnswer(3);
            singleOptionQuestionModel = CreateSingleOptionQuestionModel(
                new[]
                {
                    new OptionModel() { Title = "1", Value = 1 },
                    new OptionModel() { Title = "2", Value = 2 },
                    new OptionModel() { Title = "3", Value = 3 },
                    new OptionModel() { Title = "4", Value = 4 },
                });
        };

        Because of = () =>
            result = answerToStringService.AnswerToUIString(singleOptionQuestionModel, singleOptionAnswer);

        It should_return_3 = () =>
            result.ShouldEqual("3");


        static string result;
        static SingleOptionAnswer singleOptionAnswer;
        static SingleOptionQuestionModel singleOptionQuestionModel;
        static IAnswerToStringService answerToStringService;
    }
}