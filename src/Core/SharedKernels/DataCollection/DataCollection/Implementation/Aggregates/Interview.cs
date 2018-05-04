﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Ncqrs.Domain;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Invariants;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Questionnaire.Documents;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public partial class Interview : AggregateRootMappedByConvention
    {
        public Interview(IInterviewTreeBuilder treeBuilder)
        {
            this.treeBuilder = treeBuilder;
        }

        private InterviewTree tree;
        protected InterviewTree Tree => this.tree 
                                        ?? (this.tree = this.treeBuilder.BuildInterviewTree(this.EventSourceId, this.GetQuestionnaireOrThrow()));

        protected readonly InterviewEntities.InterviewProperties properties = new InterviewEntities.InterviewProperties();

        public override Guid EventSourceId
        {
            get { return base.EventSourceId; }

            protected set
            {
                base.EventSourceId = value;
                this.properties.Id = value.FormatGuid();
            }
        }

        private ILatestInterviewExpressionState expressionProcessorStatePrototype = null;
        protected ILatestInterviewExpressionState ExpressionProcessorStatePrototype
        {
            get
            {
                if (this.expressionProcessorStatePrototype == null)
                {
                    this.expressionProcessorStatePrototype = this.expressionProcessorStatePrototypeProvider.GetExpressionState(
                            this.QuestionnaireIdentity.QuestionnaireId, this.QuestionnaireIdentity.Version);

                    this.expressionProcessorStatePrototype.SetInterviewProperties(new InterviewProperties(EventSourceId));
                }

                return this.expressionProcessorStatePrototype;
            }

            set => expressionProcessorStatePrototype = value;
        }


        private IInterviewExpressionStorage expressionStorageCached = null;
        protected IInterviewExpressionStorage GetExpressionStorage()
        {
            return expressionStorageCached ?? (expressionStorageCached = this.expressionProcessorStatePrototypeProvider.GetExpressionStorage(this.QuestionnaireIdentity));
        }

        /// <remarks>
        /// Repository operations are time-consuming.
        /// So this repository may be used only in command handlers.
        /// And should never be used in event handlers!!
        /// </remarks>
        private readonly IQuestionnaireStorage questionnaireRepository;

        private readonly IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider;

        private readonly ISubstitutionTextFactory substitutionTextFactory;

        private readonly IInterviewTreeBuilder treeBuilder;

        protected InterviewKey interviewKey;

        public Interview(IQuestionnaireStorage questionnaireRepository,
            IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider,
            ISubstitutionTextFactory substitutionTextFactory,
            IInterviewTreeBuilder treeBuilder)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.expressionProcessorStatePrototypeProvider = expressionProcessorStatePrototypeProvider;
            this.substitutionTextFactory = substitutionTextFactory;
            this.treeBuilder = treeBuilder;
        }

        #region Apply (state restore) methods

        public virtual void Apply(InterviewKeyAssigned @event)
        {
            this.interviewKey = @event.Key;
        }

        public virtual void Apply(InterviewReceivedByInterviewer @event)
        {
            this.properties.IsReceivedByInterviewer = true;
        }

        public virtual void Apply(InterviewReceivedBySupervisor @event)
        {
            this.properties.IsReceivedByInterviewer = false;
        }

        public virtual void Apply(InterviewCreated @event)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(@event.QuestionnaireId, @event.QuestionnaireVersion);
            this.UsesExpressionStorage = @event.UsesExpressionStorage;
            this.properties.AssignmentId = @event.AssignmentId;
        }

        public virtual void Apply(InterviewOnClientCreated @event)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(@event.QuestionnaireId, @event.QuestionnaireVersion);
            this.UsesExpressionStorage = @event.UsesExpressionStorage;
            this.properties.AssignmentId = @event.AssignmentId;
        }

        public virtual void Apply(InterviewFromPreloadedDataCreated @event)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(@event.QuestionnaireId, @event.QuestionnaireVersion);
            this.UsesExpressionStorage = @event.UsesExpressionStorage;
            this.properties.AssignmentId = @event.AssignmentId;
        }

        public virtual void Apply(SynchronizationMetadataApplied @event)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(@event.QuestionnaireId, @event.QuestionnaireVersion);
            this.UsesExpressionStorage = @event.UsesExpressionStorage;
            this.properties.Status = @event.Status;
        }

        public virtual void Apply(TextQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity)?.SetAnswer(TextAnswer.FromString(@event.Answer));
            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateTextAnswer(@event.QuestionId, @event.RosterVector, @event.Answer);
        }

        public virtual void Apply(QRBarcodeQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(QRBarcodeAnswer.FromString(@event.Answer));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateQrBarcodeAnswer(@event.QuestionId, @event.RosterVector, @event.Answer);
        }

        public virtual void Apply(PictureQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(MultimediaAnswer.FromString(@event.PictureFileName, @event.AnswerTimeUtc));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateMediaAnswer(@event.QuestionId, @event.RosterVector, @event.PictureFileName);
        }

        public virtual void Apply(NumericRealQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(NumericRealAnswer.FromDecimal(@event.Answer));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateNumericRealAnswer(@event.QuestionId, @event.RosterVector, (double)@event.Answer);
        }

        public virtual void Apply(NumericIntegerQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(NumericIntegerAnswer.FromInt(@event.Answer));
            this.ActualizeRostersIfQuestionIsRosterSize(@event.QuestionId);

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateNumericIntegerAnswer(@event.QuestionId, @event.RosterVector, @event.Answer);
        }

        public virtual void Apply(DateTimeQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(DateTimeAnswer.FromDateTime(@event.Answer));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateDateAnswer(@event.QuestionId, @event.RosterVector, @event.Answer);
        }

        public virtual void Apply(SingleOptionQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            var question = this.Tree.GetQuestion(questionIdentity);

            question.SetAnswer(CategoricalFixedSingleOptionAnswer.FromDecimal(@event.SelectedValue));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateSingleOptionAnswer(@event.QuestionId, @event.RosterVector, @event.SelectedValue);
        }

        public virtual void Apply(MultipleOptionsQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            var question = this.Tree.GetQuestion(questionIdentity);

            question.SetAnswer(CategoricalFixedMultiOptionAnswer.Convert(@event.SelectedValues));

            this.ActualizeRostersIfQuestionIsRosterSize(@event.QuestionId);
            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateMultiOptionAnswer(@event.QuestionId, @event.RosterVector, @event.SelectedValues);
        }

        public virtual void Apply(YesNoQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(YesNoAnswer.FromAnsweredYesNoOptions(@event.AnsweredOptions));
            this.ActualizeRostersIfQuestionIsRosterSize(@event.QuestionId);

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateYesNoAnswer(@event.QuestionId, @event.RosterVector, YesNoAnswer.FromAnsweredYesNoOptions(@event.AnsweredOptions).ToYesNoAnswersOnly());
        }

        public virtual void Apply(GeoLocationQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(GpsAnswer.FromGeoPosition(new GeoPosition(
                    @event.Latitude, @event.Longitude, @event.Accuracy, @event.Altitude, @event.Timestamp)));

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateGeoLocationAnswer(@event.QuestionId, @event.RosterVector, @event.Latitude,
                @event.Longitude, @event.Accuracy, @event.Altitude);
        }

        public virtual void Apply(AreaQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(AreaAnswer.FromArea(new Area(@event.Geometry, @event.MapName, @event.AreaSize,
                @event.Length, @event.Coordinates, @event.DistanceToEditor)));
        }

        public virtual void Apply(AudioQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(AudioAnswer.FromString(@event.FileName, @event.Length));
        }

        public virtual void Apply(TextListQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);

            this.Tree.GetQuestion(questionIdentity).SetAnswer(TextListAnswer.FromTupleArray(@event.Answers));
            this.ActualizeRostersIfQuestionIsRosterSize(@event.QuestionId);

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateTextListAnswer(@event.QuestionId, @event.RosterVector, @event.Answers);
        }

        public virtual void Apply(SingleOptionLinkedQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);
            this.Tree.GetQuestion(questionIdentity).SetAnswer(CategoricalLinkedSingleOptionAnswer.FromRosterVector(@event.SelectedRosterVector));
            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateLinkedSingleOptionAnswer(@event.QuestionId, @event.RosterVector, @event.SelectedRosterVector);
        }

        public virtual void Apply(MultipleOptionsLinkedQuestionAnswered @event)
        {
            var questionIdentity = Identity.Create(@event.QuestionId, @event.RosterVector);
            this.SetStartDateOnFirstAnswerSet(questionIdentity, @event.AnswerTimeUtc);
            this.Tree.GetQuestion(questionIdentity).SetAnswer(CategoricalLinkedMultiOptionAnswer.FromRosterVectors(@event.SelectedRosterVectors.Select(x => new RosterVector(x)).ToArray()));
            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.UpdateLinkedMultiOptionAnswer(@event.QuestionId, @event.RosterVector, @event.SelectedRosterVectors);
        }

        public virtual void Apply(AnswersDeclaredValid @event)
        {
            foreach (var questionIdentity in @event.Questions)
                this.Tree.GetQuestion(questionIdentity)?.MarkValid();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DeclareAnswersValid(@event.Questions);
        }

        public virtual void Apply(AnswersDeclaredInvalid @event)
        {
            if (@event.FailedValidationConditions.Count > 0)
            {
                foreach (var failedValidationCondition in @event.FailedValidationConditions)
                {
                    if (failedValidationCondition.Value?.Count > 0)
                        this.Tree.GetQuestion(failedValidationCondition.Key).MarkInvalid(failedValidationCondition.Value);
                    else
                        this.Tree.GetQuestion(failedValidationCondition.Key).MarkInvalid();
                }

                if (this.UsesExpressionStorage) return;
                this.ExpressionProcessorStatePrototype.ApplyFailedValidations(@event.FailedValidationConditions);
            }
            else //handling of old events
            {
                foreach (var invalidQuestionIdentity in @event.Questions)
                    this.Tree.GetQuestion(invalidQuestionIdentity).MarkInvalid();

                if (this.UsesExpressionStorage) return;
                this.ExpressionProcessorStatePrototype.DeclareAnswersInvalid(@event.Questions);
            }
        }

        public virtual void Apply(StaticTextsDeclaredValid @event)
        {
            foreach (var staticTextIdentity in @event.StaticTexts)
                this.Tree.GetStaticText(staticTextIdentity).MarkValid();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DeclareStaticTextValid(@event.StaticTexts);
        }

        public virtual void Apply(StaticTextsDeclaredInvalid @event)
        {
            var staticTextsConditions = @event.GetFailedValidationConditionsDictionary();

            foreach (var staticTextIdentity in staticTextsConditions.Keys)
                this.Tree.GetStaticText(staticTextIdentity).MarkInvalid(staticTextsConditions[staticTextIdentity]);

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.ApplyStaticTextFailedValidations(staticTextsConditions);
        }

        public virtual void Apply(AnswersDeclaredPlausible @event)
        {
            foreach (var questionIdentity in @event.Questions)
                this.Tree.GetQuestion(questionIdentity)?.MarkPlausible();
        }

        public virtual void Apply(AnswersDeclaredImplausible @event)
        {
            var questionsConditions = @event.GetFailedValidationConditionsDictionary();

            foreach (var questionIdentity in questionsConditions.Keys)
                this.Tree.GetQuestion(questionIdentity).MarkImplausible(questionsConditions[questionIdentity]);
        }

        public virtual void Apply(StaticTextsDeclaredPlausible @event)
        {
            foreach (var staticTextIdentity in @event.StaticTexts)
                this.Tree.GetStaticText(staticTextIdentity).MarkPlausible();
        }

        public virtual void Apply(StaticTextsDeclaredImplausible @event)
        {
            var staticTextsConditions = @event.GetFailedValidationConditionsDictionary();

            foreach (var staticTextIdentity in staticTextsConditions.Keys)
                this.Tree.GetStaticText(staticTextIdentity).MarkImplausible(staticTextsConditions[staticTextIdentity]);
        }

        public void Apply(LinkedOptionsChanged @event)
        {
            foreach (var linkedQuestion in @event.ChangedLinkedQuestions)
                this.Tree.GetQuestion(linkedQuestion.QuestionId)?.AsLinked.SetOptions(linkedQuestion.Options);
        }

        public void Apply(LinkedToListOptionsChanged @event)
        {
            foreach (var linkedQuestion in @event.ChangedLinkedQuestions)
                this.Tree.GetQuestion(linkedQuestion.QuestionId).AsLinkedToList.SetOptions(linkedQuestion.Options?.Select(Convert.ToInt32) ?? EmptyArray<int>.Value);
        }

        public virtual void Apply(GroupsDisabled @event)
        {
            foreach (var groupIdentity in @event.Groups)
                this.Tree.GetGroup(groupIdentity).Disable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DisableGroups(@event.Groups);
        }

        public virtual void Apply(GroupsEnabled @event)
        {
            foreach (var groupIdentity in @event.Groups)
                this.Tree.GetGroup(groupIdentity)?.Enable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.EnableGroups(@event.Groups);
        }

        public virtual void Apply(VariablesDisabled @event)
        {
            foreach (var variableIdentity in @event.Variables)
                this.Tree.GetVariable(variableIdentity).Disable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DisableVariables(@event.Variables);
        }

        public virtual void Apply(VariablesEnabled @event)
        {
            foreach (var variableIdentity in @event.Variables)
                this.Tree.GetVariable(variableIdentity)?.Enable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.EnableVariables(@event.Variables);
        }

        public virtual void Apply(VariablesChanged @event)
        {
            foreach (var changedVariableValueDto in @event.ChangedVariables)
            {
                this.Tree.GetVariable(changedVariableValueDto.Identity)?.SetValue(changedVariableValueDto.NewValue);
                if (this.UsesExpressionStorage) continue;
                this.ExpressionProcessorStatePrototype.UpdateVariableValue(changedVariableValueDto.Identity, changedVariableValueDto.NewValue);
            }
        }

        public virtual void Apply(QuestionsDisabled @event)
        {
            foreach (var questionIdentity in @event.Questions)
                this.Tree.GetQuestion(questionIdentity).Disable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DisableQuestions(@event.Questions);
        }

        public virtual void Apply(QuestionsEnabled @event)
        {
            foreach (var questionIdentity in @event.Questions)
                this.Tree.GetQuestion(questionIdentity)?.Enable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.EnableQuestions(@event.Questions);
        }

        public virtual void Apply(QuestionsMarkedAsReadonly @event)
        {
            foreach (var questionIdentity in @event.Questions)
                this.Tree.GetQuestion(questionIdentity)?.MarkAsReadonly();
        }

        public virtual void Apply(AnswersMarkedAsProtected @event)
        {
            foreach (var protectedAnswer in @event.Questions)
                this.Tree.GetQuestion(protectedAnswer).ProtectAnswer();
        }

        public virtual void Apply(StaticTextsEnabled @event)
        {
            foreach (var staticTextIdentity in @event.StaticTexts)
                this.Tree.GetStaticText(staticTextIdentity)?.Enable();

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.EnableStaticTexts(@event.StaticTexts);
        }

        public virtual void Apply(StaticTextsDisabled @event)
        {
            foreach (var staticTextIdentity in @event.StaticTexts)
                this.Tree.GetStaticText(staticTextIdentity).Disable();
            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.DisableStaticTexts(@event.StaticTexts);
        }

        public virtual void Apply(AnswerCommented @event)
        {
            var commentByQuestion = Identity.Create(@event.QuestionId, @event.RosterVector);

            var userRole = @event.UserId == this.properties.InterviewerId
                ? UserRoles.Interviewer
                : @event.UserId == this.properties.SupervisorId ? UserRoles.Supervisor : UserRoles.Headquarter;

            this.Tree.GetQuestion(commentByQuestion).AnswerComments.Add(new AnswerComment(@event.UserId, userRole, @event.CommentTime, @event.Comment,
                commentByQuestion));
        }

        public virtual void Apply(FlagSetToAnswer @event) { }

        public virtual void Apply(TranslationSwitched @event)
        {
            this.Language = @event.Language;

            var questionnaire = this.GetQuestionnaireOrThrow();

            this.Tree.SwitchQuestionnaire(questionnaire);
            this.UpdateTitlesAndTexts(questionnaire);
        }

        public virtual void Apply(FlagRemovedFromAnswer @event) { }

        public virtual void Apply(SubstitutionTitlesChanged @event)
        {
            foreach (var @group in @event.Groups)
                this.Tree.GetGroup(@group)?.ReplaceSubstitutions();

            foreach (var staticText in @event.StaticTexts)
                this.Tree.GetStaticText(staticText)?.ReplaceSubstitutions();

            foreach (var question in @event.Questions)
                this.Tree.GetQuestion(question)?.ReplaceSubstitutions();
        }

        public virtual void Apply(RosterInstancesTitleChanged @event)
        {
            foreach (var changedRosterTitle in @event.ChangedInstances)
                this.Tree.GetRoster(changedRosterTitle.RosterInstance.GetIdentity())?.SetRosterTitle(changedRosterTitle.Title);
        }

        private bool isFixedRostersInitialized = false;
        public virtual void Apply(RosterInstancesAdded @event)
        {
            // compatibility with previous versions < 5.16
            // for fixed rosters only
            if (!this.isFixedRostersInitialized)
            {
                this.Tree.ActualizeTree();
                this.isFixedRostersInitialized = true;
            }

            if (this.UsesExpressionStorage) return;
            foreach (var instance in @event.Instances)
            {
                this.ExpressionProcessorStatePrototype.AddRoster(instance.GroupId, instance.OuterRosterVector,
                    instance.RosterInstanceId, instance.SortIndex);
            }
        }

        public virtual void Apply(RosterInstancesRemoved @event)
        {
            if (this.UsesExpressionStorage) return;
            foreach (var instance in @event.Instances)
            {
                this.ExpressionProcessorStatePrototype.RemoveRoster(instance.GroupId, instance.OuterRosterVector, instance.RosterInstanceId);
            }
        }

        public virtual void Apply(InterviewStatusChanged @event)
        {
            this.properties.Status = @event.Status;
        }

        public virtual void Apply(SupervisorAssigned @event)
        {
            this.properties.SupervisorId = @event.SupervisorId;
        }

        public virtual void Apply(InterviewerAssigned @event)
        {
            this.properties.InterviewerId = @event.InterviewerId;
            this.properties.IsReceivedByInterviewer = false;
            this.properties.InterviewerAssignedDateTime = @event.AssignTime;
        }

        public virtual void Apply(InterviewDeleted @event) { }

        public virtual void Apply(InterviewHardDeleted @event)
        {
            this.properties.IsHardDeleted = true;
        }

        public virtual void Apply(InterviewSentToHeadquarters @event) { }

        public virtual void Apply(InterviewRestored @event) { }

        public virtual void Apply(InterviewCompleted @event)
        {
            this.properties.WasCompleted = true;
            this.properties.CompletedDate = @event.CompleteTime;
        }

        public virtual void Apply(InterviewRestarted @event) { }

        public virtual void Apply(InterviewApproved @event) { }

        public virtual void Apply(InterviewApprovedByHQ @event) { }

        public virtual void Apply(UnapprovedByHeadquarters @event) { }

        public virtual void Apply(InterviewRejected @event)
        {
            this.properties.WasCompleted = false;
            this.properties.WasRejected = true;
            this.properties.RejectDateTime = @event.RejectTime;
        }

        public virtual void Apply(InterviewRejectedByHQ @event) { }

        public virtual void Apply(InterviewDeclaredValid @event)
        {
            this.properties.IsValid = true;
        }

        public virtual void Apply(InterviewDeclaredInvalid @event)
        {
            this.properties.IsValid = false;
        }

        public virtual void Apply(AnswersRemoved @event)
        {
            foreach (var identity in @event.Questions)
            {
                // can be removed from removed roster. No need for this event anymore
                this.Tree.GetQuestion(identity)?.RemoveAnswer();
                this.ActualizeRostersIfQuestionIsRosterSize(identity.Id);
                if (this.UsesExpressionStorage) continue;
                this.ExpressionProcessorStatePrototype.RemoveAnswer(new Identity(identity.Id, identity.RosterVector));
            }
        }

        public virtual void Apply(AnswerRemoved @event)
        {
            this.Tree.GetQuestion(Identity.Create(@event.QuestionId, @event.RosterVector)).RemoveAnswer();
            this.ActualizeRostersIfQuestionIsRosterSize(@event.QuestionId);

            if (this.UsesExpressionStorage) return;
            this.ExpressionProcessorStatePrototype.RemoveAnswer(new Identity(@event.QuestionId, @event.RosterVector));
        }

        #endregion

        #region Questionnaire

        public string Language { get; private set; }
        public QuestionnaireIdentity QuestionnaireIdentity { get; protected set; }

        public bool UsesExpressionStorage { get; protected set; } = false;
        public string QuestionnaireId => this.QuestionnaireIdentity?.ToString();

        protected IQuestionnaire GetQuestionnaireOrThrow() => this.GetQuestionnaireOrThrow(this.Language);

        private IQuestionnaire GetQuestionnaireOrThrow(string language)
        {
            IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(this.QuestionnaireIdentity, language);

            if (questionnaire == null)
                throw new InterviewException(
                    $"Questionnaire '{this.QuestionnaireIdentity}' was not found. InterviewId {this.EventSourceId}",
                    InterviewDomainExceptionType.QuestionnaireIsMissing);

            return questionnaire;
        }

        protected static string FormatQuestionForException(Guid questionId, IQuestionnaire questionnaire)
            => $"'{GetQuestionTitleForException(questionId, questionnaire)} [{GetQuestionVariableNameForException(questionId, questionnaire)}]'";

        protected static string FormatGroupForException(Guid groupId, IQuestionnaire questionnaire)
            => $"'{GetGroupTitleForException(groupId, questionnaire)} ({groupId:N})'";

        private static string GetQuestionTitleForException(Guid questionId, IQuestionnaire questionnaire)
            => questionnaire.HasQuestion(questionId)
                ? questionnaire.GetQuestionTitle(questionId) ?? "<<NO QUESTION TITLE>>"
                : "<<MISSING QUESTION>>";

        private static string GetQuestionVariableNameForException(Guid questionId, IQuestionnaire questionnaire)
            => questionnaire.HasQuestion(questionId)
                ? questionnaire.GetQuestionVariableName(questionId) ?? "<<NO VARIABLE NAME>>"
                : "<<MISSING QUESTION>>";

        private static string GetGroupTitleForException(Guid groupId, IQuestionnaire questionnaire)
            => questionnaire.HasGroup(groupId)
                ? questionnaire.GetGroupTitle(groupId) ?? "<<NO GROUP TITLE>>"
                : "<<MISSING GROUP>>";


        /// Filter for regular categorical questions, such as YesNo, Single and Multi.
        public virtual List<CategoricalOption> GetFirstTopFilteredOptionsForQuestion(Identity questionIdentity, 
            int? parentQuestionValue, string filter, int itemsCount = 200)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            if (!questionnaire.IsSupportFilteringForOptions(questionIdentity.Id))
                return questionnaire.GetOptionsForQuestion(questionIdentity.Id, parentQuestionValue, filter).Take(itemsCount).ToList();

            if (this.UsesExpressionStorage)
            {
                var unfilteredOptionsForQuestion = questionnaire.GetOptionsForQuestion(questionIdentity.Id, parentQuestionValue, filter);

                return this.FilteredCategoricalOptions(questionIdentity, itemsCount, unfilteredOptionsForQuestion);
            }

            return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(questionIdentity,
                questionnaire.GetOptionsForQuestion(questionIdentity.Id, parentQuestionValue, filter)).Take(itemsCount).ToList();
        }

        protected List<CategoricalOption> FilteredCategoricalOptions(Identity questionIdentity, int itemsCount,
            IEnumerable<CategoricalOption> unfilteredOptionsForQuestion)
        {
            // too much
            IInterviewExpressionStorage expressionStorage = this.GetExpressionStorage();

            var interviewPropertiesForExpressions = new InterviewPropertiesForExpressions(new InterviewProperties(this.EventSourceId), this.properties);

            expressionStorage.Initialize(new InterviewStateForExpressions(this.tree, interviewPropertiesForExpressions));

            var question = this.tree.GetQuestion(questionIdentity);

            var nearestRoster = question.Parents.OfType<InterviewTreeRoster>().LastOrDefault()?.Identity ??
                                new Identity(this.QuestionnaireIdentity.QuestionnaireId, RosterVector.Empty);

            var level = expressionStorage.GetLevel(nearestRoster);
            var categoricalFilter = level.GetCategoricalFilter(questionIdentity);

            return unfilteredOptionsForQuestion
                .Where(x => RunOptionFilter(categoricalFilter, x.Value))
                .Take(itemsCount)
                .ToList();
        }

        private static bool RunOptionFilter(Func<int, bool> filter, int selectedValue)
        {
            try
            {
                return filter(selectedValue);
            }
            catch
            {
                return false;
            }
        }

        public CategoricalOption GetOptionForQuestionWithoutFilter(Identity question, int value, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            return questionnaire.GetOptionForQuestionByOptionValue(question.Id, value);
        }

        public CategoricalOption GetOptionForQuestionWithFilter(Identity question, string optionText, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            CategoricalOption filteredOption = questionnaire.GetOptionForQuestionByOptionText(question.Id, optionText, parentQuestionValue);

            if (filteredOption == null)
                return null;

            if (questionnaire.IsSupportFilteringForOptions(question.Id))
            {
                if (this.UsesExpressionStorage)
                {
                    return FilteredCategoricalOptions(question, 1, filteredOption.ToEnumerable()).SingleOrDefault();
                }
                return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(question, Enumerable.Repeat(filteredOption, 1)).SingleOrDefault();
            }
            return filteredOption;
        }

        #endregion

        #region Assembly

        protected void UpdateExpressionState(InterviewTree changedInterview, ILatestInterviewExpressionState expressionState)
            => this.UpdateExpressionState(this.Tree, changedInterview, expressionState);

        protected void UpdateExpressionState(InterviewTree sourceInterview, InterviewTree changedInterview, ILatestInterviewExpressionState expressionState)
        {
            var diff = sourceInterview.Compare(changedInterview);

            var diffByQuestions = diff.OfType<InterviewTreeQuestionDiff>().ToList();
            var questionsWithRemovedAnswer = diffByQuestions.Where(x => x.IsAnswerRemoved).ToArray();
            var questionsWithChangedAnswer = diffByQuestions.Where(x => x.IsAnswerChanged).ToArray();
            var changedRosters = diff.OfType<InterviewTreeRosterDiff>().ToArray();
            var changedVariables = diff.OfType<InterviewTreeVariableDiff>().ToArray();

            UpdateRostersInExpressionState(changedRosters, expressionState);
            UpdateAnswersInExpressionState(questionsWithChangedAnswer, expressionState);
            RemoveAnswersInExpressionState(questionsWithRemovedAnswer, expressionState);
            UpdateEnablementInExpressionState(diff, expressionState);
            UpdateValidityInExpressionState(diff, expressionState);
            UpdateVariablesInExpressionState(changedVariables, expressionState);
        }

        private void UpdateVariablesInExpressionState(InterviewTreeVariableDiff[] diffsByChangedVariables, ILatestInterviewExpressionState expressionState)
        {
            var changedVariables = diffsByChangedVariables.Where(x => x.ChangedNode != null && x.IsValueChanged)
                    .Select(x => x.ChangedNode)
                    .ToArray();

            foreach (var changedVariable in changedVariables)
            {
                expressionState.UpdateVariableValue(changedVariable.Identity, changedVariable.Value);
            }
        }

        private static void UpdateValidityInExpressionState(IReadOnlyCollection<InterviewTreeNodeDiff> diff, ILatestInterviewExpressionState expressionState)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null).ToList();

            var allChangedQuestionDiffs = allNotNullableNodes.OfType<InterviewTreeQuestionDiff>().ToList();
            var allChangedStaticTextDiffs = allNotNullableNodes.OfType<InterviewTreeStaticTextDiff>().ToList();

            var validQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecameValid).Select(x => x.ChangedNode.Identity).ToArray();
            var invalidQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecameInvalid).Select(x => x.ChangedNode)
                .ToDictionary(x => x.Identity, x => x.FailedErrors);

            var validStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecameValid).Select(x => x.ChangedNode.Identity).ToArray();
            var invalidStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecameInvalid).Select(x => x.ChangedNode)
                .ToDictionary(x => x.Identity, x => x.FailedErrors);

            if (validQuestionIdentities.Any()) expressionState.DeclareAnswersValid(validQuestionIdentities);
            if (invalidQuestionIdentities.Any()) expressionState.ApplyFailedValidations(invalidQuestionIdentities);

            if (validStaticTextIdentities.Any()) expressionState.DeclareStaticTextValid(validStaticTextIdentities);
            if (invalidStaticTextIdentities.Any()) expressionState.ApplyStaticTextFailedValidations(invalidStaticTextIdentities);
        }

        private static void UpdateEnablementInExpressionState(IReadOnlyCollection<InterviewTreeNodeDiff> diff, ILatestInterviewExpressionState expressionState)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null).ToList();

            var diffByGroups = allNotNullableNodes.OfType<InterviewTreeGroupDiff>().ToList();
            var diffByQuestions = allNotNullableNodes.OfType<InterviewTreeQuestionDiff>().ToList();
            var diffByStaticTexts = allNotNullableNodes.OfType<InterviewTreeStaticTextDiff>().ToList();
            var diffByVariables = allNotNullableNodes.OfType<InterviewTreeVariableDiff>().ToList();

            var disabledGroups = diffByGroups.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledGroups = diffByGroups.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            var disabledQuestions = diffByQuestions.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledQuestions = diffByQuestions.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            var disabledStaticTexts = diffByStaticTexts.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledStaticTexts = diffByStaticTexts.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            var disabledVariables = diffByVariables.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledVariables = diffByVariables.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            if (disabledGroups.Any()) expressionState.DisableGroups(disabledGroups);
            if (enabledGroups.Any()) expressionState.EnableGroups(enabledGroups);
            if (disabledQuestions.Any()) expressionState.DisableQuestions(disabledQuestions);
            if (enabledQuestions.Any()) expressionState.EnableQuestions(enabledQuestions);
            if (disabledStaticTexts.Any()) expressionState.DisableStaticTexts(disabledStaticTexts);
            if (enabledStaticTexts.Any()) expressionState.EnableStaticTexts(enabledStaticTexts);
            if (disabledVariables.Any()) expressionState.DisableVariables(disabledVariables);
            if (enabledVariables.Any()) expressionState.EnableVariables(enabledVariables);
        }

        private static void RemoveAnswersInExpressionState(IReadOnlyCollection<InterviewTreeNodeDiff> diffByQuestions, ILatestInterviewExpressionState expressionState)
        {
            foreach (var diffByQuestion in diffByQuestions)
            {
                expressionState.RemoveAnswer(diffByQuestion.SourceNode.Identity);
            }
        }

        private static void UpdateAnswersInExpressionState(InterviewTreeQuestionDiff[] diffByQuestions, ILatestInterviewExpressionState expressionState)
        {
            foreach (var diffByQuestion in diffByQuestions)
            {
                var changedQuestion = diffByQuestion.ChangedNode;

                if (changedQuestion == null) continue;

                if (!changedQuestion.IsAnswered()) continue;

                if (changedQuestion.IsText)
                {
                    expressionState.UpdateTextAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeTextQuestion().GetAnswer().Value);
                }

                if (changedQuestion.IsTextList)
                {
                    expressionState.UpdateTextListAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeTextListQuestion().GetAnswer().ToTupleArray());
                }

                if (changedQuestion.IsDouble)
                {
                    expressionState.UpdateNumericRealAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeDoubleQuestion().GetAnswer().Value);
                }

                if (changedQuestion.IsInteger)
                {
                    expressionState.UpdateNumericIntegerAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeIntegerQuestion().GetAnswer().Value);
                }

                if (changedQuestion.IsDateTime)
                {
                    expressionState.UpdateDateAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeDateTimeQuestion().GetAnswer().Value);
                }

                if (changedQuestion.IsGps)
                {
                    var gpsAnswer = changedQuestion.GetAsInterviewTreeGpsQuestion().GetAnswer().Value;
                    expressionState.UpdateGeoLocationAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, gpsAnswer.Latitude, gpsAnswer.Longitude,
                        gpsAnswer.Accuracy, gpsAnswer.Altitude);
                }

                if (changedQuestion.IsQRBarcode)
                {
                    expressionState.UpdateQrBarcodeAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeQRBarcodeQuestion().GetAnswer().DecodedText);
                }

                if (changedQuestion.IsMultimedia)
                {
                    expressionState.UpdateMediaAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeMultimediaQuestion().GetAnswer().FileName);
                }

                if (changedQuestion.IsYesNo)
                {
                    expressionState.UpdateYesNoAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeYesNoQuestion().GetAnswer().ToYesNoAnswersOnly());
                }

                if (changedQuestion.IsSingleFixedOption || changedQuestion.IsCascading)
                {
                    expressionState.UpdateSingleOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeSingleOptionQuestion().GetAnswer().SelectedValue);
                }

                if (changedQuestion.IsMultiFixedOption)
                {
                    expressionState.UpdateMultiOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeMultiOptionQuestion().GetAnswer().ToDecimals().ToArray());
                }

                if (changedQuestion.IsSingleLinkedOption)
                {
                    expressionState.UpdateLinkedSingleOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeSingleLinkedToRosterQuestion().GetAnswer().SelectedValue);
                }

                if (changedQuestion.IsMultiLinkedOption)
                {
                    expressionState.UpdateLinkedMultiOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector,
                        changedQuestion.GetAsInterviewTreeMultiLinkedToRosterQuestion().GetAnswer().ToRosterVectorArray().Select(x => x.Select(Convert.ToDecimal).ToArray()).ToArray());
                }

                if (changedQuestion.IsSingleLinkedToList)
                {
                    expressionState.UpdateSingleOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeSingleOptionLinkedToListQuestion().GetAnswer().SelectedValue);
                }

                if (changedQuestion.IsMultiLinkedToList)
                {
                    expressionState.UpdateMultiOptionAnswer(changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, changedQuestion.GetAsInterviewTreeMultiOptionLinkedToListQuestion().GetAnswer().ToDecimals().ToArray());
                }
            }
        }

        private static void UpdateRostersInExpressionState(InterviewTreeRosterDiff[] diff, ILatestInterviewExpressionState expressionState)
        {
            var removedRosters = diff.Where(x => x.IsNodeRemoved).Select(x => x.SourceNode);
            var addedRosters = diff.Where(x => x.IsNodeAdded)
                .Select(x => x.ChangedNode)
                .OrderBy(x => x.RosterVector.Length)
                .ToList();

            foreach (var removedRosterIdentity in removedRosters.Select(ToRosterInstance))
            {
                expressionState.RemoveRoster(removedRosterIdentity.GroupId, removedRosterIdentity.OuterRosterVector, removedRosterIdentity.RosterInstanceId);
            }

            foreach (var rosterNode in addedRosters)
            {
                expressionState.AddRoster(rosterNode.Identity.Id, rosterNode.Identity.RosterVector.CoordinatesAsDecimals.Shrink(),
                    rosterNode.Identity.RosterVector.Last(), rosterNode.SortIndex);
            }
        }

        private ILatestInterviewExpressionState GetClonedExpressionState()
        {
            ILatestInterviewExpressionState expressionProcessorState = this.ExpressionProcessorStatePrototype.Clone();
            expressionProcessorState.SaveAllCurrentStatesAsPrevious();
            return expressionProcessorState;
        }

        #endregion

        #region Answer handlers

        public void AnswerTextQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, string answer)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireTextAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(TextAnswer.FromString(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerDateTimeQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, DateTime answer)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireDateTimeAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();
            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(DateTimeAnswer.FromDateTime(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerNumericIntegerQuestion(AnswerNumericIntegerQuestionCommand command)
            => AnswerNumericIntegerQuestion(command.UserId, command.QuestionId, command.RosterVector, command.AnswerTime, command.Answer);

        internal void AnswerNumericIntegerQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, int answer)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireNumericIntegerAnswerAllowed(answer, this.tree.GetQuestion(questionIdentity)?.GetAsInterviewTreeIntegerQuestion()?.ProtectedAnswer);

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(NumericIntegerAnswer.FromInt(answer));

            changedInterviewTree.ActualizeTree();

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerNumericRealQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, double answer)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireNumericRealAnswerAllowed(answer);

            var changedInterviewTree = this.Tree.Clone();
            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(NumericRealAnswer.FromDouble(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerSingleOptionQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, int selectedValue)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            var treeQuestion = this.Tree.GetQuestion(questionIdentity);

            if (treeQuestion.IsLinkedToListQuestion)
            {
                new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                    .RequireLinkedToListSingleOptionAnswerAllowed(selectedValue);
            }
            else
            {
                new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                    .RequireFixedSingleOptionAnswerAllowed(selectedValue, this.QuestionnaireIdentity);
            }

            var changedInterviewTree = this.Tree.Clone();

            var givenAndRemovedAnswers = new List<Identity> { questionIdentity };
            var singleQuestion = changedInterviewTree.GetQuestion(questionIdentity);

            if (treeQuestion.IsLinkedToListQuestion)
            {
                singleQuestion.SetAnswer(CategoricalFixedSingleOptionAnswer.FromInt(Convert.ToInt32(selectedValue)));
            }
            else
            {
                var question = singleQuestion;
                var questionWasAnsweredAndAnswerChanged = question.IsAnswered() && question.GetAsInterviewTreeSingleOptionQuestion().GetAnswer().SelectedValue != selectedValue;
                question.SetAnswer(CategoricalFixedSingleOptionAnswer.FromInt(Convert.ToInt32(selectedValue)));

                if (questionWasAnsweredAndAnswerChanged)
                {
                    RemoveAnswersForDependendCascadingQuestions(questionIdentity, changedInterviewTree, questionnaire, givenAndRemovedAnswers);
                }
            }

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerSingleOptionLinkedQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, decimal[] selectedRosterVector)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireLinkedToRosterSingleOptionAnswerAllowed(selectedRosterVector);

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(CategoricalLinkedSingleOptionAnswer.FromRosterVector(selectedRosterVector));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerMultipleOptionsQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, int[] selectedValues)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            var answeredQuestion = this.Tree.GetQuestion(questionIdentity);
            var isLinkedToList = answeredQuestion.IsLinkedToListQuestion;

            if (isLinkedToList)
            {
                new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                    .RequireLinkedToListMultipleOptionsAnswerAllowed(selectedValues);
            }
            else
            {
                var protectedValues = answeredQuestion.GetAsInterviewTreeMultiOptionQuestion()?.ProtectedAnswers;
                new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                    .RequireFixedMultipleOptionsAnswerAllowed(selectedValues, protectedValues);
            }

            var changedInterviewTree = this.Tree.Clone();

            if (isLinkedToList)
            {
                changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(CategoricalFixedMultiOptionAnswer.Convert(selectedValues));
            }
            else
                changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(CategoricalFixedMultiOptionAnswer.Convert(selectedValues));

            changedInterviewTree.ActualizeTree();

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerMultipleOptionsLinkedQuestion(Guid userId, Guid questionId, RosterVector rosterVector,
            DateTime answerTime, RosterVector[] selectedRosterVectors)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireLinkedToRosterMultipleOptionsAnswerAllowed(selectedRosterVectors);

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity)
                .SetAnswer(CategoricalLinkedMultiOptionAnswer.FromRosterVectors(selectedRosterVectors));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerYesNoQuestion(AnswerYesNoQuestion command)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(command.QuestionId, command.RosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(command.Question, questionnaire, this.Tree)
                .RequireYesNoAnswerAllowed(YesNoAnswer.FromAnsweredYesNoOptions(command.AnsweredOptions));

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(YesNoAnswer.FromAnsweredYesNoOptions(command.AnsweredOptions));

            changedInterviewTree.ActualizeTree();

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, command.UserId);
        }

        public void AnswerTextListQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime,
            Tuple<decimal, string>[] answers)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireTextListAnswerAllowed(answers, 
                    this.tree.GetQuestion(questionIdentity)?.GetAsInterviewTreeTextListQuestion()?.ProtectedAnswers ?? Array.Empty<TextListAnswerRow>());

            var changedInterviewTree = this.Tree.Clone();
            var interviewTreeQuestion = changedInterviewTree.GetQuestion(questionIdentity);

            interviewTreeQuestion.SetAnswer(TextListAnswer.FromTupleArray(answers));

            changedInterviewTree.ActualizeTree();

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerGeoLocationQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, double latitude, double longitude,
            double accuracy, double altitude, DateTimeOffset timestamp)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            var questionIdentity = new Identity(questionId, rosterVector);

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireGpsCoordinatesAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();

            var answer = new GeoPosition(latitude, longitude, accuracy, altitude, timestamp);
            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(GpsAnswer.FromGeoPosition(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerQRBarcodeQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, string answer)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireQRBarcodeAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();
            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(QRBarcodeAnswer.FromString(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerAreaQuestion(AnswerAreaQuestionCommand command)
        {
            new InterviewPropertiesInvariants(this.properties).RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(command.QuestionId, command.RosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireAreaAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();

            var answer = new Area(command.Geometry, command.MapName, command.Area, command.Length, command.Coordinates, command.DistanceToEditor);
            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(AreaAnswer.FromArea(answer));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, command.UserId);
        }


        public void AnswerPictureQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, string pictureFileName)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequirePictureAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(MultimediaAnswer.FromString(pictureFileName, answerTime));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public void AnswerAudioQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, string pictureFileName, TimeSpan length)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireAudioAnswerAllowed();

            var changedInterviewTree = this.Tree.Clone();

            changedInterviewTree.GetQuestion(questionIdentity).SetAnswer(AudioAnswer.FromString(pictureFileName, length));

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        public bool IsAnswerProtected(Identity questionIdentity, decimal value)
        {
            var question = this.Tree.GetQuestion(questionIdentity);
            return question.IsAnswerProtected(value);
        }

        public void RemoveAnswer(Guid questionId, RosterVector rosterVector, Guid userId, DateTime removeTime)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            var questionIdentity = new Identity(questionId, rosterVector);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(questionIdentity, questionnaire, this.Tree)
                .RequireQuestionExists()
                .RequireQuestionEnabled();

            var targetQuestion = this.tree.GetQuestion(questionIdentity);
            if (targetQuestion.HasProtectedAnswer())
            {
                throw new InterviewException("Removing protected answer is not allowed",
                    InterviewDomainExceptionType.AnswerNotAccepted)
                {
                    Data =
                    {
                        { InterviewQuestionInvariants.ExceptionKeys.InterviewId, this.EventSourceId },
                        { InterviewQuestionInvariants.ExceptionKeys.QuestionId, questionIdentity.ToString() }
                    }
                };
            }

            var changedInterviewTree = this.Tree.Clone();

            var givenAndRemovedAnswers = new List<Identity> { questionIdentity };
            changedInterviewTree.GetQuestion(questionIdentity).RemoveAnswer();

            RemoveAnswersForDependendCascadingQuestions(questionIdentity, changedInterviewTree, questionnaire, givenAndRemovedAnswers);

            changedInterviewTree.ActualizeTree();

            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, questionIdentity);
            var treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, userId);
        }

        private static void RemoveAnswersForDependendCascadingQuestions(Identity questionIdentity, InterviewTree changedInterviewTree, IQuestionnaire questionnaire, List<Identity> givenAndRemovedAnswers)
        {
            IEnumerable<Guid> dependentQuestionIds = questionnaire.GetCascadingQuestionsThatDependUponQuestion(questionIdentity.Id);
            foreach (var dependentQuestionId in dependentQuestionIds)
            {
                var cascadingAnsweredQuestionsToRemoveAnswer = changedInterviewTree.FindQuestions(dependentQuestionId)
                    .Where(x => x.IsCascading && x.IsAnswered())
                    .Where(x => x.IsOnTheSameOrDeeperLevel(questionIdentity));

                foreach (var cascadingQuestion in cascadingAnsweredQuestionsToRemoveAnswer)
                {
                    cascadingQuestion.RemoveAnswer();
                    givenAndRemovedAnswers.Add(cascadingQuestion.Identity);
                }
            }
        }

        #endregion

        #region Other handlers

        public void CreateInterview(CreateInterview command)
        {
            this.QuestionnaireIdentity = command.QuestionnaireId;
            InterviewTree changedInterviewTree = this.Tree.Clone();

            this.PutAnswers(changedInterviewTree, command.Answers, command.AssignmentId);
            this.ProtectAnswers(changedInterviewTree, command.QuestionsWithProtectedAnswers);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            this.UpdateTreeWithDependentChanges(changedInterviewTree, questionnaire, entityIdentity: null);
            IReadOnlyCollection<InterviewTreeNodeDiff> treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            //apply events
            this.ApplyEvent(new InterviewCreated(
                command.UserId,
                this.QuestionnaireIdentity.QuestionnaireId,
                this.QuestionnaireIdentity.Version,
                command.AssignmentId,
                questionnaire.IsUsingExpressionStorage(),
                command.AnswersTime));

            this.ApplyEvents(treeDifference, command.UserId);

            this.ApplyEvent(new SupervisorAssigned(command.UserId, command.SupervisorId, command.AnswersTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null, previousStatus: null, utcTime: DateTime.UtcNow));

            if (command.InterviewerId.HasValue)
            {
                this.ApplyEvent(new InterviewerAssigned(command.UserId, command.InterviewerId.Value, command.AnswersTime));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null, previousStatus: InterviewStatus.SupervisorAssigned, utcTime: DateTime.UtcNow));
            }

            if (command.InterviewKey != null && !command.InterviewKey.Equals(this.interviewKey))
            {
                this.ApplyEvent(new InterviewKeyAssigned(command.InterviewKey));
            }

            var defaultTranslation = questionnaire.GetDefaultTransation();
            if (defaultTranslation != null)
            {
                this.SwitchTranslation(new SwitchTranslation(this.EventSourceId, defaultTranslation, command.UserId));
            }
        }

        private void ProtectAnswers(InterviewTree changedInterviewTree, List<Identity> protectedAnswers)
        {
            foreach (var protectedAnswer in protectedAnswers)
            {
                changedInterviewTree.GetQuestion(protectedAnswer).ProtectAnswer();
            }
        }

        protected void PutAnswers(InterviewTree changedInterviewTree,
            IEnumerable<InterviewAnswer> answers,
            int? commandAssignmentId)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            List<InterviewAnswer>[] answersGroupedByLevels = answers
                .GroupBy(x => x.Identity.RosterVector.Length)
                .Select(x => new { Depth = x.Key, Answers = x.ToList() })
                .OrderBy(x => x.Depth)
                .Select(x => x.Answers)
                .ToArray();

            var noAnswersOnQuestionnaireLevel = answersGroupedByLevels.All(x => x.FirstOrDefault()?.Identity.RosterVector.Length != 0);
            if (noAnswersOnQuestionnaireLevel)
                changedInterviewTree.ActualizeTree();

            foreach (var answersInLevel in answersGroupedByLevels)
            {
                foreach (InterviewAnswer answer in answersInLevel)
                {
                    var interviewTreeQuestion = changedInterviewTree.GetQuestion(answer.Identity);
                    // answers were not parsed correctly
                    if (interviewTreeQuestion == null)
                        continue;

                    interviewTreeQuestion.SetAnswer(answer.Answer);

                    interviewTreeQuestion.RunImportInvariantsOrThrow(new InterviewQuestionInvariants(answer.Identity, questionnaire, this.tree));

                    if (commandAssignmentId.HasValue && questionnaire.IsPrefilled(answer.Identity.Id))
                    {
                        interviewTreeQuestion.MarkAsReadonly();
                    }
                }

                changedInterviewTree.ActualizeTree();
            }
        }

        //todo should respect changes calculated in ExpressionState
        public void ReevaluateSynchronizedInterview()
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            ILatestInterviewExpressionState expressionProcessorState = this.ExpressionProcessorStatePrototype.Clone();

            expressionProcessorState.SaveAllCurrentStatesAsPrevious();

            InterviewTree changedInterviewTree = this.Tree.Clone();

            EnablementChanges enablementChanges = expressionProcessorState.ProcessEnablementConditions();
            this.UpdateTreeWithEnablementChanges(changedInterviewTree, enablementChanges);

            ValidityChanges validationChanges = expressionProcessorState.ProcessValidationExpressions();
            this.UpdateTreeWithValidationChanges(changedInterviewTree, validationChanges);

            IReadOnlyCollection<InterviewTreeNodeDiff> treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference);

            if (!this.HasInvalidAnswers())
            {
                this.ApplyEvent(new InterviewDeclaredValid());
            }
        }

        public void RepeatLastInterviewStatus(RepeatLastInterviewStatus command)
        {
            this.ApplyEvent(new InterviewStatusChanged(this.properties.Status, command.Comment, this.properties.Status, DateTime.UtcNow));
        }

        public void SwitchTranslation(SwitchTranslation command)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            IReadOnlyCollection<string> availableLanguages = questionnaire.GetTranslationLanguages();

            if (command.Language != null)
            {
                if (availableLanguages.All(language => language != command.Language))
                    throw new InterviewException(
                        $"Questionnaire does not have translation. Language: {command.Language}. " +
                        $"Interview ID: {this.EventSourceId.FormatGuid()}. " +
                        $"Questionnaire ID: {this.QuestionnaireIdentity}.");
            }

            IQuestionnaire targetQuestionnaire = this.GetQuestionnaireOrThrow(command.Language);

            InterviewTree changedInterviewTree = this.Tree.Clone();
            changedInterviewTree.SwitchQuestionnaire(targetQuestionnaire);

            this.UpdateRosterTitles(changedInterviewTree, targetQuestionnaire);

            IReadOnlyCollection<InterviewTreeNodeDiff> treeDifference = FindDifferenceBetweenTrees(this.Tree, changedInterviewTree);

            this.ApplyEvents(treeDifference, command.UserId);
            this.ApplyEvent(new TranslationSwitched(command.Language, command.UserId));
        }

        public void CommentAnswer(Guid userId, Guid questionId, RosterVector rosterVector, DateTime commentTime, string comment)
        {
            new InterviewPropertiesInvariants(this.properties)
                .RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            new InterviewQuestionInvariants(new Identity(questionId, rosterVector), questionnaire, this.Tree)
                .RequireQuestionExists();

            this.ApplyEvent(new AnswerCommented(userId, questionId, rosterVector, commentTime, comment));
        }

        public void AssignResponsible(AssignResponsibleCommand command)
        {
            var interviewerId = command.InterviewerId;
            var supervisorId = command.SupervisorId;

            if (!interviewerId.HasValue && !supervisorId.HasValue)
            {
                throw new InterviewException(
                    $"Can't assign interview {this.properties.Id} to empty interviewer or supervisor.",
                    InterviewDomainExceptionType.OtherUserIsResponsible);
            }

            var moveWithInTheSameTeam = interviewerId.HasValue && !supervisorId.HasValue;
            if (moveWithInTheSameTeam)
            {
                AssignInterviewer(command.UserId, interviewerId.Value, command.AssignTime);
                return;
            }

            var moveToANewTeamNoInterviewerSpecified = !interviewerId.HasValue;
            if (moveToANewTeamNoInterviewerSpecified)
            {
                AssignSupervisor(command.UserId, supervisorId.Value, command.AssignTime);
                return;
            }

            if (this.properties.SupervisorId != supervisorId && this.properties.InterviewerId == interviewerId)
                throw new InterviewException(
                    $"To change a team, provide new id of supervisor and empty (null) or nor interviewer id (and make sure it belongs to the same team). Currently, you specified a new supervisor {supervisorId} and the same interviewer {interviewerId}.",
                    InterviewDomainExceptionType.OtherUserIsResponsible);

            //within the same team
            if (this.properties.SupervisorId == supervisorId)
            {
                AssignInterviewer(command.UserId, interviewerId.Value, command.AssignTime);
                return;
            }

            // move to other team
            AssignSupervisor(command.UserId, supervisorId.Value, command.AssignTime);
            AssignInterviewer(command.UserId, interviewerId.Value, command.AssignTime);
            
            //AssignResponsible(command.UserId, interviewerId, supervisorId, command.AssignTime);
        }

        public void MoveInterviewToTeam(Guid userId, Guid supervisorId, Guid? interviewerId, DateTime assignTime)
        {
            this.ApplyEvent(new SupervisorAssigned(userId, supervisorId, assignTime));
            if (interviewerId.HasValue)
            {
                this.ApplyEvent(new InterviewerAssigned(userId, interviewerId, assignTime));
            }
            else
            {
                this.ApplyEvent(new InterviewerAssigned(userId, null, null));
            }
        }

        public void AssignSupervisor(Guid userId, Guid supervisorId, DateTime? assignTime = null)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);
            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Created, InterviewStatus.InterviewerAssigned, InterviewStatus.SupervisorAssigned, InterviewStatus.Completed, InterviewStatus.RejectedBySupervisor, InterviewStatus.RejectedByHeadquarters);

            var isInterviewInSupervisorResponsibility =
                this.properties.Status == InterviewStatus.SupervisorAssigned ||
                this.properties.Status == InterviewStatus.RejectedBySupervisor;

            if (isInterviewInSupervisorResponsibility)
                propertiesInvariants.ThrowIfTryAssignToSameSupervisor(supervisorId);

            // assign the supervisor
            this.ApplyEvent(new SupervisorAssigned(userId, supervisorId, assignTime));
            if (this.properties.InterviewerId.HasValue)
            {
                // clear responsible interviewer if set
                this.ApplyEvent(new InterviewerAssigned(userId, null, null));
            }
            if (this.properties.Status == InterviewStatus.Created ||
                this.properties.Status == InterviewStatus.InterviewerAssigned)
            {
                //change status of the interview
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            }
        }

        public void AssignInterviewer(Guid userId, Guid interviewerId, DateTime? assignTime)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);
            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(
                InterviewStatus.SupervisorAssigned, InterviewStatus.InterviewerAssigned, InterviewStatus.Completed,
                InterviewStatus.RejectedBySupervisor, InterviewStatus.RejectedByHeadquarters);

            if (!this.properties.SupervisorId.HasValue)
            {
                // direct assign to the interviewer?
                throw new InterviewException(
                    $"Specify supervisor's id for interviewer {interviewerId} to make direct assignment on interviewer for interview {this.properties.Id}.",
                    InterviewDomainExceptionType.OtherUserIsResponsible);
            }
            propertiesInvariants.ThrowIfTryAssignToSameInterviewer(interviewerId);

            if (this.properties.Status == InterviewStatus.RejectedByHeadquarters)
            {
                this.ApplyEvent(new InterviewRejected(userId, comment: null, rejectTime: assignTime));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedBySupervisor, comment: null, previousStatus: InterviewStatus.RejectedByHeadquarters, utcTime: DateTime.UtcNow));
            }

            this.ApplyEvent(new InterviewerAssigned(userId, interviewerId, assignTime));
            if (this.properties.Status != InterviewStatus.InterviewerAssigned && 
                this.properties.Status != InterviewStatus.RejectedBySupervisor &&
                this.properties.Status != InterviewStatus.Completed)
            {
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            }
        }

        public void Delete(Guid userId)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewWasCompleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(
                InterviewStatus.Created, InterviewStatus.SupervisorAssigned, InterviewStatus.InterviewerAssigned, InterviewStatus.Restored);
            propertiesInvariants.ThrowIfInterviewReceivedByInterviewer();

            this.ApplyEvent(new InterviewDeleted(userId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Deleted, comment: null, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void HardDelete(Guid userId)
        {
            if (this.properties.IsHardDeleted)
                return;

            this.ApplyEvent(new InterviewHardDeleted(userId));
        }

        public void MarkInterviewAsReceivedByInterviwer(Guid userId)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            this.ApplyEvent(new InterviewReceivedByInterviewer());
        }

        public void Restore(Guid userId)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Deleted);

            this.ApplyEvent(new InterviewRestored(userId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Restored, comment: null, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void Restart(Guid userId, string comment, DateTime restartTime)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed);

            this.ApplyEvent(new InterviewRestarted(userId, restartTime, comment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Restarted, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment, previousStatus: InterviewStatus.Restarted, utcTime: DateTime.UtcNow));
        }

        public void Approve(Guid userId, string comment, DateTime approveTime)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed,
                InterviewStatus.RejectedBySupervisor,
                InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewApproved(userId, comment, approveTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedBySupervisor, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void Reject(Guid userId, string comment, DateTime rejectTime)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed,
                InterviewStatus.ApprovedBySupervisor,
                InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewRejected(userId, comment, rejectTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedBySupervisor, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void RejectToInterviewer(Guid userId, Guid interviewerId, string comment, DateTime rejectTime)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed,
                InterviewStatus.ApprovedBySupervisor,
                InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewRejected(userId, comment, rejectTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedBySupervisor, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            this.ApplyEvent(new InterviewerAssigned(userId, interviewerId, rejectTime));
        }

        public void HqApprove(Guid userId, string comment)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed, InterviewStatus.ApprovedBySupervisor, InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewApprovedByHQ(userId, comment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedByHeadquarters, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void UnapproveByHeadquarters(Guid userId, string comment)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.ApprovedByHeadquarters);

            string unapproveCommentMessage = "[Approved by Headquarters was revoked]";
            string unapproveComment = string.IsNullOrEmpty(comment)
                ? unapproveCommentMessage
                : string.Format("{0} \r\n {1}", unapproveCommentMessage, comment);
            this.ApplyEvent(new UnapprovedByHeadquarters(userId, unapproveComment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedBySupervisor, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
        }

        public void RejectInterviewFromHeadquarters(Guid userId,
            Guid supervisorId,
            Guid? interviewerId,
            InterviewSynchronizationDto interviewDto,
            DateTime synchronizationTime)
        {
            var commentedAnswers = (
                from answerDto in interviewDto.Answers
                from answerComment in answerDto.AllComments
                where !this.Tree.GetQuestion(Identity.Create(answerDto.Id, answerDto.QuestionRosterVector)).AnswerComments.Contains(ToAnswerComment(answerComment, answerDto))
                select new
                {
                    UserId = answerComment.UserId,
                    Date = answerComment.Date,
                    Text = answerComment.Text,
                    QuestionId = answerDto.Id,
                    RosterVector = answerDto.QuestionRosterVector
                });

            if (this.properties.Status == InterviewStatus.Deleted)
            {
                this.ApplyEvent(new InterviewRestored(userId));
            }

            this.ApplyEvent(new InterviewRejectedByHQ(userId, interviewDto.Comments));
            this.ApplyEvent(new InterviewStatusChanged(interviewDto.Status, comment: interviewDto.Comments, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));

            if (interviewerId.HasValue)
            {
                this.ApplyEvent(new InterviewerAssigned(userId, interviewerId.Value, synchronizationTime));
            }

            foreach (var commentedAnswer in commentedAnswers)
            {
                this.ApplyEvent(new AnswerCommented(commentedAnswer.UserId, commentedAnswer.QuestionId,
                    commentedAnswer.RosterVector, commentedAnswer.Date, commentedAnswer.Text));
            }
        }

        public void HqReject(Guid userId, string comment)
        {
            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed, InterviewStatus.ApprovedBySupervisor, InterviewStatus.Deleted);

            var isCompleted = this.properties.Status == InterviewStatus.Completed;
            if (isCompleted)
            {
                this.ApplyEvent(new InterviewRejected(userId, comment, DateTime.UtcNow));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedBySupervisor, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            }
            else
            {
                this.ApplyEvent(new InterviewRejectedByHQ(userId, comment));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedByHeadquarters, comment, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));
            }
        }

        public void SynchronizeInterviewEvents(SynchronizeInterviewEventsCommand command)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(command.QuestionnaireId, command.QuestionnaireVersion);

            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            bool isInterviewNeedToBeCreated = command.CreatedOnClient && this.Version == 0;
            var questionnaire = this.GetQuestionnaireOrThrow();

            if (isInterviewNeedToBeCreated)
            {
                if (!(command.SynchronizedEvents.FirstOrDefault() is InterviewOnClientCreated))
                {
                    this.ApplyEvent(new InterviewOnClientCreated(command.UserId, command.QuestionnaireId,
                        command.QuestionnaireVersion, null, questionnaire.IsUsingExpressionStorage()));
                }
            }
            else
            {
                propertiesInvariants.ThrowIfOtherInterviewerIsResponsible(command.UserId);

                if (this.properties.Status == InterviewStatus.Deleted)
                    this.Restore(command.UserId);
                else
                    propertiesInvariants.ThrowIfStatusNotAllowedToBeChangedWithMetadata(command.InterviewStatus);
            }

            foreach (IEvent synchronizedEvent in command.SynchronizedEvents)
            {
                this.ApplyEvent(synchronizedEvent);
            }

            var sourceInterview = this.Tree.Clone();

            this.UpdateTreeWithDependentChanges(this.Tree, questionnaire, null);
            var treeDifference = FindDifferenceBetweenTrees(sourceInterview, this.Tree);
            this.ApplyPassiveEvents(treeDifference);

            if (command.InterviewKey != null)
            {
                this.ApplyEvent(new InterviewKeyAssigned(command.InterviewKey));
            }

            if (command.NewSupervisorId.HasValue)
            {
                this.ApplyEvent(new SupervisorAssigned(command.UserId, command.NewSupervisorId.Value, DateTime.UtcNow));
                this.ApplyEvent(new InterviewerAssigned(command.UserId, command.UserId, DateTime.UtcNow));
            }

            this.ApplyEvent(new InterviewReceivedBySupervisor());
        }

        public void CreateInterviewFromSynchronizationMetadata(Guid id, Guid userId, Guid questionnaireId, long questionnaireVersion,
            InterviewStatus interviewStatus,
            AnsweredQuestionSynchronizationDto[] featuredQuestionsMeta,
            string comments,
            DateTime? rejectedDateTime,
            DateTime? interviewerAssignedDateTime,
            bool valid,
            bool createdOnClient,
            bool usesExpressionStorage = false)
        {
            this.QuestionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);

            InterviewPropertiesInvariants propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            if (this.properties.Status == InterviewStatus.Deleted)
                this.Restore(userId);
            else
                propertiesInvariants.ThrowIfStatusNotAllowedToBeChangedWithMetadata(interviewStatus);

            this.ApplyEvent(new SynchronizationMetadataApplied(userId,
                questionnaireId,
                questionnaireVersion,
                interviewStatus,
                featuredQuestionsMeta,
                createdOnClient,
                comments,
                rejectedDateTime,
                interviewerAssignedDateTime,
                usesExpressionStorage));

            this.ApplyEvent(new InterviewStatusChanged(interviewStatus, comments, previousStatus: this.properties.Status, utcTime: DateTime.UtcNow));

            if (valid)
                this.ApplyEvent(new InterviewDeclaredValid());
            else
                this.ApplyEvent(new InterviewDeclaredInvalid());
        }

        #endregion

        #region Tree


        #endregion

        #region Events raising

        protected void ApplyPassiveEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var diffByQuestions = diff.OfType<InterviewTreeQuestionDiff>().ToList();
            var questionsWithChangedOptionsSet = diffByQuestions.Where(x => x.AreLinkedOptionsChanged).ToArray();
            var questionsWithChangedLinkedToListOptionsSet = diffByQuestions.Where(x => x.AreLinkedToListOptionsChanged).ToArray();
            var changedVariables = diff.OfType<InterviewTreeVariableDiff>().ToArray();

            this.ApplyEnablementEvents(diff);
            this.ApplyValidityEvents(diff);
            this.ApplyVariableEvents(changedVariables);
            this.ApplyLinkedOptionsChangesEvents(questionsWithChangedOptionsSet);
            this.ApplyLinkedToListOptionsChangesEvents(questionsWithChangedLinkedToListOptionsSet);
            this.ApplySubstitutionEvents(diff);
            this.ApplyReadonlyStateEvents(diff);
            this.ApplyProtectedAnswers(diff);
        }

        protected void ApplyEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff, Guid? responsibleId = null)
        {
            var diffByQuestions = diff.OfType<InterviewTreeQuestionDiff>().ToList();
            var questionsWithRemovedAnswer = diffByQuestions.Where(x => x.IsAnswerRemoved).ToArray();
            var questionsWithChangedAnswer = diffByQuestions.Where(x => x.IsAnswerChanged).ToArray();

            var changedRosters = diff.OfType<InterviewTreeRosterDiff>().ToArray();

            var questionsRosterLevels = questionsWithChangedAnswer.Select(x => x.Identity.RosterVector).ToHashSet();
            var rosterLevels = changedRosters.Select(x => x.Identity.RosterVector).ToHashSet();
            var intersection = questionsRosterLevels.Intersect(rosterLevels).ToList();

            var aggregatedEventsShouldBeFired = intersection.Count == 0;
            if (aggregatedEventsShouldBeFired)
            {
                this.ApplyUpdateAnswerEvents(questionsWithChangedAnswer, responsibleId.Value);
                this.ApplyRosterEvents(changedRosters);
            }
            else
            {
                var maxDepth = Math.Max(questionsRosterLevels.Max(x => x.Length), rosterLevels.Select(x => x.Length).Max());
                // events fired by levels (from the questionnaire level down to nested rosters)
                for (int i = 0; i <= maxDepth; i++)
                {
                    this.ApplyUpdateAnswerEvents(questionsWithChangedAnswer.Where(x => x.Identity.RosterVector.Length == i).ToArray(), responsibleId.Value);
                    this.ApplyRosterEvents(changedRosters.Where(x => x.Identity.RosterVector.Length == i + 1).ToArray());
                }
            }
            this.ApplyRemoveAnswerEvents(questionsWithRemovedAnswer);
            this.ApplyPassiveEvents(diff); 
        }

        private void ApplySubstitutionEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var groupsWithChangedTitles = diff.OfType<InterviewTreeGroupDiff>().Where(x => x.IsTitleChanged).Select(x => x.ChangedNode.Identity).ToArray();
            var questionsWithChangedTitles = diff.OfType<InterviewTreeQuestionDiff>().Where(x => x.IsTitleChanged || x.AreValidationMessagesChanged).Select(x => x.ChangedNode.Identity).ToArray();
            var staticTextsWithChangedTitles = diff.OfType<InterviewTreeStaticTextDiff>().Where(x => x.IsTitleChanged || x.AreValidationMessagesChanged).Select(x => x.ChangedNode.Identity).ToArray();

            if (groupsWithChangedTitles.Any() || questionsWithChangedTitles.Any() || staticTextsWithChangedTitles.Any())
            {
                this.ApplyEvent(new SubstitutionTitlesChanged(questionsWithChangedTitles, staticTextsWithChangedTitles, groupsWithChangedTitles));
            }
        }

        private void ApplyLinkedOptionsChangesEvents(InterviewTreeQuestionDiff[] questionsWithChangedOptionsSet)
        {
            var changedLinkedOptions = questionsWithChangedOptionsSet
                .Select(x => new ChangedLinkedOptions(x.ChangedNode.Identity, x.ChangedNode.AsLinked.Options.ToArray()))
                .ToArray();

            if (changedLinkedOptions.Any())
            {
                this.ApplyEvent(new LinkedOptionsChanged(changedLinkedOptions));
            }
        }

        private void ApplyLinkedToListOptionsChangesEvents(InterviewTreeQuestionDiff[] questionsWithChangedOptionsSet)
        {
            var changedLinkedOptions = questionsWithChangedOptionsSet
                .Select(x => new ChangedLinkedToListOptions(x.ChangedNode.Identity, x.ChangedNode.AsLinkedToList.Options.Select(Convert.ToDecimal).ToArray()))
                .ToArray();

            if (changedLinkedOptions.Any())
            {
                this.ApplyEvent(new LinkedToListOptionsChanged(changedLinkedOptions));
            }
        }

        private void ApplyVariableEvents(InterviewTreeVariableDiff[] diffsByChangedVariables)
        {
            var changedVariables = diffsByChangedVariables.Where(x => x.ChangedNode != null && x.IsValueChanged).Select(ToChangedVariable).ToArray();

            if (changedVariables.Any())
                this.ApplyEvent(new VariablesChanged(changedVariables));
        }

        private void ApplyValidityEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null).ToList();

            var allChangedQuestionDiffs = allNotNullableNodes.OfType<InterviewTreeQuestionDiff>().ToList();
            var allChangedStaticTextDiffs = allNotNullableNodes.OfType<InterviewTreeStaticTextDiff>().ToList();

            var validQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecameValid).Select(x => x.ChangedNode.Identity).ToArray();
            var invalidQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecameInvalid || x.IsFailedErrorValidationIndexChanged).Select(x => x.ChangedNode)
                .ToDictionary(x => x.Identity, x => x.FailedErrors);

            var plausibleQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecamePlausibled).Select(x => x.ChangedNode.Identity).ToArray();
            var implausibleQuestionIdentities = allChangedQuestionDiffs.Where(x => x.ChangedNodeBecameImplausibled || x.IsFailedWarningValidationIndexChanged).Select(x => x.ChangedNode)
                //.ToDictionary(x => x.Identity, x => x.FailedWarnings);
                .Select(x => new KeyValuePair<Identity, IReadOnlyList<FailedValidationCondition>>(x.Identity, x.FailedWarnings))
                .ToList();

            var validStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecameValid).Select(x => x.ChangedNode.Identity).ToArray();
            var invalidStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecameInvalid || x.IsFailedErrorValidationIndexChanged).Select(x => x.ChangedNode)
                .Select(x => new KeyValuePair<Identity, IReadOnlyList<FailedValidationCondition>>(x.Identity, x.FailedErrors))
                .ToList();

            var plausibleStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecamePlausibled).Select(x => x.ChangedNode.Identity).ToArray();
            var implausibleStaticTextIdentities = allChangedStaticTextDiffs.Where(x => x.ChangedNodeBecameImplausibled || x.IsFailedWarningValidationIndexChanged).Select(x => x.ChangedNode)
                //.ToDictionary(x => x.Identity, x => x.FailedWarnings);
                .Select(x => new KeyValuePair<Identity, IReadOnlyList<FailedValidationCondition>>(x.Identity, x.FailedWarnings))
                .ToList();


            if (validQuestionIdentities.Any()) this.ApplyEvent(new AnswersDeclaredValid(validQuestionIdentities));
            if (invalidQuestionIdentities.Any()) this.ApplyEvent(new AnswersDeclaredInvalid(invalidQuestionIdentities));

            if (plausibleQuestionIdentities.Any()) this.ApplyEvent(new AnswersDeclaredPlausible(plausibleQuestionIdentities));
            if (implausibleQuestionIdentities.Any()) this.ApplyEvent(new AnswersDeclaredImplausible(implausibleQuestionIdentities));

            if (validStaticTextIdentities.Any()) this.ApplyEvent(new StaticTextsDeclaredValid(validStaticTextIdentities));
            if (invalidStaticTextIdentities.Any()) this.ApplyEvent(new StaticTextsDeclaredInvalid(invalidStaticTextIdentities));

            if (plausibleStaticTextIdentities.Any()) this.ApplyEvent(new StaticTextsDeclaredPlausible(plausibleStaticTextIdentities));
            if (implausibleStaticTextIdentities.Any()) this.ApplyEvent(new StaticTextsDeclaredImplausible(implausibleStaticTextIdentities));

            if (HasInvalidAnswers() || HasInvalidStaticTexts)
            {
                if (this.properties.IsValid)
                {
                    this.ApplyEvent(new InterviewDeclaredInvalid());
                }
            }
            else if(!this.properties.IsValid)
            {
                this.ApplyEvent(new InterviewDeclaredValid());
            }
        }

        private void ApplyEnablementEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null).ToList();

            var diffByGroups = allNotNullableNodes.OfType<InterviewTreeGroupDiff>().ToList();
            var diffByQuestions = allNotNullableNodes.OfType<InterviewTreeQuestionDiff>().ToList();
            var diffByStaticTexts = allNotNullableNodes.OfType<InterviewTreeStaticTextDiff>().ToList();
            var diffByVariables = allNotNullableNodes.OfType<InterviewTreeVariableDiff>().ToList();

            var disabledGroups = diffByGroups.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledGroups = diff.OfType<InterviewTreeGroupDiff>().Where(x => x.IsNodeEnabled).Select(x => (x.ChangedNode ?? x.SourceNode).Identity).ToArray();

            var disabledQuestions = diffByQuestions.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledQuestions = diff.OfType<InterviewTreeQuestionDiff>().Where(x => x.IsNodeEnabled).Select(x => (x.ChangedNode ?? x.SourceNode).Identity).ToArray();

            var disabledStaticTexts = diffByStaticTexts.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledStaticTexts = diffByStaticTexts.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            var disabledVariables = diffByVariables.Where(x => x.IsNodeDisabled).Select(x => x.ChangedNode.Identity).ToArray();
            var enabledVariables = diffByVariables.Where(x => x.IsNodeEnabled).Select(x => x.ChangedNode.Identity).ToArray();

            if (disabledGroups.Any()) this.ApplyEvent(new GroupsDisabled(disabledGroups));
            if (enabledGroups.Any()) this.ApplyEvent(new GroupsEnabled(enabledGroups));
            if (disabledQuestions.Any()) this.ApplyEvent(new QuestionsDisabled(disabledQuestions));
            if (enabledQuestions.Any()) this.ApplyEvent(new QuestionsEnabled(enabledQuestions));
            if (disabledStaticTexts.Any()) this.ApplyEvent(new StaticTextsDisabled(disabledStaticTexts));
            if (enabledStaticTexts.Any()) this.ApplyEvent(new StaticTextsEnabled(enabledStaticTexts));
            if (disabledVariables.Any()) this.ApplyEvent(new VariablesDisabled(disabledVariables));
            if (enabledVariables.Any()) this.ApplyEvent(new VariablesEnabled(enabledVariables));
        }

        private void ApplyReadonlyStateEvents(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null).ToList();
            var diffByQuestions = allNotNullableNodes.OfType<InterviewTreeQuestionDiff>().ToList();
            var readonlyQuestions = diffByQuestions.Where(x => x.NodeIsMarkedAsReadonly).Select(x => x.ChangedNode.Identity).ToArray();
            if (readonlyQuestions.Any()) this.ApplyEvent(new QuestionsMarkedAsReadonly(readonlyQuestions));
        }

        private void ApplyProtectedAnswers(IReadOnlyCollection<InterviewTreeNodeDiff> diff)
        {
            var allNotNullableNodes = diff.Where(x => x.ChangedNode != null)
                                          .OfType<InterviewTreeQuestionDiff>()
                                          .Where(x => x.AnswersMarkedAsProtected)
                                          .Select(x => x.ChangedNode)
                                          .ToList();
            if (allNotNullableNodes.Count > 0)
            {
                var @event = new AnswersMarkedAsProtected(allNotNullableNodes.Select(x => x.Identity).ToArray());
                this.ApplyEvent(@event);
            }
        }

        private void ApplyUpdateAnswerEvents(InterviewTreeQuestionDiff[] diffByQuestions, Guid responsibleId)
        {
            foreach (var diffByQuestion in diffByQuestions)
            {
                var changedQuestion = diffByQuestion.ChangedNode;

                if (changedQuestion == null) continue;

                if (!changedQuestion.IsAnswered()) continue;

                if (changedQuestion.IsText)
                {
                    this.ApplyEvent(new TextQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeTextQuestion().GetAnswer().Value));
                }

                else if (changedQuestion.IsTextList)
                {
                    this.ApplyEvent(new TextListQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeTextListQuestion().GetAnswer().ToTupleArray()));
                }

                else if (changedQuestion.IsDouble)
                {
                    this.ApplyEvent(new NumericRealQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, (decimal)changedQuestion.GetAsInterviewTreeDoubleQuestion().GetAnswer().Value));
                }

                else if (changedQuestion.IsInteger)
                {
                    this.ApplyEvent(new NumericIntegerQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeIntegerQuestion().GetAnswer().Value));
                }

                else if (changedQuestion.IsDateTime)
                {
                    this.ApplyEvent(new DateTimeQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeDateTimeQuestion().GetAnswer().Value));
                }

                else if (changedQuestion.IsGps)
                {
                    var gpsAnswer = changedQuestion.GetAsInterviewTreeGpsQuestion().GetAnswer().Value;
                    this.ApplyEvent(new GeoLocationQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, gpsAnswer.Latitude, gpsAnswer.Longitude,
                        gpsAnswer.Accuracy, gpsAnswer.Altitude, gpsAnswer.Timestamp));
                }

                else if (changedQuestion.IsQRBarcode)
                {
                    this.ApplyEvent(new QRBarcodeQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeQRBarcodeQuestion().GetAnswer().DecodedText));
                }

                else if (changedQuestion.IsMultimedia)
                {
                    this.ApplyEvent(new PictureQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeMultimediaQuestion().GetAnswer().FileName));
                }

                else if (changedQuestion.IsYesNo)
                {
                    this.ApplyEvent(new YesNoQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeYesNoQuestion().GetAnswer().ToAnsweredYesNoOptions().ToArray()));
                }

                else if (changedQuestion.IsSingleFixedOption || changedQuestion.IsCascading)
                {
                    this.ApplyEvent(new SingleOptionQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeSingleOptionQuestion().GetAnswer().SelectedValue));
                }

                else if (changedQuestion.IsMultiFixedOption)
                {
                    this.ApplyEvent(new MultipleOptionsQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeMultiOptionQuestion().GetAnswer().ToDecimals().ToArray()));
                }

                else if (changedQuestion.IsSingleLinkedOption)
                {
                    this.ApplyEvent(new SingleOptionLinkedQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeSingleLinkedToRosterQuestion().GetAnswer().SelectedValue));
                }

                else if (changedQuestion.IsMultiLinkedOption)
                {
                    this.ApplyEvent(new MultipleOptionsLinkedQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow,
                        changedQuestion.GetAsInterviewTreeMultiLinkedToRosterQuestion().GetAnswer().ToRosterVectorArray().Select(x => x.Select(Convert.ToDecimal).ToArray()).ToArray()));
                }

                else if (changedQuestion.IsSingleLinkedToList)
                {
                    this.ApplyEvent(new SingleOptionQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeSingleOptionLinkedToListQuestion().GetAnswer().SelectedValue));
                }

                else if (changedQuestion.IsMultiLinkedToList)
                {
                    this.ApplyEvent(new MultipleOptionsQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, changedQuestion.GetAsInterviewTreeMultiOptionLinkedToListQuestion().GetAnswer().ToDecimals().ToArray()));
                }
                else if (changedQuestion.IsArea)
                {
                    var answer = changedQuestion.GetAsInterviewTreeAreaQuestion().GetAnswer().Value;
                    this.ApplyEvent(new AreaQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                        changedQuestion.Identity.RosterVector, DateTime.UtcNow, answer.Geometry, answer.MapName, answer.AreaSize, answer.Length,
                        answer.Coordinates, answer.DistanceToEditor));
                }

                else if (changedQuestion.IsAudio)
                {
                    var audioAnswer = changedQuestion.GetAsInterviewTreeAudioQuestion().GetAnswer();

                    this.ApplyEvent(new AudioQuestionAnswered(responsibleId, changedQuestion.Identity.Id,
                       changedQuestion.Identity.RosterVector, DateTime.UtcNow,
                       audioAnswer.FileName, audioAnswer.Length));
                }
            }
        }

        private void ApplyRemoveAnswerEvents(InterviewTreeQuestionDiff[] diffByQuestions)
        {
            var questionIdentittiesWithRemovedAnswer = diffByQuestions.Select(x => x.SourceNode.Identity).ToArray();

            if (questionIdentittiesWithRemovedAnswer.Any())
                this.ApplyEvent(new AnswersRemoved(questionIdentittiesWithRemovedAnswer));
        }

        private void ApplyRosterEvents(InterviewTreeRosterDiff[] diff)
        {
            var removedRosters = diff.Where(x => x.IsNodeRemoved).Select(x => x.SourceNode).ToArray();
            var addedRosters = diff.Where(x => x.IsNodeAdded).Select(x => x.ChangedNode).ToArray();
            var changedRosterTitles = diff.Where(x => x.IsRosterTitleChanged).Select(x => x.ChangedNode).ToArray();

            if (removedRosters.Any())
                this.ApplyEvent(new RosterInstancesRemoved(removedRosters.Select(ToRosterInstance).ToArray()));

            if (addedRosters.Any())
                this.ApplyEvent(new RosterInstancesAdded(addedRosters.OrderBy(x => x.RosterVector.Length).Select(ToAddedRosterInstance).ToArray()));

            if (changedRosterTitles.Any())
                this.ApplyEvent(new RosterInstancesTitleChanged(changedRosterTitles.Select(ToChangedRosterInstanceTitleDto).ToArray()));
        }

        private static ChangedRosterInstanceTitleDto ToChangedRosterInstanceTitleDto(InterviewTreeRoster roster)
            => new ChangedRosterInstanceTitleDto(ToRosterInstance(roster), roster.RosterTitle);

        private static AddedRosterInstance ToAddedRosterInstance(IInterviewTreeNode rosterNode)
            => AddedRosterInstance.CreateFromIdentityAndSortIndex(rosterNode.Identity, (rosterNode as InterviewTreeRoster)?.SortIndex);

        private static RosterInstance ToRosterInstance(IInterviewTreeNode rosterNode)
            => RosterInstance.CreateFromIdentity(rosterNode.Identity);

        private static ChangedVariable ToChangedVariable(InterviewTreeVariableDiff variable)
            => new ChangedVariable(variable.ChangedNode.Identity, variable.ChangedNode.Value);

        #endregion

        private static AnswerComment ToAnswerComment(CommentSynchronizationDto answerComment,
            AnsweredQuestionSynchronizationDto answerDto)
            => new AnswerComment(answerComment.UserId, answerComment.UserRole, answerComment.Date, answerComment.Text,
                Identity.Create(answerDto.Id, answerDto.QuestionRosterVector));

        public InterviewTreeMultiLinkedToRosterQuestion GetLinkedMultiOptionQuestion(Identity identity) =>
            this.Tree.GetQuestion(identity).GetAsInterviewTreeMultiLinkedToRosterQuestion();

        public InterviewTreeSingleLinkedToRosterQuestion GetLinkedSingleOptionQuestion(Identity identity) =>
            this.Tree.GetQuestion(identity).GetAsInterviewTreeSingleLinkedToRosterQuestion();

        public string GetLinkedOptionTitle(Identity linkedQuestionIdentity, RosterVector option)
        {
            InterviewTreeQuestion linkedQuestion = this.Tree.GetQuestion(linkedQuestionIdentity);
            if (!linkedQuestion.IsLinked) return string.Empty;

            Identity sourceIdentity = Identity.Create(linkedQuestion.AsLinked.LinkedSourceId, option);

            IInterviewTreeNode sourceNode = this.Tree.GetNodeByIdentity(sourceIdentity);

            string optionTitle = string.Empty;
            int skipBreadcrumsThreshold = 1;
            if (sourceNode is InterviewTreeRoster)
            {
                InterviewTreeRoster sourceRoster = sourceNode as InterviewTreeRoster;
                optionTitle = sourceRoster.RosterTitle;
                skipBreadcrumsThreshold = 0;
            }
            if (sourceNode is InterviewTreeQuestion)
            {
                InterviewTreeQuestion sourceQuestion = sourceNode as InterviewTreeQuestion;
                optionTitle = sourceQuestion.GetAnswerAsString();
            }

            InterviewTreeRoster[] sourceBreadcrumbsOfRosterTitles = sourceNode.Parents.OfType<InterviewTreeRoster>().ToArray();
            InterviewTreeRoster[] linkedBreadcrumbsOfRosterTitles = linkedQuestion.Parents.OfType<InterviewTreeRoster>().ToArray();

            int common = sourceBreadcrumbsOfRosterTitles.Zip(linkedBreadcrumbsOfRosterTitles, (x, y) => x.RosterSizeId.Equals(y.RosterSizeId) ? x : null).TakeWhile(x => x != null).Count();

            string[] breadcrumbsOfRosterTitles = sourceBreadcrumbsOfRosterTitles.Skip(common).Select(x => x.RosterTitle).ToArray();

            string breadcrumbs = string.Join(": ", breadcrumbsOfRosterTitles);

            return breadcrumbsOfRosterTitles.Length > skipBreadcrumsThreshold
                ? $"{breadcrumbs}: {optionTitle}"
                : optionTitle;
        }

        public IEnumerable<Identity> GetUnderlyingInterviewerEntities(Identity sectionId)
        {
            InterviewTreeGroup interviewTreeGroup = this.Tree.GetGroup(sectionId);
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();
            IEnumerable<IInterviewTreeNode> result = interviewTreeGroup.Children
                     .Except(x => questionnaire.IsQuestion(x.Identity.Id) && !questionnaire.IsInterviewierQuestion(x.Identity.Id));

            return result.Select(x => x.Identity);
        }

        protected bool HasInvalidAnswers()
            => this.Tree.FindQuestions().Any(question => !question.IsValid && !question.IsDisabled());

        protected bool HasInvalidStaticTexts
            => this.Tree.FindStaticTexts().Any(staticText => !staticText.IsValid && !staticText.IsDisabled());

        protected static IReadOnlyCollection<InterviewTreeNodeDiff> FindDifferenceBetweenTrees(InterviewTree sourceInterview, InterviewTree changedInterview)
        {
            return sourceInterview.Compare(changedInterview);
        }

        public static bool TestingConditions = false;

        protected void UpdateTreeWithDependentChanges(InterviewTree changedInterviewTree, IQuestionnaire questionnaire, Identity entityIdentity, bool removeLinkedAnswers = true)
        {
            if (questionnaire.IsUsingExpressionStorage())
            {
                InterviewTree interviewTreeCloneForTesting = null;

                if (TestingConditions)
                {
                    interviewTreeCloneForTesting = changedInterviewTree.Clone();

                    IInterviewExpressionStorage expressionStorageForTesting = this.GetExpressionStorage();
                    var interviewPropertiesForExpressionsForTesting = new InterviewPropertiesForExpressions(new InterviewProperties(this.EventSourceId), this.properties);
                    expressionStorageForTesting.Initialize(new InterviewStateForExpressions(interviewTreeCloneForTesting, interviewPropertiesForExpressionsForTesting));
                    using (var updaterForTesting = new InterviewTreeUpdater(expressionStorageForTesting, questionnaire, removeLinkedAnswers))
                    {
                        //GC.Collect();

                        //using (GlobalStopwatcher.Scope("old condutions run"))
                        {
                            var playOrderForTesting = questionnaire.GetExpressionsPlayOrder();
                            PlayActionForEachNodeInOrderForTesting(playOrderForTesting, node => node.Accept(updaterForTesting));
                            PlayActionForEachNodeInOrderForTesting(playOrderForTesting, node => (node as IInterviewTreeValidateable)?.AcceptValidity(updaterForTesting));
                        }

                        void PlayActionForEachNodeInOrderForTesting(List<Guid> playOrder, Action<IInterviewTreeNode> action)
                        {
                            foreach (var entityId in playOrder)
                            {
                                var entityIdentities = interviewTreeCloneForTesting.FindEntity(entityId).ToList();

                                foreach (var entity in entityIdentities)
                                {
                                    var changedNode = interviewTreeCloneForTesting.GetNodeByIdentity(entity.Identity);
                                    if (changedNode != null)
                                    {
                                        action(changedNode);
                                    }
                                }
                            }
                        }
                    }
                }


                IInterviewExpressionStorage expressionStorage = this.GetExpressionStorage();
                var interviewPropertiesForExpressions = new InterviewPropertiesForExpressions(new InterviewProperties(this.EventSourceId), this.properties);
                expressionStorage.Initialize(new InterviewStateForExpressions(changedInterviewTree, interviewPropertiesForExpressions));
                using (var updater = new InterviewTreeUpdater(expressionStorage, questionnaire, removeLinkedAnswers))
                {
                    if (questionnaire.SupportsExpressionsGraph() && entityIdentity != null)
                    {
                        //GC.Collect();

                        //using (GlobalStopwatcher.Scope("new condutions run"))
                        {
                            var expressionsPlayOrder = questionnaire.GetExpressionsPlayOrder(entityIdentity.Id);
                            PlayActionForEachNodeInOrder(expressionsPlayOrder, node => node.Accept(updater));
                            var validityExpressionsPlayOrder = questionnaire.GetValidationExpressionsPlayOrder(expressionsPlayOrder);
                            PlayActionForEachNodeInOrder(validityExpressionsPlayOrder, node => (node as IInterviewTreeValidateable)?.AcceptValidity(updater));
                        }

                        if (TestingConditions)
                        {
                            var treeDifference = FindDifferenceBetweenTrees(changedInterviewTree, interviewTreeCloneForTesting);
                            if (treeDifference.Count > 0)
                            {
                                throw new InterviewException($"ERROR!!! - Found {treeDifference.Count} diffs with changes. First ID {treeDifference.First().Identity}, type: {treeDifference.First().GetType().Name}", InterviewDomainExceptionType.ExpessionCalculationError);

                                /*foreach (var diff in treeDifference)
                                {
                                    bool isChangesExists = diff.IsNodeAdded 
                                        || diff.IsNodeDisabled
                                        || diff.IsNodeEnabled
                                        || diff.IsNodeRemoved
                                        || ((diff as InterviewTreeRosterDiff)?.IsRosterTitleChanged ?? false)
                                        || ((diff as InterviewTreeGroupDiff)?.IsTitleChanged ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.IsTitleChanged ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.AreLinkedOptionsChanged ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.AreLinkedToListOptionsChanged ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.IsAnswerChanged ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.IsAnswerRemoved ?? false)
                                        || ((diff as InterviewTreeQuestionDiff)?.NodeIsMarkedAsReadonly ?? false)
                                        || ((diff as InterviewTreeValidateableDiff)?.AreValidationMessagesChanged ?? false)
                                        || ((diff as InterviewTreeValidateableDiff)?.ChangedNodeBecameInvalid ?? false)
                                        || ((diff as InterviewTreeValidateableDiff)?.ChangedNodeBecameValid ?? false)
                                        || ((diff as InterviewTreeValidateableDiff)?.IsFailedErrorValidationIndexChanged ?? false)
                                        || ((diff as InterviewTreeVariableDiff)?.IsValueChanged ?? false)
                                        || ((diff as InterviewTreeStaticTextDiff)?.IsTitleChanged ?? false);

                                    if (isChangesExists)
                                        //Debug.WriteLine($"ERROR!!! - Found {diff.Identity} with changes");
                                        throw new InterviewException($"ERROR!!! - Found {diff.Identity} with changes");
                                }*/


                            }

                            //GlobalStopwatcher.DumpToDebug();
                        }
                    }
                    else
                    {
                        var playOrder = questionnaire.GetExpressionsPlayOrder();
                        PlayActionForEachNodeInOrder(playOrder, node => node.Accept(updater));
                        PlayActionForEachNodeInOrder(playOrder, node => (node as IInterviewTreeValidateable)?.AcceptValidity(updater));
                    }

                    void PlayActionForEachNodeInOrder(List<Guid> playOrder, Action<IInterviewTreeNode> action)
                    {
                        foreach (var entityId in playOrder)
                        {
                            var entityIdentities = changedInterviewTree.FindEntity(entityId).ToList();

                            foreach (var entity in entityIdentities)
                            {
                                var changedNode = changedInterviewTree.GetNodeByIdentity(entity.Identity);
                                if (changedNode != null)
                                {
                                    action(changedNode);
                                }
                            }
                        }
                    }

                    this.UpdateRosterTitles(changedInterviewTree, questionnaire);
                    this.UpdateLinkedQuestions(changedInterviewTree, updater);
                }
            }
            else
            {
                this.UpdateTreeWithDependentChangesWithExpressionState(changedInterviewTree, questionnaire);
            }
            
            changedInterviewTree.ReplaceSubstitutions();
        }

        private void UpdateTreeWithDependentChangesWithExpressionState(InterviewTree changedInterviewTree,
            IQuestionnaire questionnaire)
        {
            ILatestInterviewExpressionState expressionProcessorState = this.GetClonedExpressionState();

            this.UpdateExpressionState(changedInterviewTree, expressionProcessorState);

            EnablementChanges enablementChanges = expressionProcessorState.ProcessEnablementConditions();

            this.UpdateTreeWithEnablementChanges(changedInterviewTree, enablementChanges);

            StructuralChanges structuralChanges = expressionProcessorState.GetStructuralChanges();
            this.UpdateTreeWithStructuralChanges(changedInterviewTree, structuralChanges);

            this.UpdateRosterTitles(changedInterviewTree, questionnaire);

            this.UpdateLinkedQuestions(changedInterviewTree, expressionProcessorState);

            VariableValueChanges variableValueChanges = expressionProcessorState.ProcessVariables();
            this.UpdateTreeWithVariableChanges(changedInterviewTree, variableValueChanges);

            ValidityChanges validationChanges = expressionProcessorState.ProcessValidationExpressions();
            this.UpdateTreeWithValidationChanges(changedInterviewTree, validationChanges);
        }

        private void UpdateTreeWithVariableChanges(InterviewTree tree, VariableValueChanges variableValueChanges)
            => variableValueChanges?.ChangedVariableValues.ForEach(x => tree.GetVariable(x.Key).SetValue(x.Value));

        private void UpdateTreeWithValidationChanges(InterviewTree tree, ValidityChanges validationChanges)
        {
            if (validationChanges == null) return; // can be in tests only.

            validationChanges.AnswersDeclaredValid.ForEach(x => tree.GetQuestion(x).MarkValid());
            validationChanges.AnswersDeclaredInvalid.ForEach(x => tree.GetQuestion(x).MarkInvalid(new FailedValidationCondition(0).ToEnumerable()));
            validationChanges.FailedValidationConditionsForQuestions.ForEach(x => tree.GetQuestion(x.Key).MarkInvalid(x.Value));

            validationChanges.StaticTextsDeclaredValid.ForEach(x => tree.GetStaticText(x).MarkValid());
            validationChanges.FailedValidationConditionsForStaticTexts.ForEach(x => tree.GetStaticText(x.Key).MarkInvalid(x.Value));
        }

        private void UpdateTreeWithEnablementChanges(InterviewTree tree, EnablementChanges enablementChanges)
        {
            if (enablementChanges == null) return; // can be in tests only.

            enablementChanges.QuestionsToBeDisabled.ForEach(x => tree.GetQuestion(x).Disable());
            enablementChanges.QuestionsToBeEnabled.ForEach(x => tree.GetQuestion(x).Enable());

            enablementChanges.GroupsToBeDisabled.ForEach(x => tree.GetGroup(x).Disable());
            enablementChanges.GroupsToBeEnabled.ForEach(x => tree.GetGroup(x).Enable());

            enablementChanges.StaticTextsToBeDisabled.ForEach(x => tree.GetStaticText(x).Disable());
            enablementChanges.StaticTextsToBeEnabled.ForEach(x => tree.GetStaticText(x).Enable());

            enablementChanges.VariablesToBeDisabled.ForEach(x => tree.GetVariable(x).Disable());
            enablementChanges.VariablesToBeEnabled.ForEach(x => tree.GetVariable(x).Enable());
        }

        protected void UpdateRosterTitles(InterviewTree tree, IQuestionnaire questionnaire)
        {
            foreach (InterviewTreeRoster roster in tree.FindRosters())
            {
                if (roster.IsFixed)
                {
                    string changedRosterTitle = questionnaire.GetFixedRosterTitle(roster.Identity.Id,
                        roster.Identity.RosterVector.Last());

                    roster.SetRosterTitle(changedRosterTitle);
                }
                else
                {
                    roster.UpdateRosterTitle((questionId, answerOptionValue) =>
                        questionnaire.GetOptionForQuestionByOptionValue(questionId, answerOptionValue).Title);
                }
            }
        }

        protected void UpdateLinkedQuestions(InterviewTree interviewTree, InterviewTreeUpdater updater)
        {
            foreach (InterviewTreeQuestion question in interviewTree.FindQuestions())
            {
                if (question.IsLinked)
                {
                    updater.UpdateLinkedQuestion(question);
                }
                else if (question.IsLinkedToListQuestion)
                {
                    updater.UpdateLinkedToListQuestion(question);
                }
            }
        }

        protected void UpdateLinkedQuestions(InterviewTree interviewTree, ILatestInterviewExpressionState interviewExpressionState, bool updateAnswersIfOptionsSetChanged = true)
        {
            bool expressionStateSupportLinkedOptionsCalculation = interviewExpressionState.AreLinkedQuestionsSupported();
            if (expressionStateSupportLinkedOptionsCalculation)
            {
                LinkedQuestionOptionsChanges processLinkedQuestionFilters = interviewExpressionState.ProcessLinkedQuestionFilters();

                foreach (KeyValuePair<Identity, RosterVector[]> linkedQuestionWithOptions in processLinkedQuestionFilters.LinkedQuestionOptionsSet)
                {
                    InterviewTreeQuestion linkedQuestion = interviewTree.GetQuestion(linkedQuestionWithOptions.Key);
                    linkedQuestion.UpdateLinkedOptionsAndUpdateAnswerIfNeeded(linkedQuestionWithOptions.Value, updateAnswersIfOptionsSetChanged);
                }

                // backward compatibility with old assemblies
                UpdateLinkedQuestionsCalculatedByObsoleteAlgorythm(interviewTree, processLinkedQuestionFilters);
            }
            else
            {
                // backward compatibility if assembly cannot process linked questions
                CalculateLinkedOptionsOnTree(interviewTree);
            }

            CalculateLinkedToListOptionsOnTree(interviewTree, interviewExpressionState);
        }

        [Obsolete("v 5.10, release 01 jul 16")]
        private static void UpdateLinkedQuestionsCalculatedByObsoleteAlgorythm(InterviewTree tree, LinkedQuestionOptionsChanges processLinkedQuestionFilters)
        {
            foreach (KeyValuePair<Guid, RosterVector[]> linkedQuestionWithOptions in processLinkedQuestionFilters.LinkedQuestionOptions)
            {
                tree.FindQuestions(linkedQuestionWithOptions.Key)
                    .ForEach(x => x.AsLinked.SetOptions(linkedQuestionWithOptions.Value));
            }
        }

        private static void CalculateLinkedOptionsOnTree(InterviewTree tree)
        {
            IEnumerable<InterviewTreeQuestion> linkedQuestions = tree.FindQuestions().Where(x => x.IsLinked);
            foreach (InterviewTreeQuestion linkedQuestion in linkedQuestions)
            {
                var options = linkedQuestion.GetCalculatedLinkedOptions();
                if (options != null)
                    linkedQuestion.UpdateLinkedOptionsAndUpdateAnswerIfNeeded(options.Select(x => x.Option).ToArray());
            }
        }

        protected static void CalculateLinkedToListOptionsOnTree(InterviewTree tree, ILatestInterviewExpressionState interviewExpressionState, bool resetAnswerOnOptionChange = true)
        {
            IEnumerable<InterviewTreeQuestion> linkedToListQuestions = tree.FindQuestions().Where(x => x.IsLinkedToListQuestion);
            foreach (InterviewTreeQuestion linkedQuestion in linkedToListQuestions)
            {
                linkedQuestion.CalculateLinkedToListOptions(resetAnswerOnOptionChange);
                if (!linkedQuestion.IsAnswered())
                {
                    interviewExpressionState.RemoveAnswer(linkedQuestion.Identity);
                }
            }
        }

        private void UpdateTreeWithStructuralChanges(InterviewTree tree, StructuralChanges structuralChanges)
        {
            foreach (KeyValuePair<Identity, int[]> changedMultiQuestion in structuralChanges.ChangedMultiQuestions)
            {
                tree.GetQuestion(changedMultiQuestion.Key).SetAnswer(CategoricalFixedMultiOptionAnswer.Convert(changedMultiQuestion.Value));
            }

            foreach (KeyValuePair<Identity, int?> changedSingleQuestion in structuralChanges.ChangedSingleQuestions)
            {
                InterviewTreeQuestion interviewTreeQuestion = tree.GetQuestion(changedSingleQuestion.Key);

                if (interviewTreeQuestion.IsSingleFixedOption)
                {
                    InterviewTreeSingleOptionQuestion question = interviewTreeQuestion.GetAsInterviewTreeSingleOptionQuestion();

                    if (changedSingleQuestion.Value.HasValue)
                        question.SetAnswer(CategoricalFixedSingleOptionAnswer.FromInt(changedSingleQuestion.Value.Value));
                    else
                        question.RemoveAnswer();
                }

                if (interviewTreeQuestion.IsSingleLinkedToList)
                {
                    InterviewTreeSingleOptionLinkedToListQuestion question = interviewTreeQuestion.GetAsInterviewTreeSingleOptionLinkedToListQuestion();
                    if (changedSingleQuestion.Value.HasValue)
                        question.SetAnswer(CategoricalFixedSingleOptionAnswer.FromInt(changedSingleQuestion.Value.Value));
                    else
                        question.RemoveAnswer();
                }
            }

            foreach (KeyValuePair<Identity, YesNoAnswersOnly> changedYesNoQuestion in structuralChanges.ChangedYesNoQuestions)
            {
                tree.GetQuestion(changedYesNoQuestion.Key).SetAnswer(YesNoAnswer.FromYesNoAnswersOnly(changedYesNoQuestion.Value));
            }

            foreach (Identity removedRosterIdentity in structuralChanges.RemovedRosters)
            {
                tree.RemoveNode(removedRosterIdentity);
            }
        }

        private void UpdateTitlesAndTexts(IQuestionnaire questionnaire)
        {
            foreach (IInterviewTreeNode node in this.Tree.AllNodes)
            {
                InterviewTreeQuestion question = node as InterviewTreeQuestion;
                if (question != null)
                {
                    SubstitutionText title = this.substitutionTextFactory.CreateText(question.Identity,
                        questionnaire.GetQuestionTitle(question.Identity.Id), questionnaire);

                    SubstitutionText[] validationMessages = questionnaire.GetValidationMessages(question.Identity.Id)
                        .Select(x => this.substitutionTextFactory.CreateText(question.Identity, x, questionnaire))
                        .ToArray();

                    question.SetTitle(title);
                    question.SetValidationMessages(validationMessages);
                    question.ReplaceSubstitutions();
                }

                InterviewTreeGroup groupOrRoster = node as InterviewTreeGroup;
                if (groupOrRoster != null)
                {
                    SubstitutionText title = this.substitutionTextFactory.CreateText(groupOrRoster.Identity,
                        questionnaire.GetGroupTitle(groupOrRoster.Identity.Id), questionnaire);

                    groupOrRoster.SetTitle(title);
                    groupOrRoster.ReplaceSubstitutions();
                }

                InterviewTreeStaticText staticText = node as InterviewTreeStaticText;
                if (staticText != null)
                {
                    SubstitutionText title = this.substitutionTextFactory.CreateText(staticText.Identity,
                       questionnaire.GetStaticText(staticText.Identity.Id), questionnaire);

                    SubstitutionText[] validationMessages = questionnaire.GetValidationMessages(staticText.Identity.Id)
                        .Select(x => this.substitutionTextFactory.CreateText(staticText.Identity, x, questionnaire))
                        .ToArray();

                    staticText.SetTitle(title);
                    staticText.SetValidationMessages(validationMessages);
                    staticText.ReplaceSubstitutions();
                }
            }
        }

        protected void ActualizeRostersIfQuestionIsRosterSize(Guid questionId)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow();

            foreach (Guid rosterId in questionnaire.GetRosterGroupsByRosterSizeQuestion(questionId))
            {
                Guid? parentOfRoster = questionnaire.GetParentGroup(rosterId);
                if (!parentOfRoster.HasValue) continue;

                List<InterviewTreeGroup> parentsOfRosters = this.Tree.FindEntity(parentOfRoster.Value).OfType<InterviewTreeGroup>().ToList();

                foreach (InterviewTreeGroup parentRoster in parentsOfRosters)
                    parentRoster.ActualizeChildren();
            }
        }

        private void SetStartDateOnFirstAnswerSet(Identity questionIdentity, DateTime answerDate)
        {
            if (this.properties.StartedDate.HasValue) return;

            InterviewTreeQuestion question = this.Tree.GetQuestion(questionIdentity);
            if (question == null) return;

            if (question.IsPrefilled) return;
            if (!question.IsInterviewer) return;

            this.properties.StartedDate = answerDate;
        }
    }
}
