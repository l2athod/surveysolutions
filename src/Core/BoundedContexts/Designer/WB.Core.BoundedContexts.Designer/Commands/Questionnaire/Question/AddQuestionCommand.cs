﻿using System;
using Main.Core.Entities.SubEntities;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question
{
    [Serializable]
    [MapsToAggregateRootMethod(typeof (Aggregates.Questionnaire), "NewAddQuestion")]
    public class AddQuestionCommand : FullQuestionDataCommand
    {
        public AddQuestionCommand(
            Guid questionnaireId,
            Guid questionId, 
            Guid parentGroupId,
            string title, 
            QuestionType type,
            string variableName, string variableLabel, string mask, 
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
            bool? isFilteredCombobox
            )

            : base(questionnaireId, questionId, title, type, variableName,variableLabel,mask, isMandatory, isPreFilled,
                scope, enablementCondition, validationExpression, validationMessage, instructions, options, 
                responsibleId, linkedToQuestionId, areAnswersOrdered, maxAllowedAnswers, isFilteredCombobox)
        {
            this.ParentGroupId = parentGroupId;
        }

        public Guid ParentGroupId { get; private set; }
    }
}