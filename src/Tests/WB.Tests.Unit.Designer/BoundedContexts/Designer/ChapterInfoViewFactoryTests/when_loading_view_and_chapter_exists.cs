using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using WB.Core.GenericSubdomains.Portable;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.ChapterInfoViewFactoryTests
{
    internal class when_loading_view_and_chapter_exists : ChapterInfoViewFactoryContext
    {
        [OneTimeSetUp]
        public void context()
        {
            var repositoryMock = new Mock<IDesignerQuestionnaireStorage>();

            repositoryMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(Create.QuestionnaireDocumentWithOneChapter(chapterId,
                    Create.TextListQuestion(variable: "list"),
                    Create.FixedRoster(variable: "fixed_roster"),
                    Create.Variable(variableName: "variable")
                ));

            factory = CreateChapterInfoViewFactory(repository: repositoryMock.Object);
            BecauseOf();
        }

        private void BecauseOf() =>
            view = factory.Load(questionnaireId, chapterId.FormatGuid());

        [Test]
        public void should_find_chapter() =>
            view.Should().NotBeNull();

        [Test]
        public void should_chapter_id_be_equal_chapterId() =>
            view.Chapter.ItemId.Should().Be(chapterId.FormatGuid());

        [Test]
        public void should_view_have_all_variabe_names_() =>
            view.VariableNames.Length.Should().Be(keywordsAndVariables.Length);

        [Test]
        public void should_contain_all_variabe_names_() =>
            view.VariableNames.Select(x => x.Name).Should().Contain(keywordsAndVariables);

        private static NewChapterView view;
        private static ChapterInfoViewFactory factory;
        private static QuestionnaireRevision questionnaireId = Create.QuestionnaireRevision("11111111111111111111111111111111");
        private static Guid chapterId = Guid.Parse("22222222222222222222222222222222");

        private static readonly string[] keywordsAndVariables =
        {
            "list",
            "fixed_roster",
            "variable",
            "self",
            "@optioncode",
            "@rowindex",
            "@rowcode"
        };
    }
}
