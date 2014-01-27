﻿using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Snapshots
{
    internal class InterviewState
    {
        public InterviewState(Guid questionnaireId, long questionnaireVersion, InterviewStatus status,
            Dictionary<string, object> answersSupportedInExpressions, Dictionary<string, Tuple<Guid, decimal[], decimal[]>> linkedSingleOptionAnswers,
            Dictionary<string, Tuple<Guid, decimal[], decimal[][]>> linkedMultipleOptionsAnswers, HashSet<string> answeredQuestions,
            HashSet<string> disabledGroups, HashSet<string> disabledQuestions, Dictionary<string, Interview.DistinctDecimalList> rosterGroupInstanceIds,
            HashSet<string> validAnsweredQuestions, HashSet<string> invalidAnsweredQuestions, bool wasCompleted)
        {
            this.QuestionnaireId = questionnaireId;
            this.QuestionnaireVersion = questionnaireVersion;
            this.Status = status;
            this.AnswersSupportedInExpressions = answersSupportedInExpressions;
            this.LinkedSingleOptionAnswers = linkedSingleOptionAnswers;
            this.LinkedMultipleOptionsAnswers = linkedMultipleOptionsAnswers;
            this.AnsweredQuestions = answeredQuestions;
            this.DisabledGroups = disabledGroups;
            this.DisabledQuestions = disabledQuestions;
            this.RosterGroupInstanceIds = rosterGroupInstanceIds;
            this.ValidAnsweredQuestions = validAnsweredQuestions;
            this.InvalidAnsweredQuestions = invalidAnsweredQuestions;
            this.WasCompleted = wasCompleted;
        }

        public Guid QuestionnaireId { get; private set; }
        public long QuestionnaireVersion { get; private set; }
        public InterviewStatus Status { get; private set; }
        public Dictionary<string, object> AnswersSupportedInExpressions { get; private set; }
        public Dictionary<string, Tuple<Guid, decimal[], decimal[]>> LinkedSingleOptionAnswers { get; private set; }
        public Dictionary<string, Tuple<Guid, decimal[], decimal[][]>> LinkedMultipleOptionsAnswers { get; private set; }
        public HashSet<string> AnsweredQuestions { get; private set; }
        public HashSet<string> DisabledGroups { get; private set; }
        public HashSet<string> DisabledQuestions { get; private set; }
        public Dictionary<string, Interview.DistinctDecimalList> RosterGroupInstanceIds { get; private set; }
        public HashSet<string> ValidAnsweredQuestions { get; private set; }
        public HashSet<string> InvalidAnsweredQuestions { get; private set; }
        public bool WasCompleted { get; private set; }
    }
}
