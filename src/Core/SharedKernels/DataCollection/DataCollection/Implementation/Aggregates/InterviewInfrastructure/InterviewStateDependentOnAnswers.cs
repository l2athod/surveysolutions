using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public class InterviewStateDependentOnAnswers : IReadOnlyInterviewStateDependentOnAnswers
    {
        private class AmendingWrapper : IReadOnlyInterviewStateDependentOnAnswers
        {
            private readonly IReadOnlyInterviewStateDependentOnAnswers actualState;
            private readonly Func<Guid, RosterVector, IEnumerable<decimal>> getRosterInstanceIds;

            public AmendingWrapper(IReadOnlyInterviewStateDependentOnAnswers actualState, Func<Guid, RosterVector, IEnumerable<decimal>> getRosterInstanceIds)
            {
                this.actualState = actualState;
                this.getRosterInstanceIds = getRosterInstanceIds;
            }

            public IReadOnlyInterviewStateDependentOnAnswers Amend(Func<Guid, RosterVector, IEnumerable<decimal>> getRosterInstanceIds) => this.actualState.Amend(getRosterInstanceIds);

            public bool IsGroupDisabled(Identity @group) => this.actualState.IsGroupDisabled(@group);

            public bool IsQuestionDisabled(Identity question) => this.actualState.IsQuestionDisabled(question);

            public bool WasQuestionAnswered(Identity question) => this.actualState.WasQuestionAnswered(question);

            public object GetAnswerSupportedInExpressions(Identity question) => this.actualState.GetAnswerSupportedInExpressions(question);

            public Tuple<decimal, string>[] GetTextListAnswer(Identity question) => this.actualState.GetTextListAnswer(question);

            public ReadOnlyCollection<decimal> GetRosterInstanceIds(Guid groupId, RosterVector outerRosterVector)
            {
                return this.getRosterInstanceIds != null
                    ? this.getRosterInstanceIds(groupId, outerRosterVector).ToReadOnlyCollection()
                    : this.actualState.GetRosterInstanceIds(groupId, outerRosterVector);
            }

            public IEnumerable<Tuple<Identity, RosterVector>> GetAllLinkedSingleOptionAnswers(IQuestionnaire questionnaire) => this.actualState.GetAllLinkedSingleOptionAnswers(questionnaire);

            public IEnumerable<Tuple<Identity, RosterVector[]>> GetAllLinkedMultipleOptionsAnswers() => this.actualState.GetAllLinkedMultipleOptionsAnswers();
        }


        public InterviewStateDependentOnAnswers()
        {
            this.AnswersSupportedInExpressions = new ConcurrentDictionary<string, object>();
            this.LinkedSingleOptionAnswersBuggy = new ConcurrentDictionary<string, Tuple<Identity, RosterVector>>();
            this.LinkedMultipleOptionsAnswers = new ConcurrentDictionary<string, Tuple<Identity, RosterVector[]>>();
            this.TextListAnswers = new ConcurrentDictionary<string, Tuple<decimal, string>[]>();

            this.AnsweredQuestions = new ConcurrentHashSet<string>();
            this.DisabledGroups = new ConcurrentHashSet<string>();
            this.DisabledQuestions = new ConcurrentHashSet<string>();
            this.RosterGroupInstanceIds = new ConcurrentDictionary<string, ConcurrentDistinctList<decimal>>();
            this.ValidAnsweredQuestions = new ConcurrentHashSet<string>();
            this.InvalidAnsweredQuestions = new ConcurrentHashSet<string>();
            this.AnswerComments = new ConcurrentBag<AnswerComment>();
        }

        public ConcurrentDictionary<string, object> AnswersSupportedInExpressions { set; get; }
        public ConcurrentDictionary<string, Tuple<Identity, RosterVector>> LinkedSingleOptionAnswersBuggy { set; get; }
        public ConcurrentDictionary<string, Tuple<Identity, RosterVector[]>> LinkedMultipleOptionsAnswers { set; get; }
        public ConcurrentDictionary<string, Tuple<decimal, string>[]> TextListAnswers { set; get; }
        public ConcurrentHashSet<string> AnsweredQuestions { set; get; }
        public ConcurrentHashSet<string> DisabledGroups { set; get; }
        public ConcurrentHashSet<string> DisabledQuestions { set; get; }
        public ConcurrentDictionary<string, ConcurrentDistinctList<decimal>> RosterGroupInstanceIds { set; get; }
        public ConcurrentHashSet<string> ValidAnsweredQuestions { set; get; }
        public ConcurrentHashSet<string> InvalidAnsweredQuestions { set; get; }
        public ConcurrentBag<AnswerComment> AnswerComments { get; set; }

        public void ApplyInterviewChanges(InterviewChanges changes)
        {
            if (changes.EnablementChanges != null)
                this.ApplyEnablementChanges(changes.EnablementChanges);

            if (changes.ValidityChanges != null)
            {
                this.DeclareAnswersInvalid(changes.ValidityChanges.AnswersDeclaredInvalid);
            }

            if (changes.RosterCalculationData != null)
            {
                this.ApplyRosterData(changes.RosterCalculationData);
            }

            if (changes.AnswersForLinkedQuestionsToRemove != null)
            {
                this.RemoveAnswers(changes.AnswersForLinkedQuestionsToRemove);
            }
        }

        public void ApplyRosterData(RosterCalculationData rosterCalculationData)
        {
            if (rosterCalculationData.RosterInstancesToAdd.Any())
            {
                AddedRosterInstance[] instances = rosterCalculationData
                    .RosterInstancesToAdd
                    .Select(roster => new AddedRosterInstance(roster.GroupId, roster.OuterRosterVector, roster.RosterInstanceId, roster.SortIndex))
                    .ToArray();

                this.AddRosterInstances(instances);
            }

            if (rosterCalculationData.RosterInstancesToRemove.Any())
            {
                RosterInstance[] instances = rosterCalculationData
                    .RosterInstancesToRemove
                    .Select(roster => new RosterInstance(roster.GroupId, roster.OuterRosterVector, roster.RosterInstanceId))
                    .ToArray();

                this.RemoveRosterInstances(instances);
            }

            if (rosterCalculationData.AnswersToRemoveByDecreasedRosterSize.Any())
            {
                this.RemoveAnswers(rosterCalculationData.AnswersToRemoveByDecreasedRosterSize);
            }

            rosterCalculationData.RosterInstantiatesFromNestedLevels.ForEach(this.ApplyRosterData);
        }

        public void ApplyEnablementChanges(EnablementChanges enablementChanges)
        {
            this.EnableGroups(enablementChanges.GroupsToBeEnabled);
            this.DisableGroups(enablementChanges.GroupsToBeDisabled);
            this.EnableQuestions(enablementChanges.QuestionsToBeEnabled);
            this.DisableQuestions(enablementChanges.QuestionsToBeDisabled);
        }

        public void AddRosterInstances(AddedRosterInstance[] instances)
        {
            foreach (var instance in instances)
            {
                string rosterGroupKey = ConversionHelper.ConvertIdAndRosterVectorToString(instance.GroupId, instance.OuterRosterVector);
                var rosterRowInstances = this.RosterGroupInstanceIds.ContainsKey(rosterGroupKey)
                    ? this.RosterGroupInstanceIds[rosterGroupKey]
                    : new ConcurrentDistinctList<decimal>();

                rosterRowInstances.Add(instance.RosterInstanceId);

                this.RosterGroupInstanceIds[rosterGroupKey] = rosterRowInstances;
            }
        }

        public void RemoveRosterInstances(IEnumerable<RosterInstance> instances)
        {
            foreach (var instance in instances)
            {
                string rosterGroupKey = ConversionHelper.ConvertIdAndRosterVectorToString(instance.GroupId, instance.OuterRosterVector);

                var rosterRowInstances = this.RosterGroupInstanceIds.ContainsKey(rosterGroupKey)
                    ? this.RosterGroupInstanceIds[rosterGroupKey]
                    : new ConcurrentDistinctList<decimal>();
                rosterRowInstances.Remove(instance.RosterInstanceId);

                this.RosterGroupInstanceIds[rosterGroupKey] = rosterRowInstances;
            }
        }

        public void EnableQuestions(IEnumerable<Identity> groups)
        {
            foreach (string questionKey in groups.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.DisabledQuestions.Remove(questionKey);
            }
        }

        public void DisableQuestions(IEnumerable<Identity> groups)
        {
            foreach (string questionKey in groups.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.DisabledQuestions.Add(questionKey);
            }
        }

        public void EnableGroups(IEnumerable<Identity> groups)
        {
            foreach (string groupKey in groups.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.DisabledGroups.Remove(groupKey);
            }
        }

        public void DisableGroups(IEnumerable<Identity> groups)
        {
            foreach (string groupKey in groups.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.DisabledGroups.Add(groupKey);
            }
        }

        public void DeclareAnswersInvalid(IEnumerable<Identity> questions)
        {
            foreach (string questionKey in questions.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.ValidAnsweredQuestions.Remove(questionKey);
                this.InvalidAnsweredQuestions.Add(questionKey);
            }
        }

        public void DeclareAnswersValid(IEnumerable<Identity> questions)
        {
            foreach (string questionKey in questions.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.ValidAnsweredQuestions.Add(questionKey);
                this.InvalidAnsweredQuestions.Remove(questionKey);
            }
        }
        
        public void RemoveAnswers(IEnumerable<Identity> questions)
        {
            foreach (string questionKey in questions.Select(ConversionHelper.ConvertIdentityToString))
            {
                this.AnswersSupportedInExpressions.TryRemove(questionKey);
                this.LinkedSingleOptionAnswersBuggy.TryRemove(questionKey);
                this.LinkedMultipleOptionsAnswers.TryRemove(questionKey);
                this.TextListAnswers.TryRemove(questionKey);
                this.AnsweredQuestions.Remove(questionKey);
                this.DisabledQuestions.Remove(questionKey);
                this.ValidAnsweredQuestions.Remove(questionKey);
                this.InvalidAnsweredQuestions.Remove(questionKey);
            }
        }


        public IReadOnlyInterviewStateDependentOnAnswers Amend(Func<Guid, RosterVector, IEnumerable<decimal>> getRosterInstanceIds)
        {
            return new AmendingWrapper(this, getRosterInstanceIds);
        }


        public bool IsGroupDisabled(Identity group)
        {
            string groupKey = ConversionHelper.ConvertIdentityToString(group);

            return this.DisabledGroups.Contains(groupKey);
        }

        public bool IsQuestionDisabled(Identity question)
        {
            string questionKey = ConversionHelper.ConvertIdentityToString(question);

            return this.DisabledQuestions.Contains(questionKey);
        }

        public bool WasQuestionAnswered(Identity question)
        {
            string questionKey = ConversionHelper.ConvertIdentityToString(question);

            return this.AnsweredQuestions.Contains(questionKey);
        }

        public object GetAnswerSupportedInExpressions(Identity question)
        {
            string questionKey = ConversionHelper.ConvertIdentityToString(question);

            return this.AnswersSupportedInExpressions.ContainsKey(questionKey)
                ? this.AnswersSupportedInExpressions[questionKey]
                : null;
        }

        public Tuple<decimal, string>[] GetTextListAnswer(Identity question)
        {
            string questionKey = ConversionHelper.ConvertIdentityToString(question);

            return this.TextListAnswers.ContainsKey(questionKey)
                ? this.TextListAnswers[questionKey]
                : null;
        }

        public ReadOnlyCollection<decimal> GetRosterInstanceIds(Guid groupId, RosterVector outerRosterVector)
        {
            string groupKey = ConversionHelper.ConvertIdAndRosterVectorToString(groupId, outerRosterVector);

            return this.RosterGroupInstanceIds.ContainsKey(groupKey)
                ? this.RosterGroupInstanceIds[groupKey].ToReadOnlyCollection()
                : Enumerable.Empty<decimal>().ToReadOnlyCollection();
        }

        public IEnumerable<Tuple<Identity, RosterVector>> GetAllLinkedSingleOptionAnswers(IQuestionnaire questionnaire)
        {
            // we currently have a bug that after restore linked single option answers list is filled with not linked answers after sync
            return this.LinkedSingleOptionAnswersBuggy.Values.Where(answer => questionnaire.IsQuestionLinked(answer.Item1.Id));
        }

        public IEnumerable<Tuple<Identity, RosterVector[]>> GetAllLinkedMultipleOptionsAnswers()
        {
            return this.LinkedMultipleOptionsAnswers.Values;
        }
    }
}