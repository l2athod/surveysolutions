﻿using System;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire
{
    public class ReplaceTextsCommand : QuestionnaireCommand
    {
        public ReplaceTextsCommand(Guid questionnaireId, Guid responsibleId, string searchFor, string replaceWith, bool matchCase, bool matchWholeWord)
            : base(questionnaireId, responsibleId)
        {
            this.SearchFor = searchFor;
            this.ReplaceWith = replaceWith;
            this.MatchCase = matchCase;
            this.MatchWholeWord = matchWholeWord;
        }

        public string SearchFor { get; }
        public string ReplaceWith { get; }
        public bool MatchCase { get; }
        public bool MatchWholeWord { get; }
    }
}