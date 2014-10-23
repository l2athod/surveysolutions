﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;
using WB.UI.Headquarters.Controllers;
using It = Machine.Specifications.It;

namespace WB.UI.Headquarters.Tests.ImportExportControllerTests
{
    internal class when_GetExportedDataAsync_is_called_and_exception_occured : ImportExportControllerTestContext
    {
        Establish context = () =>
        {
            questionnaireId = Guid.NewGuid();
            dataExportServiceMock = new Mock<IDataExportRepositoryWriter>();
            dataExportServiceMock.Setup(x => x.GetFilePathToExportedCompressedData(questionnaireId, 1)).Throws<NullReferenceException>();
            
            controller = CreateImportExportController(dataExportServiceMock.Object);
        };

        Because of = () => ExecuteAsync(controller, () => controller.GetExportedDataAsync(questionnaireId, 1), () =>
        {
            Result = controller.AsyncManager.Parameters["result"];
        });

        It should_have_null_result = () =>
            Result.ShouldBeNull();

        private static ImportExportController controller;
        private static Guid questionnaireId;
        private static object Result;
        private static Mock<IDataExportRepositoryWriter> dataExportServiceMock;

    }
}
