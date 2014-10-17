﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Moq;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.Interview.InterviewExportedDataEventHandlerTests
{
    internal class when_InterviewApproved_recived_by_interview_with_linked_question_referenced_on_third_level_roster : InterviewExportedDataEventHandlerTestContext
    {
        Establish context = () =>
        {
            dataExportServiceMock = CreateDataExportService(r => result = r);

            linkedQuestionSourceId = Guid.Parse("12222222222222222222222222222222");
            rosterId = Guid.Parse("13333333333333333333333333333333");
            var nestedRosterId = Guid.Parse("23333333333333333333333333333333");

            linkedQuestionId = Guid.Parse("10000000000000000000000000000000");

            questionnarie = CreateQuestionnaireDocumentWithOneChapter(
                new SingleQuestion()
                {
                    PublicKey = linkedQuestionId,
                    QuestionType = QuestionType.SingleOption,
                    LinkedToQuestionId = linkedQuestionSourceId
                },
                new Group()
                {
                    PublicKey = rosterId,
                    IsRoster = true,
                    RosterFixedTitles = new string[] { "t1", "t2" },
                    RosterSizeSource = RosterSizeSourceType.FixedTitles,
                    Children = new List<IComposite>
                    {
                        new Group()
                        {
                            PublicKey = nestedRosterId,
                            IsRoster = true,
                            RosterFixedTitles = new string[] { "n1", "n2" },
                            RosterSizeSource = RosterSizeSourceType.FixedTitles,
                            Children = new List<IComposite>
                            {
                                new NumericQuestion()
                                {
                                    PublicKey = linkedQuestionSourceId,
                                    QuestionType = QuestionType.Numeric,
                                    StataExportCaption = "q1"
                                }
                            }
                        }
                    }
                });

            InterviewData interview = CreateInterviewData();
            var textListQuestion = interview.Levels["#"].GetOrCreateQuestion(linkedQuestionId);
            textListQuestion.Answer = new decimal[] { 0, 0 };

            interviewExportedDataDenormalizer = CreateInterviewExportedDataEventHandlerForQuestionnarieCreatedByMethod(
                templateCreationAction:() => questionnarie,
                dataCreationAction:() => interview, dataExportService:dataExportServiceMock.Object);
        };

        Because of = () =>
             interviewExportedDataDenormalizer.Handle(CreateInterviewApprovedByHQPublishableEvent());

        It should_linked_question_have_one_answer = () =>
           GetLevel(result, new Guid[0]).Records[0].Questions[0].Answers.Length.ShouldEqual(1);

        It should_linked_question_have_first_answer_be_equal_to_0 = () =>
           GetLevel(result, new Guid[0]).Records[0].Questions[0].Answers[0].ShouldEqual("[0|0]");

        private static InterviewExportedDataDenormalizer interviewExportedDataDenormalizer;
        private static InterviewDataExportView result;
        private static Guid rosterId;
        private static Guid linkedQuestionId;
        private static Guid linkedQuestionSourceId;
        private static QuestionnaireDocument questionnarie;
        private static Mock<IDataExportService> dataExportServiceMock;
    }
}
