﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Moq;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Parser;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Verifier;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.ValueObjects;
using WB.Core.BoundedContexts.Headquarters.ValueObjects.PreloadedData;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.GenericSubdomains.Portable.Implementation.ServiceVariables;
using WB.Tests.Abc;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataVerifierTests
{
    internal class when_verifying_preloaded_data_with_incorrect_decimal_value : PreloadedDataVerifierTestContext
    {
        private Establish context = () =>
        {
            questionnaireId = Guid.Parse("11111111111111111111111111111111");
            questionId = Guid.Parse("21111111111111111111111111111111");
            questionnaire =
                CreateQuestionnaireDocumentWithOneChapter(new NumericQuestion()
                {
                    StataExportCaption = "q1",
                    PublicKey = questionId,
                    IsInteger = true,
                    QuestionType = QuestionType.Numeric
                });
            questionnaire.Title = "questionnaire";
            preloadedDataByFile = CreatePreloadedDataByFile(new[] { ServiceColumns.InterviewId, "q1"},
                new string[][] { new string[] { "1", "text"} },
                "questionnaire.csv");

            preloadedDataServiceMock = new Mock<IPreloadedDataService>();
            preloadedDataServiceMock.Setup(x => x.FindLevelInPreloadedData(Moq.It.IsAny<string>()))
                .Returns(new HeaderStructureForLevel()
                {
                    LevelIdColumnName = ServiceColumns.InterviewId,
                    HeaderItems =
                        new Dictionary<Guid, IExportedHeaderItem>
                        {
                            { Guid.NewGuid(), new ExportedQuestionHeaderItem() { VariableName = "q1", ColumnHeaders = new List<HeaderColumn>(){new HeaderColumn(){Name = "q1"}} } }
                        }
                });

            object outValue;

            preloadedDataServiceMock.Setup(
                x => x.ParseQuestionInLevel(Moq.It.IsAny<string>(), "q1", Moq.It.IsAny<HeaderStructureForLevel>(), out outValue)).Returns(ValueParsingResult.AnswerAsDecimalWasNotParsed);
            preloadedDataServiceMock.Setup(x => x.GetColumnIndexByHeaderName(preloadedDataByFile, Moq.It.IsAny<string>())).Returns(-1);
            importDataVerifier = CreatePreloadedDataVerifier(questionnaire, preloadedDataServiceMock.Object);
        };

        Because of =
            () => VerificationErrors = importDataVerifier.VerifyPanelFiles(Create.Entity.PreloadedDataByFile(preloadedDataByFile), preloadedDataServiceMock.Object).ToList();

        It should_result_has_1_error = () =>
            VerificationErrors.Count().ShouldEqual(1);

        It should_return_single_PL0019_error = () =>
            VerificationErrors.First().Code.ShouldEqual("PL0019");

        It should_return_reference_with_Cell_type = () =>
            VerificationErrors.First().References.First().Type.ShouldEqual(PreloadedDataVerificationReferenceType.Cell);

        private static ImportDataVerifier importDataVerifier;
        private static QuestionnaireDocument questionnaire;
        private static Guid questionnaireId;
        private static Guid questionId;
        private static PreloadedDataByFile preloadedDataByFile;

        private static Mock<IPreloadedDataService> preloadedDataServiceMock;
    }
}
