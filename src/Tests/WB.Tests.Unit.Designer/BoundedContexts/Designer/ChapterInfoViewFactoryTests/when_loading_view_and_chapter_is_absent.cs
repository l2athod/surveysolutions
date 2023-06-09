using FluentAssertions;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.ChapterInfoViewFactoryTests
{
    internal class when_loading_view_and_chapter_is_absent : ChapterInfoViewFactoryContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var repositoryMock = new Mock<IDesignerQuestionnaireStorage>();

            repositoryMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(CreateQuestionnaireDocumentWithoutChapters(questionnaireId.QuestionnaireId.FormatGuid()));

            factory = CreateChapterInfoViewFactory(repository: repositoryMock.Object);
            BecauseOf();
        }

        private void BecauseOf() =>
            view = factory.Load(questionnaireId, chapterId);

        [NUnit.Framework.Test] public void should_chapter_be_null () =>
            view.Should().BeNull();

        private static NewChapterView view;
        private static ChapterInfoViewFactory factory;
        private static QuestionnaireRevision questionnaireId = Create.QuestionnaireRevision("11111111111111111111111111111111");
        private static string chapterId = "22222222222222222222222222222222";
    }
}
