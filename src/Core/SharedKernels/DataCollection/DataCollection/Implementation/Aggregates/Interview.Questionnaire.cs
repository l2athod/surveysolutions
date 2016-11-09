using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public partial class Interview
    {
        protected IQuestionnaire GetQuestionnaireOrThrow(Guid id, long version, string questionnaireLanguage)
        {
            var questionnaireIdentity = new QuestionnaireIdentity(id, version);

            IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(questionnaireIdentity, questionnaireLanguage);

            if (questionnaire == null)
                throw new InterviewException($"Questionnaire '{questionnaireIdentity}' was not found. InterviewId {EventSourceId}", InterviewDomainExceptionType.QuestionnaireIsMissing);

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

        public virtual List<CategoricalOption> GetFirstTopFilteredOptionsForQuestion(Identity question, int? parentQuestionValue, string filter, int itemsCount = 200)
        {
            itemsCount = itemsCount > 200 ? 200 : itemsCount;

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);

            if (!questionnaire.IsSupportFilteringForOptions(question.Id))
                return questionnaire.GetOptionsForQuestion(question.Id, parentQuestionValue, filter).Take(itemsCount).ToList();

            return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(question,
                questionnaire.GetOptionsForQuestion(question.Id, parentQuestionValue, filter)).Take(itemsCount).ToList();
        }

        public CategoricalOption GetOptionForQuestionWithoutFilter(Identity question, int value, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            return questionnaire.GetOptionForQuestionByOptionValue(question.Id, value);
        }

        public CategoricalOption GetOptionForQuestionWithFilter(Identity question, string optionText, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            var filteredOption = questionnaire.GetOptionForQuestionByOptionText(question.Id, optionText);

            if (filteredOption == null)
                return null;

            if (questionnaire.IsSupportFilteringForOptions(question.Id))
                return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(question, Enumerable.Repeat(filteredOption, 1)).SingleOrDefault();
            else
                return filteredOption;
        }
    }
}