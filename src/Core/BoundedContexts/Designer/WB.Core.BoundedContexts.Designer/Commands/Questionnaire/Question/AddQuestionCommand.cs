﻿using System;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question
{
    [Serializable]
    public class AddQuestionCommand : FullQuestionDataCommand
    {
        public AddQuestionCommand(
            Guid questionnaireId,
            Guid questionId, 
            Guid parentGroupId,
            string title, 
            QuestionType type,
            string variableName,
            string variableLabel,
            string mask, 
            bool isMandatory, 
            bool isPreFilled,
            QuestionScope scope, 
            string enablementCondition, 
            string validationExpression, 
            string validationMessage,
            string instructions,
            Option[] options, 
            Guid responsibleId, 
            Guid? linkedToQuestionId,
            bool areAnswersOrdered,
            int? maxAllowedAnswers, 
            bool? isFilteredCombobox,
            Guid? cascadeFromQuestionId)

            : base(questionnaireId, questionId, title, type, variableName,variableLabel,mask, isMandatory, isPreFilled,
                scope, enablementCondition, validationExpression, validationMessage, instructions, options,
                responsibleId, linkedToQuestionId, areAnswersOrdered, maxAllowedAnswers, isFilteredCombobox, cascadeFromQuestionId)
        {
            this.ParentGroupId = parentGroupId;
        }

        public Guid ParentGroupId { get; private set; }
    }
}