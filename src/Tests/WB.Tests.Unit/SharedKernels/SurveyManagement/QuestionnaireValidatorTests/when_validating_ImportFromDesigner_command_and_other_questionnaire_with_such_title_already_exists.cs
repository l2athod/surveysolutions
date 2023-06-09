using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Commands;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Tests.Abc;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.QuestionnaireValidatorTests
{
    internal class when_validating_ImportFromDesigner_command_and_different_questionnaire_with_such_title_already_exists
    {
        [NUnit.Framework.Test]
        public void should_throw_QuestionnaireException()
        {
            command = Create.Command.ImportFromDesigner(title: title, questionnaireId: importedQuestionnaireId);

            var questionnaireBrowseItemStorage = new Mock<IPlainStorageAccessor<QuestionnaireBrowseItem>>();

            questionnaireBrowseItemStorage
                .Setup(reader => reader.Query(It.IsAny<Func<IQueryable<QuestionnaireBrowseItem>, List<QuestionnaireBrowseItem>>>()))
                .Returns<Func<IQueryable<QuestionnaireBrowseItem>, List<QuestionnaireBrowseItem>>>(query => query.Invoke(new[]
                {
                      Create.Entity.QuestionnaireBrowseItem(title: title, questionnaireId: differentQuestionnaireId),
                }.AsQueryable()));
            validator = Create.Service.QuestionnaireNameValidator(questionnaireBrowseItemStorage: questionnaireBrowseItemStorage.Object);

            var exception = Assert.Throws<QuestionnaireException>(() =>
               validator.Validate(null, command));

            exception.Message.Should().Contain(title);
        }

        private static QuestionnaireValidator validator;
        private static ImportFromDesigner command;
        private static string title = "The Title";
        private static Guid importedQuestionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static Guid differentQuestionnaireId = Guid.Parse("22222222222222222222222222222222");
    }
}
