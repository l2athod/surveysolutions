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
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadedDataVerifierTests
{
    internal class when_verifying_preloaded_data_with_missing_numeric_value : PreloadedDataVerifierTestContext
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
            preloadedDataByFile = CreatePreloadedDataByFile(new[] { "Id", "q1"},
                new string[][] { new string[] { "1", ExportFormatSettings.MissingNumericQuestionValue } },
                "questionnaire.csv");

            preloadedDataServiceMock = new Mock<IPreloadedDataService>();
            preloadedDataServiceMock.Setup(x => x.FindLevelInPreloadedData(Moq.It.IsAny<string>()))
                .Returns(new HeaderStructureForLevel()
                {
                    HeaderItems =
                        new Dictionary<Guid, IExportedHeaderItem>
                        {
                            { Guid.NewGuid(), new ExportedHeaderItem() { VariableName = "q1", ColumnNames = new[] { "q1" } } }
                        }
                });

            preloadedDataServiceMock.Setup(x => x.GetColumnIndexByHeaderName(preloadedDataByFile, Moq.It.IsAny<string>())).Returns(-1);
            importDataVerifier = CreatePreloadedDataVerifier(questionnaire, preloadedDataServiceMock.Object);
        };

        Because of =
            () => importDataVerifier.VerifyPanelFiles(questionnaireId, 1, new[] { preloadedDataByFile }, status);

        It should_result_has_0_error = () =>
            status.VerificationState.Errors.Count().ShouldEqual(0);

        private static ImportDataVerifier importDataVerifier;
        private static QuestionnaireDocument questionnaire;
        private static Guid questionnaireId;
        private static Guid questionId;
        private static PreloadedDataByFile preloadedDataByFile;

        private static Mock<IPreloadedDataService> preloadedDataServiceMock;
    }
}
