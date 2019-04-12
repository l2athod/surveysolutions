using System;
using FluentAssertions;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.Accounts.Membership;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireList;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.Implementation;
using WB.Core.Infrastructure.PlainStorage;
using WB.UI.Designer.Code.Implementation;

using it = Moq.It;

namespace WB.Tests.Unit.Designer.Applications.CommandInflaterTests
{
    internal class when_PasteIntoCommand_is_inflating_with_shared_non_public_questionnaire_but_shared : CommandInflaterTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            var membershipUserService = Mock.Of<IMembershipUserService>(
                _ => _.WebUser == Mock.Of<IMembershipWebUser>(
                    u => u.UserId == actionUserId && u.MembershipUser.Email == actionUserEmail));

            var questionnaire = CreateQuestionnaireDocument(questionnaireId, questionnaiteTitle, ownerId, false);
            var documentStorage = Mock.Of<IPlainKeyValueStorage<QuestionnaireDocument>>(storage
                    => storage.GetById(it.IsAny<string>()) == questionnaire);

            var listStorage = new InMemoryPlainStorageAccessor<QuestionnaireListViewItem>();
            var questionnaireListViewItem = Create.QuestionnaireListViewItem();
            questionnaireListViewItem.SharedPersons.Add(Create.SharedPerson(actionUserId));
            listStorage.Store(questionnaireListViewItem, questionnaireId.FormatGuid());

            command = new PasteInto(questionnaireId, entityId, questionnaireId, pasteAfterId, entityId, actionUserId);

            commandInflater = CreateCommandInflater(membershipUserService, documentStorage, listStorage);
            BecauseOf();
        }

        private void BecauseOf() =>
            commandInflater.PrepareDeserializedCommandForExecution(command, TODO);

        [NUnit.Framework.Test] public void should_not_be_null () =>
           command.SourceDocument.Should().NotBeNull();

        [NUnit.Framework.Test] public void should_questionnarie_id_as_provided () =>
            command.SourceDocument.PublicKey.Should().Be(questionnaireId);

        private static CommandInflater commandInflater;
        private static PasteInto command;
        private static Guid questionnaireId = Guid.Parse("13333333333333333333333333333333");

        private static Guid entityId = Guid.Parse("23333333333333333333333333333333");
        private static Guid pasteAfterId = Guid.Parse("43333333333333333333333333333333");
        
        private static string questionnaiteTitle = "questionnaire title";

        private static Guid actionUserId = Guid.Parse("33333333333333333333333333333333");
        private static string actionUserEmail = "test1@example.com";

        private static Guid ownerId = Guid.Parse("53333333333333333333333333333333");
    }
}
