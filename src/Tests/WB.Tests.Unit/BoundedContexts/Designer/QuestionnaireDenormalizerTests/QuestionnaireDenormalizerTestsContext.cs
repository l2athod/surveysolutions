﻿extern alias designer;

using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.Events.Questionnaire;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Implementation.Factories;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Document;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using TemplateImported = designer::Main.Core.Events.Questionnaire.TemplateImported;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireDenormalizerTests
{
    [Subject(typeof(QuestionnaireDenormalizer))]
    internal class QuestionnaireDenormalizerTestsContext
    {
        protected static IPublishedEvent<T> ToPublishedEvent<T>(T @event)
            where T : class
        {
            return Mock.Of<IPublishedEvent<T>>(publishedEvent
                => publishedEvent.Payload == @event);
        }

        protected static QuestionnaireDenormalizer CreateQuestionnaireDenormalizer(
            IReadSideKeyValueStorage<QuestionnaireDocument> documentStorage = null,
            IQuestionnaireEntityFactory questionnaireEntityFactory = null,
            ILogger logger = null)
        {
            return new QuestionnaireDenormalizer(
                documentStorage ?? Mock.Of<IReadSideKeyValueStorage<QuestionnaireDocument>>(),
                questionnaireEntityFactory ?? Mock.Of<IQuestionnaireEntityFactory>(),
                logger ?? Mock.Of<ILogger>());
        }

        protected static T GetEntityById<T>(QuestionnaireDocument document, Guid entityId)
            where T : class, IComposite
        {
            return document.FirstOrDefault<T>(entity => entity.PublicKey == entityId);
        }

        protected static QuestionnaireDocument CreateQuestionnaireDocument(params IComposite[] children)
        {
            return CreateQuestionnaireDocument(children.AsEnumerable());
        }

        protected static QuestionnaireDocument CreateQuestionnaireDocument(IEnumerable<IComposite> children = null)
        {
            var questionnaire = new QuestionnaireDocument();

            if (children != null)
            {
                questionnaire.Children.AddRange(children);
            }

            return questionnaire;
        }

        protected static Group CreateGroup(Guid? groupId = null, string title = "Group X",
            IEnumerable<IComposite> children = null, Action<Group> setup = null)
        {
            var group = new Group
            {
                PublicKey = groupId ?? Guid.NewGuid(),
                Title = title,
            };

            if (children != null)
            {
                group.Children.AddRange(children);
            }

            if (setup != null)
            {
                setup(group);
            }

            return group;
        }

        protected static QRBarcodeQuestion CreateQRBarcodeQuestion(Guid questionId, string enablementCondition, string instructions, string title, string variableName)
        {
            return new QRBarcodeQuestion
            {
                PublicKey = questionId,
                QuestionText = title,
                QuestionType = QuestionType.QRBarcode,
                ConditionExpression = enablementCondition,
                StataExportCaption = variableName,
                Instructions = instructions
            };
        }

        protected static IQuestion CreateMultimediaQuestion(Guid questionId, string enablementCondition, string instructions, string title, string variableName)
        {
            return new MultimediaQuestion()
            {
                PublicKey = questionId,
                QuestionText = title,
                QuestionType = QuestionType.Multimedia,
                ConditionExpression = enablementCondition,
                StataExportCaption = variableName,
                Instructions = instructions
            };
        }

        protected static TextQuestion CreateTextQuestion(Guid? questionId = null, string title = null)
        {
            return new TextQuestion
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                QuestionText = title,
                QuestionType = QuestionType.Text,
            };
        }

        protected static NumericQuestion CreateNumericQuestion(Guid? questionId = null, string title = null)
        {
            return new NumericQuestion
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                QuestionText = title,
                QuestionType = QuestionType.Numeric
            };
        }


        protected static TextListQuestion CreateTextListQuestion(Guid? questionId = null)
        {
            return new TextListQuestion
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                QuestionType = QuestionType.TextList
            };
        }

        protected static StaticText CreateStaticText(Guid entityId, string text)
        {
            return new StaticText(publicKey: entityId, text: text);
        }

        protected static IPublishedEvent<GroupDeleted> CreateGroupDeletedEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupDeleted
            {
                GroupPublicKey = groupId,
            });
        }

        protected static IPublishedEvent<QuestionDeleted> CreateQuestionDeletedEvent(Guid questionId)
        {
            return ToPublishedEvent(new QuestionDeleted
            {
                QuestionId = questionId,
            });
        }

        protected static IPublishedEvent<NewGroupAdded> CreateNewGroupAddedEvent(Guid groupId,
            string title = "New Group X")
        {
            return ToPublishedEvent(new NewGroupAdded
            {
                PublicKey = groupId,
                GroupText = title,
            });
        }

        protected static IPublishedEvent<GroupCloned> CreateGroupClonedEvent(Guid groupId,
            string title = "New Cloned Group X")
        {
            return ToPublishedEvent(new GroupCloned
            {
                PublicKey = groupId,
                GroupText = title,
            });
        }

        protected static IPublishedEvent<GroupUpdated> CreateGroupUpdatedEvent(Guid groupId,
            string title = "Updated Group Title X")
        {
            return ToPublishedEvent(new GroupUpdated
            {
                GroupPublicKey = groupId,
                GroupText = title,
            });
        }

        protected static IPublishedEvent<NewQuestionAdded> CreateNewQuestionAddedEvent(Guid questionId, Guid? groupId = null, string title = "New Question X")
        {
            return ToPublishedEvent(CreateNewQuestionAdded
            (
                publicKey : questionId,
                groupPublicKey : groupId,
                questionText : title,
                questionType : QuestionType.Numeric
            ));
        }

        protected static IPublishedEvent<QuestionChanged> CreateQuestionChangedEvent(Guid questionId, Guid targetGroupId, string title, QuestionType questionType = QuestionType.Numeric)
        {
            return ToPublishedEvent(CreateQuestionChanged
            (
                publicKey : questionId,
                questionType : questionType,
                questionText : title
            ));
        }

        protected static IPublishedEvent<QuestionnaireItemMoved> CreateQuestionnaireItemMovedEvent(Guid itemId, Guid? targetGroupId)
        {
            return ToPublishedEvent(new QuestionnaireItemMoved
            {
                PublicKey = itemId,
                GroupKey = targetGroupId,
            });
        }

        protected static IPublishedEvent<GroupBecameARoster> CreateGroupBecameARosterEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupBecameARoster(Guid.NewGuid(), groupId));
        }

        protected static IPublishedEvent<GroupStoppedBeingARoster> CreateGroupStoppedBeingARosterEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupStoppedBeingARoster(Guid.NewGuid(), groupId));
        }

        protected static IPublishedEvent<RosterChanged> CreateRosterChangedEvent(Guid groupId, Guid rosterSizeQuestionId, 
            RosterSizeSourceType rosterSizeSource, FixedRosterTitle[] rosterFixedTitles, Guid? rosterTitleQuestionId)
        {
            return
                ToPublishedEvent(new RosterChanged(Guid.NewGuid(), groupId)
                {
                    RosterSizeQuestionId = rosterSizeQuestionId,
                    RosterSizeSource = rosterSizeSource,
                    FixedRosterTitles = rosterFixedTitles,
                    RosterTitleQuestionId = rosterTitleQuestionId
                });
        }

        protected static IPublishedEvent<NumericQuestionAdded> CreateNumericQuestionAddedEvent(
            Guid questionId, Guid? parentGroupId = null)
        {
            return ToPublishedEvent(CreateNumericQuestionAdded
            (
                publicKey : questionId,
                groupPublicKey : parentGroupId ?? Guid.NewGuid()
            ));
        }

        protected static IPublishedEvent<TextListQuestionAdded> CreateTextListQuestionAddedEvent(
            Guid questionId, Guid parentGroupId)
        {
            return ToPublishedEvent(new TextListQuestionAdded
            {
                PublicKey = questionId,
                GroupId = parentGroupId
            });
        }

        protected static IPublishedEvent<TextListQuestionCloned> CreateTextListQuestionClonedEvent(Guid questionId, Guid sourceQuestionId)
        {
            return ToPublishedEvent(new TextListQuestionCloned
            {
                PublicKey = questionId,
                SourceQuestionId = sourceQuestionId
            });
        }

        protected static IPublishedEvent<TextListQuestionChanged> CreateTextListQuestionChangedEvent(Guid questionId)
        {
            return ToPublishedEvent(new TextListQuestionChanged
            {
                PublicKey = questionId
            });
        }
        protected static IPublishedEvent<NumericQuestionChanged> CreateNumericQuestionChangedEvent(
            Guid questionId)
        {
            return ToPublishedEvent(CreateNumericQuestionChanged
            (
                publicKey : questionId
            ));
        }

        protected static IPublishedEvent<NumericQuestionCloned> CreateNumericQuestionClonedEvent(
            Guid questionId, Guid? sourceQuestionId = null, Guid? parentGroupId = null)
        {
            return ToPublishedEvent(CreateNumericQuestionCloned
            (
                publicKey : questionId,
                sourceQuestionId : sourceQuestionId ?? Guid.NewGuid(),
                groupPublicKey : parentGroupId ?? Guid.NewGuid()
            ));
        }

        protected static IPublishedEvent<QuestionCloned> CreateQuestionClonedEvent(
            Guid questionId, QuestionType questionType = QuestionType.Numeric, Guid? sourceQuestionId = null, Guid? parentGroupId = null, int? maxValue = null)
        {
            return ToPublishedEvent(CreateQuestionCloned(
                publicKey : questionId,
                questionType : questionType,
                sourceQuestionId : sourceQuestionId ?? Guid.NewGuid(),
                groupPublicKey : parentGroupId ?? Guid.NewGuid()
            ));
        }

        protected static IPublishedEvent<TemplateImported> CreateTemplateImportedEvent(QuestionnaireDocument questionnaireDocument = null)
        {
            return ToPublishedEvent(new TemplateImported
            {
                Source = questionnaireDocument ?? new QuestionnaireDocument()
            });
        }

        protected static IPublishedEvent<QuestionnaireCloned> CreateQuestionnaireClonedEvent(QuestionnaireDocument questionnaireDocument = null)
        {
            return ToPublishedEvent(new QuestionnaireCloned
            {
                QuestionnaireDocument = questionnaireDocument ?? new QuestionnaireDocument()
            });
        }


        protected static IPublishedEvent<TextListQuestionAdded> CreateTextListQuestionAddedEvent(
            Guid questionId, Guid? parentGroupId = null, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionAdded
            {
                PublicKey = questionId,
                GroupId = parentGroupId ?? Guid.NewGuid(),
                MaxAnswerCount = maxAnswerCount
            });
        }

        protected static IPublishedEvent<TextListQuestionCloned> TextListQuestionClonedEvent(
            Guid questionId, Guid? sourceQuestionId = null, Guid? parentGroupId = null, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionCloned
            {
                PublicKey = questionId,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                GroupId = parentGroupId ?? Guid.NewGuid(),
                MaxAnswerCount = maxAnswerCount
            });
        }

        protected static IPublishedEvent<TextListQuestionChanged> CreateTextListQuestionChangedEvent(
            Guid questionId, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionChanged
            {
                PublicKey = questionId,
                MaxAnswerCount = maxAnswerCount

            });
        }

        protected static IPublishedEvent<StaticTextAdded> CreateStaticTextAddedEvent(Guid entityId, Guid parentId, string text = null)
        {
            return ToPublishedEvent(new StaticTextAdded()
            {
                EntityId = entityId,
                ParentId = parentId,
                Text = text
            });
        }

        protected static IPublishedEvent<StaticTextUpdated> CreateStaticTextUpdatedEvent(Guid entityId, string text = null)
        {
            return ToPublishedEvent(new StaticTextUpdated()
            {
                EntityId = entityId,
                Text = text
            });
        }

        protected static IPublishedEvent<StaticTextCloned> CreateStaticTextClonedEvent(Guid targetEntityId,
            Guid sourceEntityId, Guid parentId, string text = null, int targetIndex = 0)
        {
            return ToPublishedEvent(new StaticTextCloned()
            {
                EntityId = targetEntityId,
                SourceEntityId = sourceEntityId,
                ParentId = parentId,
                Text = text,
                TargetIndex = targetIndex
            });
        }

        protected static IPublishedEvent<StaticTextDeleted> CreateStaticTextDeletedEvent(Guid entityId)
        {
            return ToPublishedEvent(new StaticTextDeleted()
            {
                EntityId = entityId
            });
        }

        public static NewQuestionAdded CreateNewQuestionAdded(Guid publicKey, Guid? groupPublicKey = null, string questionText = null, bool? isInteger = null,
            string stataExportCaption = null, Guid? linkedToQuestionId = null, bool capital = false, string variableLabel = null, string validationExpression = null, string validationMessage = null,
            QuestionScope questionScope = QuestionScope.Interviewer, string instructions = null, Answer[] answers = null, bool featured = false, Guid? responsibleId = null,
            QuestionType questionType = QuestionType.Text, bool? isFilteredCombobox = null, Guid? cascadeFromQuestionId = null, string conditionExpression = null, Order? answerOrder = null)
        {
            return new NewQuestionAdded(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: variableLabel,
                featured: featured,
                questionScope: questionScope,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: capital,
                isInteger: isInteger,
                questionType: questionType,
                answerOrder: answerOrder,
                answers: answers,
                linkedToQuestionId: null,
                areAnswersOrdered: null,
                yesNoView: null,
                maxAllowedAnswers: null,
                mask: null,
                isFilteredCombobox: isFilteredCombobox,
                cascadeFromQuestionId: cascadeFromQuestionId);
        }


        public static QuestionCloned CreateQuestionCloned(Guid publicKey, Guid sourceQuestionId, Guid? groupPublicKey = null, string questionText = null, bool? isInteger = null,
            string stataExportCaption = null, Guid? linkedToQuestionId = null, bool capital = false, string validationExpression = null, string validationMessage = null,
            QuestionScope questionScope = QuestionScope.Interviewer, string instructions = null, Answer[] answers = null, bool featured = false, Guid? responsibleId = null,
            QuestionType questionType = QuestionType.Text, bool? isFilteredCombobox = null, Guid? cascadeFromQuestionId = null, string conditionExpression = null, Order? answerOrder = null,
            Guid? sourceQuestionnaireId = null, int targetIndex = 0, int? maxAnswerCount = null, int? countOfDecimalPlaces = null)
        {
            return new QuestionCloned(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: null,
                featured: featured,
                questionScope: questionScope,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: capital,
                isInteger: isInteger,
                questionType: questionType,
                answerOrder: answerOrder,
                answers: answers,
                linkedToQuestionId: null,
                areAnswersOrdered: null,
                yesNoView: null,
                maxAllowedAnswers: null,
                mask: null,
                isFilteredCombobox: isFilteredCombobox,
                cascadeFromQuestionId: cascadeFromQuestionId,
                sourceQuestionnaireId: sourceQuestionnaireId,
                sourceQuestionId: sourceQuestionId,
                targetIndex: targetIndex,
                maxAnswerCount: maxAnswerCount,
                countOfDecimalPlaces: countOfDecimalPlaces);
        }

        public static QuestionChanged CreateQuestionChanged(Guid publicKey, Guid targetGroupKey, Guid? groupPublicKey = null, string questionText = null, bool? isInteger = null,
            string stataExportCaption = null, Guid? linkedToQuestionId = null, bool capital = false, string validationExpression = null, string validationMessage = null,
            QuestionScope questionScope = QuestionScope.Interviewer, string instructions = null, Answer[] answers = null, bool featured = false, Guid? responsibleId = null,
            QuestionType questionType = QuestionType.Text, bool? isFilteredCombobox = null, Guid? cascadeFromQuestionId = null, string conditionExpression = null, Order? answerOrder = null)
        {
            return new QuestionChanged(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: null,
                featured: featured,
                questionScope: questionScope,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: capital,
                isInteger: isInteger,
                questionType: questionType,
                answerOrder: answerOrder,
                answers: answers,
                linkedToQuestionId: null,
                areAnswersOrdered: null,
                yesNoView: null,
                maxAllowedAnswers: null,
                mask: null,
                isFilteredCombobox: isFilteredCombobox,
                cascadeFromQuestionId: cascadeFromQuestionId,
                targetGroupKey: targetGroupKey);
        }

        public static NumericQuestionAdded CreateNumericQuestionAdded(Guid publicKey, Guid groupPublicKey,
            bool? isInteger = null,
            string stataExportCaption = null,
            string questionText = null,
            string variableLabel = null,
            bool featured = false,
            string conditionExpression = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            Guid? responsibleId = null)
        {
            return new NumericQuestionAdded(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: variableLabel,
                featured: featured,
                questionScope: QuestionScope.Interviewer,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: false,
                isInteger: isInteger,
                countOfDecimalPlaces: null);
        }

        public static NumericQuestionCloned CreateNumericQuestionCloned(Guid publicKey,
            Guid sourceQuestionId,
            Guid groupPublicKey,
            bool? isInteger = null,
            string stataExportCaption = null,
            string questionText = null,
            string variableLabel = null,
            bool featured = false,
            string conditionExpression = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            Guid? responsibleId = null,
            int targetIndex = 0)
        {
            return new NumericQuestionCloned(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: variableLabel,
                featured: featured,
                questionScope: QuestionScope.Interviewer,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: false,
                isInteger: isInteger,
                countOfDecimalPlaces: null,
                sourceQuestionnaireId: null,
                sourceQuestionId: sourceQuestionId,
                targetIndex: targetIndex);
        }

        public static NumericQuestionChanged CreateNumericQuestionChanged(
            Guid publicKey,
            bool? isInteger = null,
            string stataExportCaption = null,
            string questionText = null,
            string variableLabel = null,
            bool featured = false,
            string conditionExpression = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            Guid? responsibleId = null)
        {
            return new NumericQuestionChanged(
                publicKey: publicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: variableLabel,
                featured: featured,
                questionScope: QuestionScope.Interviewer,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: false,
                isInteger: isInteger,
                countOfDecimalPlaces: null);
        }

        public static QuestionChanged CreateQuestionChanged(Guid publicKey, Guid? groupPublicKey = null, string questionText = null, bool? isInteger = null,
            string stataExportCaption = null, Guid? linkedToQuestionId = null, bool capital = false, string validationExpression = null, string validationMessage = null,
            QuestionScope questionScope = QuestionScope.Interviewer, string instructions = null, Answer[] answers = null, bool featured = false, Guid? responsibleId = null,
            QuestionType questionType = QuestionType.Text, bool? isFilteredCombobox = null, Guid? cascadeFromQuestionId = null, string conditionExpression = null, Order? answerOrder = null)
        {
            return new QuestionChanged(
                publicKey: publicKey,
                groupPublicKey: groupPublicKey,
                questionText: questionText,
                stataExportCaption: stataExportCaption,
                variableLabel: null,
                featured: featured,
                questionScope: questionScope,
                conditionExpression: conditionExpression,
                validationExpression: validationExpression,
                validationMessage: validationMessage,
                instructions: instructions,
                responsibleId: responsibleId.HasValue ? responsibleId.Value : Guid.NewGuid(),
                capital: capital,
                isInteger: isInteger,
                questionType: questionType,
                answerOrder: answerOrder,
                answers: answers,
                linkedToQuestionId: null,
                areAnswersOrdered: null,
                yesNoView: null,
                maxAllowedAnswers: null,
                mask: null,
                isFilteredCombobox: isFilteredCombobox,
                cascadeFromQuestionId: cascadeFromQuestionId,
                targetGroupKey: Guid.NewGuid());
        }
    }
}
