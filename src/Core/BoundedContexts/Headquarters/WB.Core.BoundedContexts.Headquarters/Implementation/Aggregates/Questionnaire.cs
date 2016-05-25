using System;
using System.Linq;
using System.Text.RegularExpressions;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.Commands;
using WB.Core.BoundedContexts.Headquarters.EventHandler.WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.BoundedContexts.Supervisor.Factories;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Accessors;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Aggregates
{
    public class Questionnaire : IPlainAggregateRoot
    {
        private const int MaxTitleLength = 500;
        private static readonly Regex InvalidTitleRegex = new Regex(@"^[\w \-\(\)\\/]*$", RegexOptions.Compiled);

        private readonly IPlainQuestionnaireRepository plainQuestionnaireRepository;
        private readonly IQuestionnaireAssemblyFileAccessor questionnaireAssemblyFileAccessor;

        private readonly IReferenceInfoForLinkedQuestionsFactory referenceInfoForLinkedQuestionsFactory;
        private readonly IQuestionnaireRosterStructureFactory questionnaireRosterStructureFactory;

        private readonly IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemStorage;
        private readonly IPlainKeyValueStorage<ReferenceInfoForLinkedQuestions> referenceInfoForLinkedQuestionsStorage;
        private readonly IPlainKeyValueStorage<QuestionnaireRosterStructure> questionnaireRosterStructureStorage;
        private readonly IPlainKeyValueStorage<QuestionnaireQuestionsInfo> questionnaireQuestionsInfoStorage;
        private readonly IFileSystemAccessor fileSystemAccessor;

        private Guid Id { get; set; }

        public Questionnaire(
            IPlainQuestionnaireRepository plainQuestionnaireRepository, 
            IQuestionnaireAssemblyFileAccessor questionnaireAssemblyFileAccessor, 
            IReferenceInfoForLinkedQuestionsFactory referenceInfoForLinkedQuestionsFactory, 
            IQuestionnaireRosterStructureFactory questionnaireRosterStructureFactory,
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemStorage,
            IPlainKeyValueStorage<ReferenceInfoForLinkedQuestions> referenceInfoForLinkedQuestionsStorage,
            IPlainKeyValueStorage<QuestionnaireRosterStructure> questionnaireRosterStructureStorage,
            IPlainKeyValueStorage<QuestionnaireQuestionsInfo> questionnaireQuestionsInfoStorage,
            IFileSystemAccessor fileSystemAccessor)
        {
            this.plainQuestionnaireRepository = plainQuestionnaireRepository;
            this.questionnaireAssemblyFileAccessor = questionnaireAssemblyFileAccessor;
            this.referenceInfoForLinkedQuestionsFactory = referenceInfoForLinkedQuestionsFactory;
            this.questionnaireRosterStructureFactory = questionnaireRosterStructureFactory;
            this.questionnaireBrowseItemStorage = questionnaireBrowseItemStorage;
            this.referenceInfoForLinkedQuestionsStorage = referenceInfoForLinkedQuestionsStorage;
            this.questionnaireRosterStructureStorage = questionnaireRosterStructureStorage;
            this.questionnaireQuestionsInfoStorage = questionnaireQuestionsInfoStorage;
            this.fileSystemAccessor = fileSystemAccessor;
        }

        public void SetId(Guid id) => this.Id = id;

        public void ImportFromDesigner(ImportFromDesigner command)
        {
            QuestionnaireDocument questionnaireDocument = CastToQuestionnaireDocumentOrThrow(command.Source);

            questionnaireDocument.ConnectChildrenWithParent();

            if (string.IsNullOrWhiteSpace(command.SupportingAssembly))
                throw new QuestionnaireException(
                    $"Cannot import questionnaire. Assembly file is empty. QuestionnaireId: {this.Id}");

            this.StoreQuestionnaireAndProjectionsAsNewVersion(
                questionnaireDocument, command.SupportingAssembly,
                command.AllowCensusMode, command.QuestionnaireContentVersion);
        }

        public void CloneQuestionnaire(CloneQuestionnaire command)
        {
            this.ThrowIfQuestionnaireIsAbsentOrDisabled(command.QuestionnaireId, command.QuestionnaireVersion);

            QuestionnaireDocument questionnaireDocument = this.plainQuestionnaireRepository.GetQuestionnaireDocument(command.QuestionnaireId, command.QuestionnaireVersion);

            this.ThrowIfTitleIsInvalid(command.NewTitle, questionnaireDocument);

            string assemblyAsBase64 = this.questionnaireAssemblyFileAccessor.GetAssemblyAsBase64String(command.QuestionnaireId, command.QuestionnaireVersion);
            QuestionnaireBrowseItem questionnaireBrowseItem = this.GetQuestionnaireBrowseItem(command.QuestionnaireId, command.QuestionnaireVersion);

            questionnaireDocument.Title = command.NewTitle;

            this.StoreQuestionnaireAndProjectionsAsNewVersion(
                questionnaireDocument, assemblyAsBase64, questionnaireBrowseItem.AllowCensusMode, questionnaireBrowseItem.QuestionnaireContentVersion);
        }

        private void StoreQuestionnaireAndProjectionsAsNewVersion(
            QuestionnaireDocument questionnaireDocument, string assemblyAsBase64,
            bool isCensus, long questionnaireContentVersion)
        {
            var identity = new QuestionnaireIdentity(this.Id, this.GetNextVersion());

            this.plainQuestionnaireRepository.StoreQuestionnaire(identity.QuestionnaireId, identity.Version, questionnaireDocument);
            this.questionnaireAssemblyFileAccessor.StoreAssembly(identity.QuestionnaireId, identity.Version, assemblyAsBase64);

            string projectionId = GetProjectionId(identity);

            this.questionnaireBrowseItemStorage.Store(
                new QuestionnaireBrowseItem(questionnaireDocument, identity.Version, isCensus, questionnaireContentVersion),
                projectionId);

            this.referenceInfoForLinkedQuestionsStorage.Store(
                this.referenceInfoForLinkedQuestionsFactory.CreateReferenceInfoForLinkedQuestions(questionnaireDocument, identity.Version),
                projectionId);

            this.questionnaireRosterStructureStorage.Store(
                this.questionnaireRosterStructureFactory.CreateQuestionnaireRosterStructure(questionnaireDocument, identity.Version),
                projectionId);

            this.questionnaireQuestionsInfoStorage.Store(
                new QuestionnaireQuestionsInfo
                {
                    QuestionIdToVariableMap = questionnaireDocument
                        .Find<IQuestion>()
                        .ToDictionary(x => x.PublicKey, x => x.StataExportCaption)
                },
                projectionId);
        }

        public void DisableQuestionnaire(DisableQuestionnaire command)
        {
            this.ThrowIfQuestionnaireIsAbsentOrDisabled(command.QuestionnaireId, command.QuestionnaireVersion);

            var browseItem = this.questionnaireBrowseItemStorage.GetById(new QuestionnaireIdentity(this.Id, command.QuestionnaireVersion).ToString());
            if (browseItem != null)
            {
                browseItem.Disabled = true;
                this.questionnaireBrowseItemStorage.Store(browseItem, browseItem.Id);
            }
        }

        public void DeleteQuestionnaire(DeleteQuestionnaire command)
        {
            this.ThrowIfQuestionnaireIsAbsentOrNotDisabled(command.QuestionnaireId, command.QuestionnaireVersion);

            var browseItem = this.questionnaireBrowseItemStorage.GetById(new QuestionnaireIdentity(this.Id, command.QuestionnaireVersion).ToString());
            if (browseItem != null)
            {
                browseItem.IsDeleted = true;
                this.questionnaireBrowseItemStorage.Store(browseItem, browseItem.Id);
            }
        }

        public void RegisterPlainQuestionnaire(RegisterPlainQuestionnaire command)
        {
            QuestionnaireDocument questionnaireDocument = this.plainQuestionnaireRepository.GetQuestionnaireDocument(command.Id, command.Version);
            
            if (questionnaireDocument == null || questionnaireDocument.IsDeleted)
                throw new QuestionnaireException(string.Format(
                    "Plain questionnaire {0} ver {1} cannot be registered because it is absent in plain repository.",
                    this.Id, command.Version));

            throw new NotSupportedException("This command is no longer supported and should be reimplemented if we decide to resurrect Supervisor");
        }

        private long GetNextVersion()
        {
            var availableVersions =
                this.questionnaireBrowseItemStorage.Query(
                    _ => _.Where(q => q.QuestionnaireId == this.Id).Select(q => q.Version));

            if (!availableVersions.Any())
                return 1;

            return availableVersions.Max() + 1;
        }

        private static QuestionnaireDocument CastToQuestionnaireDocumentOrThrow(IQuestionnaireDocument source)
        {
            var document = source as QuestionnaireDocument;

            if (document == null)
                throw new QuestionnaireException(
                    $"Cannot import questionnaire with a document of a not supported type {source.GetType()}. QuestionnaireId: {source.PublicKey}");

            return document;
        }

        private void ThrowIfTitleIsInvalid(string title, QuestionnaireDocument questionnaireDocument)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new QuestionnaireException("Questionnaire title should not be empty.");

            if (title.Length > MaxTitleLength)
                throw new QuestionnaireException($"Questionnaire title can't have more than {MaxTitleLength} symbols.");

            if (!InvalidTitleRegex.IsMatch(title))
                throw new QuestionnaireException("Questionnaire title contains characters that are not allowed. Only letters, numbers, space and _ are allowed.");

            IGroup rosterWithNameEqualToQuestionnaireTitle = questionnaireDocument.Find<IGroup>(
                group => this.IsRosterWithNameEqualToQuestionnaireTitle(@group, title)).FirstOrDefault();

            if (rosterWithNameEqualToQuestionnaireTitle != null)
                throw new QuestionnaireException($"Questionnaire title is similar to roster ID '{rosterWithNameEqualToQuestionnaireTitle.VariableName}'.");
        }

        private bool IsRosterWithNameEqualToQuestionnaireTitle(IGroup group, string title)
        {
            if (!group.IsRoster)
                return false;

            var questionnaireVariableName = this.fileSystemAccessor.MakeValidFileName(title);

            return group.VariableName.Equals(questionnaireVariableName, StringComparison.InvariantCultureIgnoreCase);
        }

        private void ThrowIfQuestionnaireIsAbsentOrDisabled(Guid questionnaireId, long questionnaireVersion)
        {
            QuestionnaireBrowseItem questionnaireBrowseItem = this.GetQuestionnaireBrowseItemOrThrow(questionnaireId, questionnaireVersion);

            if (questionnaireBrowseItem.Disabled)
                throw new QuestionnaireException(
                    $"Questionnaire {questionnaireId.FormatGuid()} ver {questionnaireVersion} is disabled and probably is being deleted.");
        }

        private void ThrowIfQuestionnaireIsAbsentOrNotDisabled(Guid questionnaireId, long questionnaireVersion)
        {
            QuestionnaireBrowseItem questionnaireBrowseItem = this.GetQuestionnaireBrowseItemOrThrow(questionnaireId, questionnaireVersion);

            if (!questionnaireBrowseItem.Disabled)
                throw new QuestionnaireException(
                    $"Questionnaire {this.Id.FormatGuid()} ver {questionnaireVersion} is not disabled.");
        }

        private QuestionnaireBrowseItem GetQuestionnaireBrowseItemOrThrow(Guid questionnaireId, long questionnaireVersion)
        {
            QuestionnaireBrowseItem questionnaireBrowseItem = this.GetQuestionnaireBrowseItem(questionnaireId, questionnaireVersion);

            if (questionnaireBrowseItem == null)
                throw new QuestionnaireException(
                    $"Questionnaire {questionnaireId.FormatGuid()} ver {questionnaireVersion} is absent in repository.");

            return questionnaireBrowseItem;
        }

        private QuestionnaireBrowseItem GetQuestionnaireBrowseItem(Guid questionnaireId, long questionnaireVersion)
        {
            string projectionId = GetProjectionId(new QuestionnaireIdentity(questionnaireId, questionnaireVersion));

            var questionnaire = this.questionnaireBrowseItemStorage.GetById(projectionId);
            return questionnaire;
        }

        private static string GetProjectionId(QuestionnaireIdentity identity) => identity.ToString();
    }
}