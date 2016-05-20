﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    internal class when_interview_has_multioption_question_answer : ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            questionId = Guid.Parse("d7127d06-5668-4fa3-b255-8a2a0aaaa020");

            var questionnaire = Create.Other.QuestionnaireDocumentWithOneChapter(
                Create.Other.MultyOptionsQuestion(id: questionId, options: new List<Answer> {Create.Other.Answer("foo", 28), Create.Other.Answer("bar", 42), Create.Other.Answer("bartender", 18) }));

            exportViewFactory = CreateExportViewFactory();
            questionnaaireExportStructure = exportViewFactory.CreateQuestionnaireExportStructure(questionnaire, 1);

            interview = Create.Other.InterviewData(Create.Other.InterviewQuestion(questionId, new [] {42m, 18m}));
        };

         Because of = () => result = exportViewFactory.CreateInterviewDataExportView(questionnaaireExportStructure, interview);

        It should_put_answers_to_export = () => result.Levels.Length.ShouldEqual(1);

        It should_put_answers_to_export_in_appropriate_order = () =>
        {
            InterviewDataExportLevelView first = result.Levels.First();
            var exportedQuestion = first.Records.First().GetQuestions().First();
            exportedQuestion.Answers.Length.ShouldEqual(3);
            exportedQuestion.Answers.ShouldEqual(new [] {"0", "1", "1"});
        };

        static ExportViewFactory exportViewFactory;
        static QuestionnaireExportStructure questionnaaireExportStructure;
        static InterviewDataExportView result;
        static InterviewData interview;
        static Guid questionId;
    }
}