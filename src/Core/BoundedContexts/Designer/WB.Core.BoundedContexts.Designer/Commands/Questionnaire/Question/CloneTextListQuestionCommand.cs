﻿using System;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question
{
    [Serializable]
    [MapsToAggregateRootMethod(typeof (Aggregates.Questionnaire), "CloneTextListQuestion")]
    public class CloneTextListQuestionCommand : AbstractCloneQuestionCommand
    {
        public CloneTextListQuestionCommand(Guid questionnaireId, Guid questionId, Guid groupId, Guid sourceQuestionId, int targetIndex,
            string title, string variableName, bool isMandatory, string condition, string instructions, Guid responsibleId,
            int? maxAnswerCount)
            : base(
                responsibleId: responsibleId, questionnaireId: questionnaireId, questionId: questionId, title: title,
                variableName: variableName, isMandatory: isMandatory, condition: condition, instructions: instructions, groupId: groupId,
                sourceQuestionId: sourceQuestionId, targetIndex: targetIndex)
        {
            this.MaxAnswerCount = maxAnswerCount;
        }

        public int? MaxAnswerCount { get; private set; }
    }
}
