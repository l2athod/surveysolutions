using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.BoundedContexts.Headquarters.EventHandler;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.ChangeStatus;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    public class InterviewDetailsViewFactory : IInterviewDetailsViewFactory
    {
        private readonly IUserViewFactory userStore;
        private readonly IChangeStatusFactory changeStatusFactory;
        private readonly IInterviewPackagesService incomingSyncPackagesQueue;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaryRepository;
        private readonly IReadSideKeyValueStorage<InterviewData> interviewDataRepository;
        private readonly ISubstitutionService substitutionService;
        private readonly IStatefulInterviewRepository statefulInterviewRepository;

        private class ValidationView
        {
            public string Message { get; set; }
            public int FailedValidationIndex { get; set; }
        }

        public InterviewDetailsViewFactory(
            IUserViewFactory userStore,
            IChangeStatusFactory changeStatusFactory,
            IInterviewPackagesService incomingSyncPackagesQueue,
            IQuestionnaireStorage questionnaireStorage,
            IStatefulInterviewRepository statefulInterviewRepository,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaryRepository,
            IReadSideKeyValueStorage<InterviewData>  interviewDataRepository,
            ISubstitutionService substitutionService)
        {
            this.userStore = userStore;
            this.changeStatusFactory = changeStatusFactory;
            this.incomingSyncPackagesQueue = incomingSyncPackagesQueue;
            this.questionnaireStorage = questionnaireStorage;
            this.interviewSummaryRepository = interviewSummaryRepository;
            this.interviewDataRepository = interviewDataRepository;
            this.substitutionService = substitutionService;
            this.statefulInterviewRepository = statefulInterviewRepository;
        }

        public DetailsViewModel GetInterviewDetails(Guid interviewId, InterviewDetailsFilter questionsTypes, Identity currentGroupIdentity)
        {
            var interview = this.statefulInterviewRepository.Get(interviewId.FormatGuid());
            InterviewSummary interviewSummary = this.interviewSummaryRepository.GetById(interviewId);
            var interviewData = this.interviewDataRepository.GetById(interviewId);

            var questionnaireIdentity = new QuestionnaireIdentity(interviewSummary.QuestionnaireId, interviewSummary.QuestionnaireVersion);
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, interview.Language);
            var questionnaireDocument = this.questionnaireStorage.GetQuestionnaireDocument(questionnaireIdentity);

            var responsible = this.userStore.GetUser(new UserViewInputModel(interviewSummary.ResponsibleId));

            var rootNode = new InterviewGroupView(Identity.Create(questionnaire.QuestionnaireId, RosterVector.Empty))
            {
                Title = questionnaire.Title
            };
            var interviewGroupViews = rootNode.ToEnumerable()
                                              .Concat(interview.GetAllGroupsAndRosters().Select(this.ToGroupView)).ToList();

            var interviewEntityViews = this.GetFilteredEntities(interview, interviewData, questionnaire, questionnaireDocument, currentGroupIdentity, questionsTypes);
            if (questionsTypes != InterviewDetailsFilter.All)
                interviewEntityViews = this.GetEntitiesWithoutEmptyGroupsAndRosters(interviewEntityViews);

            return new DetailsViewModel
            {
                QuestionsTypes = questionsTypes,
                SelectedGroupId = currentGroupIdentity,
                FilteredEntities = interviewEntityViews,
                InterviewDetails = new InterviewDetailsView
                {
                    Groups = interviewGroupViews,
                    Responsible = new UserLight(interviewSummary.ResponsibleId, responsible?.UserName ?? "<UNKNOWN>"),
                    Title = questionnaire.Title,
                    Description = questionnaireDocument.Description,
                    PublicKey = interviewSummary.InterviewId,
                    Status = interview.Status,
                    ReceivedByInterviewer = interviewSummary.ReceivedByInterviewer,
                    CurrentTranslation = interview.Language,
                    IsAssignedToInterviewer = interviewSummary.IsAssignedToInterviewer
                },
                Statistic = new DetailsStatisticView
                {
                    AnsweredCount = interview.CountAllEnabledAnsweredQuestions(),
                    AllCount = interview.CountAllEnabledQuestions(),
                    CommentedCount = interview.GetAllCommentedEnabledQuestions().Count(),
                    EnabledCount = interview.CountAllEnabledQuestions(),
                    FlaggedCount = interviewData.Levels.Sum(lvl => lvl.Value.QuestionsSearchCache.Values.Count(q => q.IsFlagged())),
                    InvalidCount = interview.CountAllInvalidEntities(),
                    SupervisorsCount = interview.CountEnabledSupervisorQuestions(),
                    HiddenCount = interview.CountEnabledHiddenQuestions(),
                },
                History = this.changeStatusFactory.Load(new ChangeStatusInputModel {InterviewId = interviewId}),
                HasUnprocessedSyncPackages = this.incomingSyncPackagesQueue.HasPendingPackageByInterview(interviewId),
                Translations = questionnaire.GetTranslationLanguages().Select(ToTranslationView).ToReadOnlyCollection(),
                InterviewKey = interviewSummary.Key,
                QuestionnaireName = questionnaire.Title,
                QuestionnaireVersion = interviewSummary.QuestionnaireVersion
            };
        }

        private IEnumerable<InterviewEntityView> GetEntitiesWithoutEmptyGroupsAndRosters(IEnumerable<InterviewEntityView> interviewEntityViews)
        {
            var allEntities = interviewEntityViews.ToList();
            var parentsOfQuestions = allEntities.OfType<InterviewQuestionView>().Select(x => x.ParentId).ToHashSet();
            var parentsOfStaticTexts = allEntities.OfType<InterviewStaticTextView>().Select(x => x.ParentId).ToHashSet();

            foreach (var interviewEntityView in allEntities)
            {
                var groupView = interviewEntityView as InterviewGroupView;
                if (groupView == null)
                    yield return interviewEntityView;
                else if (parentsOfQuestions.Contains(groupView.Id) || parentsOfStaticTexts.Contains(groupView.Id))
                    yield return groupView;
            }
        }

        private IEnumerable<InterviewEntityView> GetFilteredEntities(IStatefulInterview interview,
            InterviewData interviewData, IQuestionnaire questionnaire, QuestionnaireDocument questionnaireDocument, Identity currentGroupIdentity,
            InterviewDetailsFilter questionsTypes)
        {
            var groupEntities = currentGroupIdentity == null || currentGroupIdentity.Id == questionnaire.QuestionnaireId
                ? interview.GetAllSections()
                : (interview.GetGroup(currentGroupIdentity) as IInterviewTreeNode).ToEnumerable();

            foreach (var entity in this.GetQuestionsFirstAndGroupsAfterFrom(groupEntities))
            {
                if (!IsEntityInFilter(questionsTypes, entity, interviewData)) continue;

                var question = entity as InterviewTreeQuestion;
                var group = entity as InterviewTreeGroup;
                var staticText = entity as InterviewTreeStaticText;

                if (question != null) yield return this.ToQuestionView(question, questionnaire, questionnaireDocument, interview, interviewData);
                else if (group != null) yield return this.ToGroupView(group);
                else if (staticText != null) yield return this.ToStaticTextView(interview, staticText, questionnaire, questionnaireDocument);
            }
        }

        private IEnumerable<IInterviewTreeNode> GetQuestionsFirstAndGroupsAfterFrom(IEnumerable<IInterviewTreeNode> groups)
        {
            var itemsQueue = new Stack<IInterviewTreeNode>(groups.Reverse());

            while (itemsQueue.Count > 0)
            {
                var currentItem = itemsQueue.Pop();

                yield return currentItem;

                IEnumerable<IInterviewTreeNode> childItems = currentItem.Children;

                if (childItems != null)
                {
                    var reverseChildItems = childItems.Reverse().ToList();
                    var childItemsIncOrrectOrder = 
                        reverseChildItems.Where(child =>  child is InterviewTreeGroup)
                        .Concat(reverseChildItems.Where(child => !(child is InterviewTreeGroup)));

                    foreach (var childItem in childItemsIncOrrectOrder)
                    {
                        itemsQueue.Push(childItem);
                    }
                }
            }
        }

        private static InterviewAttachmentViewModel ToAttachmentView(IQuestionnaire questionnaire, Guid staticTextId)
        {
            var attachment = questionnaire.GetAttachmentForEntity(staticTextId);
            
            if (attachment == null) return null;

            return new InterviewAttachmentViewModel
            {
                ContentId = attachment.ContentId,
                ContentName = attachment.Name
            };
        }

        private InterviewEntityView ToQuestionView(InterviewTreeQuestion interviewQuestion, IQuestionnaire questionnaire, 
            QuestionnaireDocument questionnaireDocument, IStatefulInterview interview, InterviewData interviewData)
        {
            var questionnaireQuestion = questionnaireDocument.FirstOrDefault<IQuestion>(q => q.PublicKey == interviewQuestion.Identity.Id);
            
            return new InterviewQuestionView
            {
                Id = interviewQuestion.Identity,
                ParentId = interviewQuestion.Parent.Identity,
                Title = interviewQuestion.Title.BrowserReadyText,
                IsAnswered = interviewQuestion.IsAnswered(),
                IsValid = interviewQuestion.IsValid,
                AnswerString = GetAnswerAsString(interviewQuestion, questionnaire), 
                QuestionType = questionnaire.GetQuestionType(interviewQuestion.Identity.Id),
                IsFeatured = interviewQuestion.IsPrefilled,
                LinkedToQuestionId = questionnaire.IsQuestionLinked(interviewQuestion.Identity.Id) ? questionnaire.GetQuestionReferencedByLinkedQuestion(interviewQuestion.Identity.Id) : (Guid?)null,
                LinkedToRosterId = questionnaire.IsQuestionLinkedToRoster(interviewQuestion.Identity.Id) ? questionnaire.GetRosterReferencedByLinkedQuestion(interviewQuestion.Identity.Id) : (Guid?)null,
                Scope = questionnaire.GetQuestionScope(interviewQuestion.Identity.Id),
                Variable = interviewQuestion.VariableName,
                Settings = ToQuestionSettingsView(questionnaireQuestion),
                Comments = interviewQuestion.AnswerComments.Select(ToCommentView).ToList(),
                IsEnabled = !interviewQuestion.IsDisabled(),
                IsReadOnly = !(interviewQuestion.IsSupervisors && interview.Status < InterviewStatus.ApprovedByHeadquarters),
                Options = ToOptionsView(interviewQuestion, interview),
                Answer = ToAnswerView(interviewQuestion),
                IsFlagged = GetIsFlagged(interviewQuestion, interviewData),
                FailedValidationMessages = GetFailedValidationMessages(
                    interviewQuestion.FailedValidations?.Select(
                        (x, index) => ToValidationView(interviewQuestion.ValidationMessages, x, index)),
                    questionnaireQuestion.ValidationConditions).ToList()
            };
        }

        private string GetAnswerAsString(InterviewTreeQuestion interviewQuestion, IQuestionnaire questionnaire)
        {
            if (!interviewQuestion.IsAnswered())
                return string.Empty;

            if (interviewQuestion.IsInteger)
            {
                var integerValue = interviewQuestion.GetAsIntegerAnswer().Value;
                return questionnaire.ShouldUseFormatting(interviewQuestion.Identity.Id)
                    ? integerValue.ToString("N0", CultureInfo.InvariantCulture)
                    : integerValue.ToString(CultureInfo.InvariantCulture);
            }

            if (interviewQuestion.IsDouble)
            {
                var doubleValue = interviewQuestion.GetAsDoubleAnswer().Value;
                return questionnaire.ShouldUseFormatting(interviewQuestion.Identity.Id)
                    ? $"{doubleValue:0,0.#################}"
                    : doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            return interviewQuestion.GetAnswerAsString();
        }

        private static bool GetIsFlagged(InterviewTreeQuestion interviewQuestion, InterviewData interviewData)
        {
            var levelId = InterviewEventHandlerFunctional.CreateLevelIdFromPropagationVector(
                    interviewQuestion.Identity.RosterVector);

            if (!interviewData.Levels.ContainsKey(levelId)) return false;
            if (!interviewData.Levels[levelId].QuestionsSearchCache.ContainsKey(interviewQuestion.Identity.Id)) return false;

            return interviewData.Levels[levelId].QuestionsSearchCache[interviewQuestion.Identity.Id].IsFlagged();
        }

        private static object ToAnswerView(InterviewTreeQuestion interviewQuestion)
        {
            if (!interviewQuestion.IsAnswered()) return null;

            if (interviewQuestion.IsYesNo)
                return interviewQuestion.GetAsYesNoAnswer().ToAnsweredYesNoOptions().ToArray();

            if (interviewQuestion.IsMultiFixedOption)
                return interviewQuestion.GetAsMultiFixedOptionAnswer().ToDecimals().ToArray();

            if (interviewQuestion.IsMultiLinkedOption)
                return interviewQuestion.GetAsMultiLinkedOptionAnswer().ToRosterVectorArray();

            if (interviewQuestion.IsSingleFixedOption)
                return interviewQuestion.GetAsSingleFixedOptionAnswer().SelectedValue;

            if (interviewQuestion.IsSingleLinkedOption)
                return interviewQuestion.GetAsSingleLinkedOptionAnswer().SelectedValue;

            if (interviewQuestion.IsGps)
                return interviewQuestion.GetAsGpsAnswer().Value;

            if (interviewQuestion.IsText)
                return interviewQuestion.GetAsTextAnswer().Value;

            if (interviewQuestion.IsInteger)
                return interviewQuestion.GetAsIntegerAnswer().Value;

            if (interviewQuestion.IsDouble)
                return interviewQuestion.GetAsDoubleAnswer().Value;

            if (interviewQuestion.IsArea)
                return interviewQuestion.GetAsAreaAnswer().Value;

            if (interviewQuestion.IsAudio)
                return interviewQuestion.GetAsAudioAnswer().FileName;

            return null;
        }

        private List<QuestionOptionView> ToOptionsView(InterviewTreeQuestion interviewQuestion, IStatefulInterview interview)
        {
            if (interviewQuestion.IsSingleFixedOption || interviewQuestion.IsMultiFixedOption)
            {
                var options = interview.GetTopFilteredOptionsForQuestion(interviewQuestion.Identity, null, null, int.MaxValue)?.Select(a => new QuestionOptionView
                              {
                                  Value = a.Value,
                                  Label = a.Title
                              })?.ToList() ?? new List<QuestionOptionView>(); ;

                var optionsToMarkAsSelected = new List<int>();
                if (interviewQuestion.IsSingleFixedOption && interviewQuestion.IsAnswered())
                    optionsToMarkAsSelected.Add(interviewQuestion.GetAsSingleFixedOptionAnswer().SelectedValue);

                if (interviewQuestion.IsMultiFixedOption && interviewQuestion.IsAnswered())
                    optionsToMarkAsSelected.AddRange(interviewQuestion.GetAsMultiFixedOptionAnswer().CheckedValues);

                foreach (var selectedValue in optionsToMarkAsSelected)
                {
                    var selectedOption = options.FirstOrDefault(x => (int)x.Value == selectedValue);
                    if (selectedOption == null) continue;
                    selectedOption.IsChecked = true;
                    selectedOption.Index = optionsToMarkAsSelected.IndexOf(selectedValue) + 1;
                }

                return options;
            }

            if (interviewQuestion.IsLinked)
            {
                var optionsToMarkAsSelected = new List<RosterVector>();
                if (interviewQuestion.IsSingleLinkedOption && interviewQuestion.IsAnswered())
                {
                    optionsToMarkAsSelected.Add(interviewQuestion.GetAsSingleLinkedOptionAnswer().SelectedValue);
                }
                if (interviewQuestion.IsMultiLinkedOption && interviewQuestion.IsAnswered())
                {
                    optionsToMarkAsSelected.AddRange(interviewQuestion.GetAsMultiLinkedOptionAnswer().CheckedValues);
                }

                var options = interviewQuestion.AsLinked.Options.Select(x => new QuestionOptionView
                {
                    Value = x,
                    Label = interview.GetLinkedOptionTitle(interviewQuestion.Identity, x),
                    IsChecked = optionsToMarkAsSelected.Contains(x),
                    Index = optionsToMarkAsSelected.IndexOf(x) + 1
                }).ToList();

                return options;
            }

            if (interviewQuestion.IsLinkedToListQuestion)
            {
                var optionsToMarkAsSelected = new List<int>();
                if (interviewQuestion.IsSingleLinkedToList && interviewQuestion.IsAnswered())
                {
                    optionsToMarkAsSelected.Add(interviewQuestion.GetAsSingleLinkedToListAnswer().SelectedValue);
                }
                if (interviewQuestion.IsMultiLinkedToList && interviewQuestion.IsAnswered())
                {
                    optionsToMarkAsSelected.AddRange(interviewQuestion.GetAsMultiLinkedToListAnswer().CheckedValues);
                }
                var listQuestion = interview.FindQuestionInQuestionBranch(interviewQuestion.AsLinkedToList.LinkedSourceId, interviewQuestion.Identity);

                var options = interviewQuestion.AsLinkedToList.Options.Select(x => new QuestionOptionView
                {
                    Value = Convert.ToInt32(x),
                    Label = ((InterviewTreeTextListQuestion)listQuestion.InterviewQuestion).GetTitleByItemCode(x),
                    IsChecked = optionsToMarkAsSelected.Contains(Convert.ToInt32(x)),
                    Index = optionsToMarkAsSelected.IndexOf(Convert.ToInt32(x)) + 1
                }).ToList();

                return options;
            }

            if (interviewQuestion.IsTextList && interviewQuestion.IsAnswered())
            {
                return interviewQuestion.GetAsTextListAnswer().Rows.Select(x => new QuestionOptionView
                {
                    Value = Convert.ToInt32(x.Value),
                    Label = x.Text
                }).ToList();
            }

            if (interviewQuestion.IsYesNo)
            {
                var options = interview.GetTopFilteredOptionsForQuestion(interviewQuestion.Identity, null, null, 200)?.Select(a => new QuestionOptionView
                {
                    Value = a.Value,
                    Label = a.Title
                })?.ToList() ?? new List<QuestionOptionView>();

                return options;
            }

            return new List<QuestionOptionView>();
        }

        private dynamic ToQuestionSettingsView(IQuestion question)
        {
            var numericQuestion = question as INumericQuestion;
            if (numericQuestion != null)
            {
                return new NumericQuestionSettings
                {
                    IsInteger = numericQuestion.IsInteger,
                    CountOfDecimalPlaces = numericQuestion.CountOfDecimalPlaces,
                    UseFormating = numericQuestion.UseFormatting
                };
            }

            var categoricalMultiQuestion = question as MultyOptionsQuestion;
            if (categoricalMultiQuestion != null)
            {
                return new MultiQuestionSettings
                {
                    YesNoQuestion = categoricalMultiQuestion.YesNoView,
                    AreAnswersOrdered = categoricalMultiQuestion.AreAnswersOrdered,
                    MaxAllowedAnswers = categoricalMultiQuestion.MaxAllowedAnswers,
                    IsLinkedToRoster = categoricalMultiQuestion.LinkedToRosterId.HasValue
                };
            }

            var categoricalSingleQuestion = question as SingleQuestion;
            if (categoricalSingleQuestion != null)
            {
                return new SingleQuestionSettings
                {
                    IsFilteredCombobox = categoricalSingleQuestion.IsFilteredCombobox ?? false,
                    IsCascade = categoricalSingleQuestion.CascadeFromQuestionId.HasValue,
                    IsLinkedToRoster = categoricalSingleQuestion.LinkedToRosterId.HasValue,
                    
                };
            }

            var textQuestion = question as TextQuestion;
            if (textQuestion != null)
            {
               return new TextQuestionSettings
                {
                    Mask = textQuestion.Mask
                };
            }

            var dateTimeQuestion = question as DateTimeQuestion;
            if (dateTimeQuestion != null)
            {
                return new DateTimeQuestionSettings
                {
                    IsTimestamp = question.IsTimestamp
                };
            }

            return null;
        }

        private InterviewQuestionCommentView ToCommentView(AnswerComment comment) => new InterviewQuestionCommentView
        {
            Text = comment.Comment,
            CommenterId = comment.UserId,
            CommenterRole = comment.UserRole,
            CommenterName = this.userStore.GetUser(new UserViewInputModel(comment.UserId))?.UserName ?? "<UNKNOWN>",
            Date = comment.CommentTime
        };

        private InterviewEntityView ToStaticTextView(IStatefulInterview interview, InterviewTreeStaticText interviewStaticText, IQuestionnaire questionnaire,
            QuestionnaireDocument questionnaireDocument)
        {
            var attachment = ToAttachmentView(questionnaire, interviewStaticText.Identity.Id);
            var questionnaireStaticText = questionnaireDocument.FirstOrDefault<StaticText>(st => st.PublicKey == interviewStaticText.Identity.Id);

            return new InterviewStaticTextView
            {
                Id = interviewStaticText.Identity,
                ParentId = interviewStaticText.Parent.Identity,
                Text = interviewStaticText.Title.Text,
                IsEnabled = !interviewStaticText.IsDisabled(),
                IsValid = interviewStaticText.IsValid,
                FailedValidationMessages = GetFailedValidationMessages(
                    interviewStaticText.FailedValidations?.Select(
                        (x, index) => ToValidationView(interviewStaticText.ValidationMessages, x, index)),
                    questionnaireStaticText.ValidationConditions).ToList(),
                Attachment = attachment
            };
        }

        private static ValidationView ToValidationView(SubstitutionText[] validationMessages,
            FailedValidationCondition validationCondition, int failedValidationIndex)
            => new ValidationView
            {
                FailedValidationIndex = validationCondition.FailedConditionIndex,
                Message = validationMessages[failedValidationIndex].Text
            };

        private static IEnumerable<ValidationCondition> GetFailedValidationMessages(
            IEnumerable<ValidationView> interviewValidations, IList<ValidationCondition> questionnaireValidations)
        {
            if (interviewValidations == null) yield break;

            foreach (var failedValidation in interviewValidations)
            {
                var validationExpression =
                    questionnaireValidations[failedValidation.FailedValidationIndex].Expression;
                var validationMessage = failedValidation.Message;

                yield return new ValidationCondition(validationExpression, validationMessage);
            }
        }

        private static InterviewTranslationView ToTranslationView(string translation)
            => new InterviewTranslationView { /*Id = translation.Id,*/ Name = translation };

        private InterviewGroupView ToGroupView(InterviewTreeGroup group) => new InterviewGroupView
        {
                Id = group.Identity,
                Title = this.ToGroupTitleView(@group),
                Depth = group.Parents?.Count() + 1 ?? 1
        };

        private string ToGroupTitleView(InterviewTreeGroup group)
        {
            var roster = group as InterviewTreeRoster;

            return roster != null
                ? $"{roster.Title.Text} - {roster.RosterTitle ?? this.substitutionService.DefaultSubstitutionText}"
                : @group.Title.Text;
        }

        private static bool IsEntityInFilter(InterviewDetailsFilter? filter, IInterviewTreeNode entity, InterviewData interviewData)
        {
            var question = entity as InterviewTreeQuestion;

            if (question != null)
            {
                switch (filter)
                {
                    case InterviewDetailsFilter.Answered:
                        return question.IsAnswered();
                    case InterviewDetailsFilter.Unanswered:
                        return !question.IsDisabled() && !question.IsAnswered();
                    case InterviewDetailsFilter.Commented:
                        return question.AnswerComments?.Any() ?? false;
                    case InterviewDetailsFilter.Enabled:
                        return !question.IsDisabled();
                    case InterviewDetailsFilter.Flagged:
                        return GetIsFlagged(question, interviewData);
                    case InterviewDetailsFilter.Invalid:
                        return !question.IsValid;
                    case InterviewDetailsFilter.Supervisors:
                        return question.IsSupervisors;
                    case InterviewDetailsFilter.Hidden:
                        return question.IsHidden;
                }
            }

            var staticText = entity as InterviewTreeStaticText;
            if (staticText != null)
            {
                switch (filter)
                {
                    case InterviewDetailsFilter.Enabled:
                        return !staticText.IsDisabled();
                    case InterviewDetailsFilter.Invalid:
                        return !staticText.IsValid;
                    case InterviewDetailsFilter.Flagged:
                        return false;
                    case InterviewDetailsFilter.All:
                        return true;
                    default:
                        return false;
                }
            }
            return true;
        }

        public Guid GetFirstChapterId(Guid id)
        {
            var interview = this.statefulInterviewRepository.Get(id.FormatGuid());
            return interview.FirstSection.Identity.Id;
        }
    }
}