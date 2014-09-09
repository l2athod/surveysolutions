﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Services.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.ServiceTests.DataExport.FileBasedDataExportServiceTests
{
    internal class when_DeleteExportedDataForQuestionnaireVersion_is_called_and_directory_is_present : FileBasedDataExportServiceTestContext
    {
        Establish context = () =>
        {
            dataFileExportServiceMock = new Mock<IDataFileExportService>();
            fileSystemAccessorMock = new Mock<IFileSystemAccessor>();
            fileSystemAccessorMock
                .Setup(x => x.IsDirectoryExists(Moq.It.IsAny<string>()))
                .Returns(true)
                .Callback<string>(directory => existingDirectory = directory);

            fileBasedDataExportService = CreateFileBasedDataExportService(fileSystemAccessorMock.Object, dataFileExportServiceMock.Object);
        };

        Because of = () => fileBasedDataExportService.DeleteExportedDataForQuestionnaireVersion(Guid.NewGuid(),1);


        It should_delete_directory = () =>
            fileSystemAccessorMock.Verify(accessor => accessor.DeleteDirectory(existingDirectory), Times.Once);

        private static FileBasedDataExportService fileBasedDataExportService;
        private static Mock<IFileSystemAccessor> fileSystemAccessorMock;
        private static Mock<IDataFileExportService> dataFileExportServiceMock;

        private static string existingDirectory;
    }
}
