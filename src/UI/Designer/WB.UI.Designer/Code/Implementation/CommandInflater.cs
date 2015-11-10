using Main.Core.Documents;
using System;
using System.Web.Security;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.UI.Designer.Extensions;
using WB.UI.Shared.Web.Membership;

namespace WB.UI.Designer.Code.Implementation
{
    public class CommandInflater : ICommandInflater
    {
        private readonly IMembershipUserService userHelper;
        private readonly IReadSideKeyValueStorage<QuestionnaireDocument> questionnaireDocumentReader;

        public CommandInflater(IMembershipUserService userHelper,
            IReadSideKeyValueStorage<QuestionnaireDocument> questionnaireDocumentReader)
        {
            this.userHelper = userHelper;
            this.questionnaireDocumentReader = questionnaireDocumentReader;
        }

        public void PrepareDeserializedCommandForExecution(ICommand command)
        {
            this.SetResponsible(command);
            ValidateAddSharedPersonCommand(command);
            ValidateRemoveSharedPersonCommand(command);

            this.InflateCopyPasteProperties(command);
        }

        private void InflateCopyPasteProperties(ICommand command)
        {
            var currentPasteItemAfterCommand = command as PasteAfterCommand;
            if (currentPasteItemAfterCommand != null)
            {
                currentPasteItemAfterCommand.SourceDocument =
                    GetQuestionnaire(currentPasteItemAfterCommand.SourceQuestionnaireId);
            }

            var currentPasteItemIntoCommand = command as PasteIntoCommand;
            if (currentPasteItemIntoCommand != null)
            {
                currentPasteItemIntoCommand.SourceDocument =
                    GetQuestionnaire(currentPasteItemIntoCommand.SourceQuestionnaireId);
            }
        }

        private QuestionnaireDocument GetQuestionnaire(Guid id)
        {
            var questionnaire = this.questionnaireDocumentReader.GetById(id);

            if (questionnaire == null)
            {
                throw new CommandInflaitingException(CommandInflatingExceptionType.EntityNotFound, "Source questionnaire was not found and might be deleted.");
            }

            if (!questionnaire.IsPublic && (questionnaire.CreatedBy != this.userHelper.WebUser.UserId && !questionnaire.SharedPersons.Contains(this.userHelper.WebUser.UserId)))
            {
                throw new CommandInflaitingException(CommandInflatingExceptionType.Forbidden, "You don't have permissions to access the source questionnaire");
            }

            return questionnaire;
        }

        private void SetResponsible(ICommand command)
        {
            var currentCommand = command as QuestionnaireCommandBase;
            if (currentCommand != null)
            {
                currentCommand.ResponsibleId = this.userHelper.WebUser.UserId;
            }
        }

        private static void ValidateAddSharedPersonCommand(ICommand command)
        {
            var addSharedPersonCommand = command as AddSharedPersonToQuestionnaireCommand;
            if (addSharedPersonCommand != null)
            {
                var sharedPersonUserName = Membership.GetUserNameByEmail(addSharedPersonCommand.Email);

                addSharedPersonCommand.PersonId = Membership.GetUser(sharedPersonUserName).ProviderUserKey.AsGuid();
            }
        }

        private static void ValidateRemoveSharedPersonCommand(ICommand command)
        {
            var removeSharedPersonCommand = command as RemoveSharedPersonFromQuestionnaireCommand;
            if (removeSharedPersonCommand != null)
            {
                var sharedPersonUserName = Membership.GetUserNameByEmail(removeSharedPersonCommand.Email);
                removeSharedPersonCommand.PersonId = Membership.GetUser(sharedPersonUserName).ProviderUserKey.AsGuid();
            }
        }
    }
}