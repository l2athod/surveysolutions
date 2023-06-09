using System;
using FluentAssertions;
using Moq;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Templates;
using WB.Core.BoundedContexts.Headquarters.DataExport;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;


namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PreloadingTemplateServiceTests
{
    internal class when_questionnaire_has_2_levels_and_GetFilePathToPreloadingTemplate_called : PreloadingTemplateServiceTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            exportedDataFormatter = new Mock<ITabularFormatExportService>();
            fileSystemAccessor = CreateIFileSystemAccessorMock();
            fileSystemAccessor.Setup(x => x.GetFilesInDirectory(Moq.It.IsAny<string>(), Moq.It.IsAny<bool>())).Returns(new[] { "1.tab" });

            var exportFileNameService = Mock.Of<IExportFileNameService>(x => 
                x.GetFileNameForBatchUploadByQuestionnaire(Moq.It.IsAny<QuestionnaireIdentity>()) == "template.zip");

            assignmentImportTemplateGenerator = CreatePreloadingTemplateService(
                fileSystemAccessor.Object,
                tabularFormatExportService: exportedDataFormatter.Object,
                exportFileNameService: exportFileNameService);
            BecauseOf();
        }

        public void BecauseOf() => result = assignmentImportTemplateGenerator.GetFilePathToPreloadingTemplate(questionnaireId, 1);

        [NUnit.Framework.Test] public void should_return_not_null_result () =>
           result.Should().NotBeNull();

        [NUnit.Framework.Test] public void should_only_create_template_for_preload_once () =>
            exportedDataFormatter.Verify(x => x.CreateTemplateFilesForAdvancedPreloading(new QuestionnaireIdentity(questionnaireId, 1), Moq.It.IsAny<string>()), Times.Once);

        private static AssignmentImportTemplateGenerator assignmentImportTemplateGenerator;
        private static string result;
        private static Mock<IFileSystemAccessor> fileSystemAccessor;
        private static Mock<ITabularFormatExportService> exportedDataFormatter;
        private static readonly Guid questionnaireId = Guid.NewGuid();
    }
}
