using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Attachments;


namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests.Attachments
{
    internal class when_deleting_attachment_with_premission_to_edit : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = CreateQuestionnaire(questionnaireId: questionnaireId, responsibleId: ownerId);
            questionnaire.AddSharedPerson(sharedPersonId, "email@email.com", ShareType.Edit, ownerId);
            questionnaire.AddOrUpdateAttachment(Create.Command.AddOrUpdateAttachment(questionnaireId, attachmentId, "", ownerId, ""));

            deleteAttachment = Create.Command.DeleteAttachment(questionnaireId, attachmentId, sharedPersonId);

            BecauseOf();
        }


        private void BecauseOf() => questionnaire.DeleteAttachment(deleteAttachment);

        [NUnit.Framework.Test] public void should_doesnt_contains_attachment_with_EntityId_specified () =>
            questionnaire.QuestionnaireDocument.Attachments.FirstOrDefault(a => a.AttachmentId == attachmentId).Should().BeNull();

        private static DeleteAttachment deleteAttachment;
        private static Questionnaire questionnaire;
        private static readonly Guid ownerId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static readonly Guid sharedPersonId = Guid.Parse("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        private static readonly Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static readonly Guid attachmentId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }
}
