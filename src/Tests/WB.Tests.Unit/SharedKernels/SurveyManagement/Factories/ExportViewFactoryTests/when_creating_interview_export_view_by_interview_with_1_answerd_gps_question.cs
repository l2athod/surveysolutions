﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using NHibernate.Util;
using WB.Core.BoundedContexts.Headquarters.DataExport.Denormalizers;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Factories.ExportViewFactoryTests
{
    internal class when_creating_interview_export_view_by_interview_with_1_answerd_gps_question : ExportViewFactoryTestsContext
    {
        Establish context = () =>
        {
            gpsQuestionId = Guid.Parse("10000000000000000000000000000000");

            interviewData =
                Create.Entity.InterviewData(Create.Entity.InterviewQuestion(questionId: gpsQuestionId,
                    answer: Create.Entity.GeoPosition()));

            questionnaireDocument =
                Create.Entity.QuestionnaireDocument(children: Create.Entity.GpsCoordinateQuestion(questionId: gpsQuestionId, variable: "gps"));

            exportViewFactory = CreateExportViewFactory();
        };

        Because of = () =>
            result = exportViewFactory.CreateInterviewDataExportView(exportViewFactory.CreateQuestionnaireExportStructure(questionnaireDocument, 1),
                interviewData);

        It should_create_record__with_one_gps_question_which_contains_composite_answer = () =>
          result.Levels[0].Records[0].GetPlainAnswers().First().ShouldEqual(new[] { "1", "2", "3", "4", "1984-04-18T00:00:00" });

        private static ExportViewFactory exportViewFactory;
        private static InterviewDataExportView result;
        private static Guid gpsQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
        private static InterviewData interviewData;
    }
}