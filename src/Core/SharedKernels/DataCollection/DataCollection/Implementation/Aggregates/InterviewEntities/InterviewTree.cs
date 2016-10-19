﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    public class InterviewTree
    {
        public InterviewTree(Guid interviewId, IEnumerable<InterviewTreeSection> sections)
        {
            this.InterviewId = interviewId.FormatGuid();
            this.Sections = sections.ToList();

            foreach (var section in this.Sections)
            {
                ((IInternalInterviewTreeNode)section).SetTree(this);
            }
        }

        public string InterviewId { get; }
        public IReadOnlyCollection<InterviewTreeSection> Sections { get; }

        public InterviewTreeQuestion GetQuestion(Identity questionIdentity)
            => this
                .GetNodes<InterviewTreeQuestion>()
                .SingleOrDefault(node => node.Identity == questionIdentity);


        internal InterviewTreeGroup GetGroup(Identity identity) 
            => this
            .GetNodes<InterviewTreeGroup>()
            .SingleOrDefault(node => node.Identity == identity);

        internal InterviewTreeStaticText GetStaticText(Identity identity) 
            => this
            .GetNodes<InterviewTreeStaticText>()
            .Single(node => node.Identity == identity);

        public InterviewTreeVariable GetVariable(Identity identity)
            => this
            .GetNodes<InterviewTreeVariable>()
            .Single(node => node.Identity == identity);

        public IReadOnlyCollection<InterviewTreeQuestion> FindQuestions(Guid questionId)
            => this
                .GetNodes<InterviewTreeQuestion>()
                .Where(node => node.Identity.Id == questionId)
                .ToReadOnlyCollection();

        public IReadOnlyCollection<InterviewTreeQuestion> FindQuestions()
            => this
                .GetNodes<InterviewTreeQuestion>()
                .ToReadOnlyCollection();

        public IReadOnlyCollection<InterviewTreeRoster> FindRosters()
            => this
                .GetNodes<InterviewTreeRoster>()
                .ToReadOnlyCollection();

        public IEnumerable<IInterviewTreeNode> FindEntity(Guid nodeId)
        {
            return this.GetNodes().Where(x => x.Identity.Id == nodeId);
        }

        private IEnumerable<TNode> GetNodes<TNode>() => this.GetNodes().OfType<TNode>();

        private IEnumerable<IInterviewTreeNode> GetNodes()
            => this.Sections.Cast<IInterviewTreeNode>().TreeToEnumerable(node => node.Children);

        public void RemoveNode(Identity identity)
        {
            foreach (var node in this.GetNodes().Where(x => x.Identity.Equals(identity)))
                ((InterviewTreeGroup)node.Parent)?.RemoveChildren(node.Identity);
        }

        public IReadOnlyCollection<InterviewTreeNodeDiff> Compare(InterviewTree changedTree)
        {
            var sourceNodes = this.GetNodes().ToList();
            var changedNodes = changedTree.GetNodes().ToList();

            var leftOuterJoin = from source in sourceNodes
                                join changed in changedNodes
                                    on source.Identity equals changed.Identity
                                    into temp
                                from changed in temp.DefaultIfEmpty()
                                select InterviewTreeNodeDiff.Create(source, changed);

            var rightOuterJoin = from changed in changedNodes
                                 join source in sourceNodes
                                     on changed.Identity equals source.Identity
                                     into temp
                                 from source in temp.DefaultIfEmpty()
                                 select InterviewTreeNodeDiff.Create(source, changed);

            var fullOuterJoin = leftOuterJoin.Concat(rightOuterJoin);

            return fullOuterJoin
                .DistinctBy(x => new {sourceIdentity = x.SourceNode?.Identity, changedIdentity = x.ChangedNode?.Identity})
                .Where(diff =>
                    diff.IsNodeAdded ||
                    diff.IsNodeRemoved ||
                    diff.IsNodeDisabled ||
                    diff.IsNodeEnabled ||
                    IsRosterTitleChanged(diff as InterviewTreeRosterDiff) ||
                    IsAnswerByQuestionChanged(diff as InterviewTreeQuestionDiff) ||
                    IsQuestionValid(diff as InterviewTreeQuestionDiff) ||
                    IsQuestionInalid(diff as InterviewTreeQuestionDiff) ||
                    IsStaticTextValid(diff as InterviewTreeStaticTextDiff) ||
                    IsStaticTextInalid(diff as InterviewTreeStaticTextDiff) ||
                    IsVariableChanged(diff as InterviewTreeVariableDiff) ||
                    IsOptionsSetChanged(diff as InterviewTreeQuestionDiff))
                .ToReadOnlyCollection();
        }

        private bool IsOptionsSetChanged(InterviewTreeQuestionDiff diffByQuestion)
        {
            if (diffByQuestion == null || diffByQuestion.IsNodeRemoved) return false;

            return diffByQuestion.IsOptionsChanged;
        }

        private static bool IsVariableChanged(InterviewTreeVariableDiff diffByVariable)
            => diffByVariable != null && diffByVariable.IsValueChanged;

        private static bool IsQuestionValid(InterviewTreeQuestionDiff diffByQuestion)
            => diffByQuestion != null && diffByQuestion.IsValid;

        private static bool IsQuestionInalid(InterviewTreeQuestionDiff diffByQuestion)
            => diffByQuestion != null && diffByQuestion.IsInvalid;

        private static bool IsStaticTextValid(InterviewTreeStaticTextDiff diffByQuestion)
            => diffByQuestion != null && diffByQuestion.IsValid;

        private static bool IsStaticTextInalid(InterviewTreeStaticTextDiff diffByQuestion)
            => diffByQuestion != null && diffByQuestion.IsInvalid;

        private static bool IsAnswerByQuestionChanged(InterviewTreeQuestionDiff diffByQuestion)
            => diffByQuestion != null && diffByQuestion.IsAnswerChanged;

        private static bool IsRosterTitleChanged(InterviewTreeRosterDiff diffByRoster)
            => diffByRoster != null && diffByRoster.IsRosterTitleChanged;

        public override string ToString()
            => $"Tree ({this.InterviewId})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Sections.Select(section => section.ToString().PrefixEachLine("  ")));
    }

    public interface IInterviewTreeNode
    {
        Identity Identity { get; }
        IInterviewTreeNode Parent { get; }
        IReadOnlyCollection<IInterviewTreeNode> Children { get; }

        bool IsDisabled();

        void Disable();
        void Enable();
    }

    public interface IInternalInterviewTreeNode
    {
        void SetTree(InterviewTree tree);
        void SetParent(IInterviewTreeNode parent);
    }

    public abstract class InterviewTreeLeafNode : IInterviewTreeNode, IInternalInterviewTreeNode
    {
        private bool isDisabled;

        protected InterviewTreeLeafNode(Identity identity, bool isDisabled)
        {
            this.Identity = identity;
            this.isDisabled = isDisabled;
        }

        public Identity Identity { get; }
        public InterviewTree Tree { get; private set; }
        public IInterviewTreeNode Parent { get; private set; }
        IReadOnlyCollection<IInterviewTreeNode> IInterviewTreeNode.Children { get; } = Enumerable.Empty<IInterviewTreeNode>().ToReadOnlyCollection();

        void IInternalInterviewTreeNode.SetTree(InterviewTree tree) => this.Tree = tree;
        void IInternalInterviewTreeNode.SetParent(IInterviewTreeNode parent) => this.Parent = parent;

        public bool IsDisabled() => this.isDisabled || (this.Parent?.IsDisabled() ?? false);

        public void Disable() => this.isDisabled = true;
        public void Enable() => this.isDisabled = false;
    }

    public abstract class InterviewTreeGroup : IInterviewTreeNode, IInternalInterviewTreeNode
    {
        private bool isDisabled;
        private readonly List<IInterviewTreeNode> children;

        protected InterviewTreeGroup(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
        {
            this.Identity = identity;
            this.children = children.ToList();
            this.isDisabled = isDisabled;

            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetParent(this);
            }
        }

        public Identity Identity { get; }
        public InterviewTree Tree { get; private set; }
        public IInterviewTreeNode Parent { get; private set; }
        public IReadOnlyCollection<IInterviewTreeNode> Children => this.children;

        void IInternalInterviewTreeNode.SetTree(InterviewTree tree)
        {
            this.Tree = tree;

            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetTree(tree);
            }
        }

        void IInternalInterviewTreeNode.SetParent(IInterviewTreeNode parent) => this.Parent = parent;

        public void AddChildren(IInterviewTreeNode child)
        {
            var internalTreeNode = child as IInternalInterviewTreeNode;
            if (internalTreeNode == null) throw new ArgumentException(nameof(child));

            internalTreeNode.SetTree(this.Tree);
            internalTreeNode.SetParent(this);
            this.children.Add(child);
        }

        public void RemoveChildren(Identity identity)
        {
            var nodesToRemove = this.children.Where(x => x.Identity.Equals(identity)).ToArray();
            nodesToRemove.ForEach(nodeToRemove => this.children.Remove(nodeToRemove));
        }

        public void RemoveChildren(Identity[] identities)
        {
            foreach (var child in this.children.Where(x => identities.Contains(x.Identity)))
                this.children.Remove(child);
        }

        public bool IsDisabled() => this.isDisabled || (this.Parent?.IsDisabled() ?? false);
        public void Disable() => this.isDisabled = true;
        public void Enable() => this.isDisabled = false;

        public InterviewTreeQuestion GetQuestionFromThisOrUpperLevel(Guid questionId)
        {
            for (int i = this.Identity.RosterVector.Length; i >= 0; i--)
            {
                var questionIdentity = new Identity(questionId, this.Identity.RosterVector.Take(i).ToArray());
                var question = this.Tree.GetQuestion(questionIdentity);
                if (question != null)
                    return question;
            }

            return null;
            //InterviewTreeQuestion question = null;
            //IInterviewTreeNode group = this;
            //while (question == null)
            //{
            //    question = group.Children.FirstOrDefault(x => x.Identity.Id == questionId) as InterviewTreeQuestion;
            //    if (group is InterviewTreeSection)
            //        break;
            //    group = group.Parent;
            //}

            //return question;
        }

        public bool HasChild(Identity identity)
        {
            return this.Children.Any(x => x.Identity.Equals(identity));
        }
    }

    public class InterviewTreeQuestion : InterviewTreeLeafNode
    {
        public InterviewTreeQuestion(Identity identity, bool isDisabled, string title, string variableName,
            QuestionType questionType, object answer,
            IEnumerable<RosterVector> linkedOptions, Identity cascadingParentQuestionIdentity, bool isYesNo, bool isDecimal, Guid? linkedSourceId = null, 
            Identity commonParentRosterIdForLinkedQuestion = null)
            : base(identity, isDisabled)
        {
            this.Title = title;
            this.VariableName = variableName;

            if (questionType == QuestionType.SingleOption)
            {
                if (linkedSourceId.HasValue)
                {
                    this.AsSingleLinkedOption = new InterviewTreeSingleLinkedOptionQuestion(linkedOptions, answer, linkedSourceId.Value, commonParentRosterIdForLinkedQuestion);
                }
                else
                    this.AsSingleOption = new InterviewTreeSingleOptionQuestion(answer);
            }

            if (questionType == QuestionType.MultyOption)
            {
                if (isYesNo)
                    this.AsYesNo = new InterviewTreeYesNoQuestion(answer);
                else if (linkedSourceId.HasValue)
                {
                    this.AsMultiLinkedOption = new InterviewTreeMultiLinkedOptionQuestion(linkedOptions, answer, linkedSourceId.Value, commonParentRosterIdForLinkedQuestion);
                }
                else
                    this.AsMultiOption = new InterviewTreeMultiOptionQuestion(answer);
            }

            if (questionType == QuestionType.DateTime)
                this.AsDateTime = new InterviewTreeDateTimeQuestion(answer);

            if (questionType == QuestionType.GpsCoordinates)
                this.AsGps = new InterviewTreeGpsQuestion(answer);

            if (questionType == QuestionType.Multimedia)
                this.AsMultimedia = new InterviewTreeMultimediaQuestion(answer);

            if (questionType == QuestionType.Numeric)
            {
                if (isDecimal)
                    this.AsDouble = new InterviewTreeDoubleQuestion(answer);
                else
                    this.AsInteger = new InterviewTreeIntegerQuestion(answer);
            }

            if (questionType == QuestionType.QRBarcode)
                this.AsQRBarcode = new InterviewTreeQRBarcodeQuestion(answer);

            if (questionType == QuestionType.Text)
                this.AsText = new InterviewTreeTextQuestion(answer);

            if (questionType == QuestionType.TextList)
                this.AsTextList = new InterviewTreeTextListQuestion(answer);

            if (cascadingParentQuestionIdentity != null)
                this.AsCascading = new InterviewTreeCascadingQuestion(this, cascadingParentQuestionIdentity);
        }

        public InterviewTreeDoubleQuestion AsDouble { get; }
        public InterviewTreeTextListQuestion AsTextList { get; }
        public InterviewTreeTextQuestion AsText { get; }
        public InterviewTreeQRBarcodeQuestion AsQRBarcode { get; }
        public InterviewTreeIntegerQuestion AsInteger { get; }
        public InterviewTreeMultimediaQuestion AsMultimedia { get; }
        public InterviewTreeGpsQuestion AsGps { get; }
        public InterviewTreeDateTimeQuestion AsDateTime { get; }
        public InterviewTreeMultiOptionQuestion AsMultiOption { get; }
        public InterviewTreeMultiLinkedOptionQuestion AsMultiLinkedOption { get; }
        public InterviewTreeYesNoQuestion AsYesNo { get; }
        public InterviewTreeSingleLinkedOptionQuestion AsSingleLinkedOption { get; }
        public InterviewTreeSingleOptionQuestion AsSingleOption { get; }

        public InterviewTreeLinkedQuestion AsLinked => this.IsSingleLinkedOption ? (InterviewTreeLinkedQuestion)this.AsSingleLinkedOption : this.AsMultiLinkedOption;

        public InterviewTreeCascadingQuestion AsCascading { get; }

        public string Title { get; }
        public string VariableName { get; }

        public bool IsValid => !this.FailedValidations?.Any() ?? true;
        public IReadOnlyList<FailedValidationCondition> FailedValidations { get; private set; }

        public void SetFailedValidations(IEnumerable<FailedValidationCondition> failedValidations)
            => this.FailedValidations = failedValidations?.ToReadOnlyCollection();
        
        public bool IsDouble => this.AsDouble != null;
        public bool IsInteger => this.AsInteger != null;
        public bool IsSingleOption => this.AsSingleOption != null;
        public bool IsMultiOption => this.AsMultiOption != null;
        public bool IsMultiLinkedOption => this.AsMultiLinkedOption != null;
        public bool IsSingleLinkedOption => this.AsSingleLinkedOption != null;
        public bool IsQRBarcode => this.AsQRBarcode != null;
        public bool IsText => this.AsText != null;
        public bool IsTextList => this.AsTextList != null;
        public bool IsYesNo => this.AsYesNo != null;
        public bool IsDateTime => this.AsDateTime != null;
        public bool IsGps => this.AsGps != null;
        public bool IsMultimedia => this.AsMultimedia != null;

        public bool IsLinked => (this.IsMultiLinkedOption || this.IsSingleLinkedOption);
        public bool IsCascading => this.AsCascading != null;

        public bool IsAnswered()
        {
            if (this.IsText) return this.AsText.IsAnswered;
            if (this.IsInteger) return this.AsInteger.IsAnswered;
            if (this.IsDouble) return this.AsDouble.IsAnswered;
            if (this.IsDateTime) return this.AsDateTime.IsAnswered;
            if (this.IsMultimedia) return this.AsMultimedia.IsAnswered;
            if (this.IsQRBarcode) return this.AsQRBarcode.IsAnswered;
            if (this.IsGps) return this.AsGps.IsAnswered;
            if (this.IsSingleOption) return this.AsSingleOption.IsAnswered;
            if (this.IsSingleLinkedOption) return this.AsSingleLinkedOption.IsAnswered;
            if (this.IsMultiOption) return this.AsMultiOption.IsAnswered;
            if (this.IsMultiLinkedOption) return this.AsMultiLinkedOption.IsAnswered;
            if (this.IsYesNo) return this.AsYesNo.IsAnswered;
            if (this.IsTextList) return this.AsTextList.IsAnswered;

            return false;
        }

        public string FormatForException() => $"'{this.Title} [{this.VariableName}] ({this.Identity})'";

        public override string ToString() => $"Question ({this.Identity}) '{this.Title}'";

        public void UpdateLinkedOptions()
        {
            if (!IsLinked) return;

            InterviewTreeLinkedQuestion linkedQuestion = this.AsLinked;

            List<IInterviewTreeNode> sourceNodes = new List<IInterviewTreeNode>();
            var isQuestionOnTopLeveOrInDifferentRosterBranch = linkedQuestion.CommonParentRosterIdForLinkedQuestion == null;
            if (isQuestionOnTopLeveOrInDifferentRosterBranch)
            {
                sourceNodes = this.Tree.FindEntity(linkedQuestion.LinkedSourceId).ToList();
            }
            else
            {
                var parentGroup = this.Tree.GetGroup(linkedQuestion.CommonParentRosterIdForLinkedQuestion);
                if (parentGroup !=null)
                    sourceNodes = parentGroup
                        .TreeToEnumerable<IInterviewTreeNode>(node => node.Children)
                        .Where(x => x.Identity.Id == linkedQuestion.LinkedSourceId)
                        .ToList();
            }

            var options = sourceNodes.Where(x => !x.IsDisabled()).Select(x => x.Identity.RosterVector).ToList();
            List<RosterVector> previousOptions = linkedQuestion.Options;
            linkedQuestion.SetOptions(options);

            if (!previousOptions.SequenceEqual(options))
            {
                if (IsMultiLinkedOption)
                    AsMultiLinkedOption.RemoveAnswer();
                else
                    AsSingleLinkedOption.RemoveAnswer();
            }
        }

        public string GetAnswerAsString(Func<decimal, string> getCategoricalAnswerOptionText = null)
        {
            if (this.IsText) return this.AsText.GetAnswer();
            if (this.IsMultimedia) return this.AsMultimedia.GetAnswer();
            if (this.IsQRBarcode) return this.AsQRBarcode.GetAnswer();
            if (this.IsInteger) return AnswerUtils.AnswerToString(this.AsInteger.GetAnswer());
            if (this.IsDouble) return AnswerUtils.AnswerToString(this.AsDouble.GetAnswer());
            if (this.IsDateTime) return AnswerUtils.AnswerToString(this.AsDateTime.GetAnswer());
            if (this.IsGps) return AnswerUtils.AnswerToString(this.AsGps.GetAnswer());
            if (this.IsTextList) return AnswerUtils.AnswerToString(this.AsTextList.GetAnswer());

            if (this.IsSingleLinkedOption)
            {
                var linkedQuestion = new Identity(AsSingleLinkedOption.LinkedSourceId, this.AsSingleLinkedOption.GetAnswer());
                return Tree.GetQuestion(linkedQuestion).GetAnswerAsString(getCategoricalAnswerOptionText);
            }
            if (this.IsMultiLinkedOption)
            {
                var formattedAnswers = this.AsMultiLinkedOption.GetAnswer()
                    .Select(x => new Identity(AsMultiLinkedOption.LinkedSourceId, x))
                    .Select(x => Tree.GetQuestion(x).GetAnswerAsString(getCategoricalAnswerOptionText));
                return string.Join(", ", formattedAnswers);
            }

            if (this.IsSingleOption) return AnswerUtils.AnswerToString(this.AsSingleOption.GetAnswer(), getCategoricalAnswerOptionText);
            if (this.IsMultiOption) return AnswerUtils.AnswerToString(this.AsMultiOption.GetAnswer(), getCategoricalAnswerOptionText);
            if (this.IsYesNo) return AnswerUtils.AnswerToString(this.AsYesNo.GetAnswer(), getCategoricalAnswerOptionText);
            return string.Empty;
        }
    }

    public class InterviewTreeDateTimeQuestion
    {
        private DateTime? answer;
        public InterviewTreeDateTimeQuestion(object answer)
        {
            this.answer = answer == null ? (DateTime?)null : Convert.ToDateTime(answer);
        }

        public bool IsAnswered => this.answer != null;
        public DateTime GetAnswer() => this.answer.Value;
        public void SetAnswer(DateTime answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeDateTimeQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeGpsQuestion
    {
        private GeoPosition answer;

        public InterviewTreeGpsQuestion(object answer)
        {
            this.answer = answer as GeoPosition;
        }

        public bool IsAnswered => this.answer != null;
        public GeoPosition GetAnswer() => this.answer;
        public void SetAnswer(GeoPosition answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeGpsQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeMultimediaQuestion
    {
        private string answer;

        public InterviewTreeMultimediaQuestion(object answer)
        {
            this.answer = answer as string;
        }

        public bool IsAnswered => this.answer != null;
        public string GetAnswer() => this.answer;
        public void SetAnswer(string answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeMultimediaQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeIntegerQuestion
    {
        private int? answer;

        public InterviewTreeIntegerQuestion(object answer)
        {
            this.answer = answer == null ? (int?)null : Convert.ToInt32(answer);
        }

        public bool IsAnswered => this.answer != null;
        public int GetAnswer() => this.answer.Value;
        public void SetAnswer(int answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeIntegerQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeDoubleQuestion
    {
        private double? answer;

        public InterviewTreeDoubleQuestion(object answer)
        {
            this.answer = answer == null ? (double?)null : Convert.ToDouble(answer);
        }

        public bool IsAnswered => this.answer != null;
        public double GetAnswer() => this.answer.Value;
        public void SetAnswer(double answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeDoubleQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeQRBarcodeQuestion
    {
        private string answer;

        public InterviewTreeQRBarcodeQuestion(object answer)
        {
            this.answer = answer as string;
        }

        public bool IsAnswered => this.answer != null;
        public string GetAnswer() => this.answer;
        public void SetAnswer(string answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;
        public bool EqualByAnswer(InterviewTreeQRBarcodeQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeTextQuestion
    {
        private string answer;

        public InterviewTreeTextQuestion(object answer)
        {
            this.answer = answer as string;
        }

        public bool IsAnswered => this.answer != null;
        public string GetAnswer() => this.answer;
        public void SetAnswer(string answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeTextQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeYesNoQuestion
    {
        private AnsweredYesNoOption[] answer;

        public InterviewTreeYesNoQuestion(object answer)
        {
            this.answer = answer as AnsweredYesNoOption[];
        }

        public bool IsAnswered => this.answer != null;
        public AnsweredYesNoOption[] GetAnswer() => this.answer;
        public void SetAnswer(AnsweredYesNoOption[] answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeYesNoQuestion question)
        {
            if (question?.answer == null && this.answer == null)
                return true;

            if (question?.answer != null && this.answer != null)
                return question.answer.SequenceEqual(this.answer);

            return false;
        }

        public string GetOptionTitle(decimal optionCode)
        {
            return string.Empty;
        }
    }

    public class InterviewTreeTextListQuestion
    {
        private Tuple<decimal, string>[] answer;

        public InterviewTreeTextListQuestion(object answer)
        {
            this.answer = answer as Tuple<decimal, string>[];
        }

        public bool IsAnswered => this.answer != null;
        public Tuple<decimal, string>[] GetAnswer() => this.answer;
        public void SetAnswer(Tuple<decimal, string>[] answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeTextListQuestion question)
        {
            if (question?.answer == null && this.answer == null)
                return true;

            if (question?.answer != null && this.answer != null)
                return question.answer.SequenceEqual(this.answer);

            return false;
        }

        public string GetTitleByItemCode(decimal code)
        {
            if (!IsAnswered)
                return string.Empty;
            return this.answer.Single(x => x.Item1 == code).Item2;
        }
    }

    public class InterviewTreeSingleOptionQuestion
    {
        private int? answer;

        public InterviewTreeSingleOptionQuestion(object answer)
        {
            this.answer = answer == null ? (int?)null : Convert.ToInt32(answer);
        }

        public bool IsAnswered => this.answer != null;

        public int GetAnswer() => this.answer.Value;

        public void SetAnswer(int answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeSingleOptionQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeMultiOptionQuestion
    {
        private decimal[] answer;

        public InterviewTreeMultiOptionQuestion(object answer)
        {
            this.answer = answer as decimal[];
        }

        public bool IsAnswered => this.answer != null;
        public decimal[] GetAnswer() => this.answer;
        public void SetAnswer(decimal[] answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeMultiOptionQuestion question)
        {
            if (question?.answer == null && this.answer == null)
                return true;

            if (question?.answer != null && this.answer != null)
                return question.answer.SequenceEqual(this.answer);

            return false;
        }

        internal void SetAnswer(int[] intValues)
        {
            SetAnswer((intValues ?? new int[0]).Select(Convert.ToDecimal).ToArray());
        }
    }

    public class InterviewTreeSingleLinkedOptionQuestion : InterviewTreeLinkedQuestion
    {
        private RosterVector answer;
        public InterviewTreeSingleLinkedOptionQuestion(IEnumerable<RosterVector> linkedOptions, object answer, Guid linkedSourceId, Identity commonParentRosterIdForLinkedQuestion)
            : base(linkedOptions, linkedSourceId, commonParentRosterIdForLinkedQuestion)
        {
            this.answer = answer as RosterVector;
        }

        public bool IsAnswered => this.answer != null;
        public RosterVector GetAnswer() => this.answer;
        public void SetAnswer(RosterVector answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeSingleLinkedOptionQuestion question) => question?.answer == this.answer;
    }

    public class InterviewTreeMultiLinkedOptionQuestion : InterviewTreeLinkedQuestion
    {
        private decimal[][] answer;
        public InterviewTreeMultiLinkedOptionQuestion(IEnumerable<RosterVector> linkedOptions, object answer, Guid linkedSourceId, Identity commonParentRosterIdForLinkedQuestion) 
            : base(linkedOptions, linkedSourceId, commonParentRosterIdForLinkedQuestion)
        {
            if (answer!=null && answer is RosterVector[])
                this.answer = (answer as RosterVector[]).Select(x => x.Coordinates.ToArray()).ToArray();
        }

        public bool IsAnswered => this.answer != null;
        public decimal[][] GetAnswer() => this.answer;
        public void SetAnswer(decimal[][] answer) => this.answer = answer;
        public void RemoveAnswer() => this.answer = null;

        public bool EqualByAnswer(InterviewTreeMultiLinkedOptionQuestion question)
        {
            if (question?.answer == null && this.answer == null)
                return true;

            if (question?.answer != null && this.answer != null)
                return question.answer.SelectMany(x => x).SequenceEqual(this.answer.SelectMany(x => x));

            return false;
        }
    }

    public abstract class InterviewTreeLinkedQuestion
    {
        public Guid LinkedSourceId { get; private set; }
        public Identity CommonParentRosterIdForLinkedQuestion { get; private set; }

        protected InterviewTreeLinkedQuestion(IEnumerable<RosterVector> linkedOptions, Guid linkedSourceId, Identity commonParentRosterIdForLinkedQuestion)
        {
            this.LinkedSourceId = linkedSourceId;
            this.CommonParentRosterIdForLinkedQuestion = commonParentRosterIdForLinkedQuestion;
            //Interview state returns null if linked question has no options
            // if (linkedOptions == null) throw new ArgumentNullException(nameof(linkedOptions));

            this.Options = linkedOptions?.ToList() ?? new List<RosterVector>();
        }

        public List<RosterVector> Options { get; private set; }

        public void SetOptions(IEnumerable<RosterVector> options)
        {
            this.Options = options.ToList();
        }
    }

    public class InterviewTreeCascadingQuestion
    {
        private readonly InterviewTreeQuestion question;
        private readonly Identity cascadingParentQuestionIdentity;

        public InterviewTreeCascadingQuestion(InterviewTreeQuestion question, Identity cascadingParentQuestionIdentity)
        {
            if (cascadingParentQuestionIdentity == null) throw new ArgumentNullException(nameof(cascadingParentQuestionIdentity));

            this.question = question;
            this.cascadingParentQuestionIdentity = cascadingParentQuestionIdentity;
        }

        private InterviewTree Tree => this.question.Tree;

        public InterviewTreeSingleOptionQuestion GetCascadingParentQuestion()
            => this.Tree.GetQuestion(this.cascadingParentQuestionIdentity).AsSingleOption;
    }

    public class InterviewTreeVariable : InterviewTreeLeafNode
    {
        public object Value { get; private set; }
        public bool HasValue => this.Value != null;

        public InterviewTreeVariable(Identity identity, bool isDisabled, object value)
            : base(identity, isDisabled)
        {
            this.SetValue(value);
        }

        public override string ToString() => $"Variable ({this.Identity})";

        public void SetValue(object value) => this.Value = value;
    }

    public class InterviewTreeStaticText : InterviewTreeLeafNode
    {
        public InterviewTreeStaticText(Identity identity, bool disabled)
            : base(identity, disabled) { }

        public bool IsValid => !this.FailedValidations?.Any() ?? true;
        public IReadOnlyList<FailedValidationCondition> FailedValidations { get; private set; }
        public void SetFailedValidations(IReadOnlyList<FailedValidationCondition> failedValidations)
            => this.FailedValidations = failedValidations;

        public override string ToString() => $"Text ({this.Identity})";
    }

    public class InterviewTreeSubSection : InterviewTreeGroup
    {
        public InterviewTreeSubSection(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
            : base(identity, children, isDisabled) { }

        public override string ToString()
            => $"SubSection ({this.Identity})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }

    public class InterviewTreeSection : InterviewTreeGroup
    {
        public InterviewTreeSection(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
            : base(identity, children, isDisabled) { }

        public override string ToString()
            => $"Section ({this.Identity})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }

    public enum RosterType
    {
        Fixed = 1,
        Numeric = 2,
        YesNo = 3,
        Multi = 4,
        List = 5
    }

    public class InterviewTreeRoster : InterviewTreeGroup
    {
        public InterviewTreeRoster(Identity identity,
            IEnumerable<IInterviewTreeNode> children,
            bool isDisabled = false,
            string rosterTitle = null,
            int sortIndex = 0,
            RosterType rosterType = RosterType.Fixed,
            Guid? rosterSizeQuestion = null,
            Identity rosterTitleQuestionIdentity = null)
            : base(identity, children, isDisabled)
        {
            RosterTitle = rosterTitle;
            SortIndex = sortIndex;

            switch (rosterType)
            {
                case RosterType.Fixed:
                    AsFixed = new InterviewTreeFixedRoster();
                    break;
                case RosterType.Numeric:
                    AsNumeric = new InterviewTreeNumericRoster(rosterSizeQuestion.Value, rosterTitleQuestionIdentity);
                    break;
                case RosterType.YesNo:
                    AsYesNo = new InterviewTreeYesNoRoster(rosterSizeQuestion.Value);
                    break;
                case RosterType.Multi:
                    AsMulti = new InterviewTreeMultiRoster(rosterSizeQuestion.Value);
                    break;
                case RosterType.List:
                    AsList = new InterviewTreeListRoster(rosterSizeQuestion.Value);
                    break;
            }
        }

        public string RosterTitle { get; set; }
        public int SortIndex { get; set; } = 0;

        public InterviewTreeNumericRoster AsNumeric { get; }
        public InterviewTreeListRoster AsList { get; }
        public InterviewTreeYesNoRoster AsYesNo { get; }
        public InterviewTreeMultiRoster AsMulti { get; }
        public InterviewTreeFixedRoster AsFixed { get; }

        public bool IsNumeric => this.AsNumeric != null;
        public bool IsList => this.AsList != null;
        public bool IsYesNo => this.AsYesNo != null;
        public bool IsMulti => this.AsMulti != null;
        public bool IsFixed => this.AsFixed != null;

        public override string ToString()
            => $"Roster ({this.Identity}) [{RosterTitle}]" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));

        public void SetRosterTitle(string rosterTitle)
        {
            RosterTitle = rosterTitle;
        }
    }

    public class InterviewTreeFixedRoster
    {
    }

    public class InterviewTreeMultiRoster
    {
        public Guid RosterSizeQuestion { get; set; }

        public InterviewTreeMultiRoster(Guid rosterSizeQuestion)
        {
            this.RosterSizeQuestion = rosterSizeQuestion;
        }
    }

    public class InterviewTreeYesNoRoster
    {
        public Guid RosterSizeQuestion { get; set; }

        public InterviewTreeYesNoRoster(Guid rosterSizeQuestion)
        {
            this.RosterSizeQuestion = rosterSizeQuestion;
        }
    }

    public class InterviewTreeNumericRoster
    {
        public Guid RosterSizeQuestion { get; set; }
        public Identity RosterTitleQuestionIdentity { get; set; }
        public bool HasTitleQuestion => RosterTitleQuestionIdentity != null;

        public InterviewTreeNumericRoster(Guid rosterSizeQuestion, Identity rosterTitleQuestionIdentity)
        {
            this.RosterSizeQuestion = rosterSizeQuestion;
            this.RosterTitleQuestionIdentity = rosterTitleQuestionIdentity;
        }
    }

    public class InterviewTreeListRoster
    {
        public Guid RosterSizeQuestion { get; set; }

        public InterviewTreeListRoster(Guid rosterSizeQuestion)
        {
            this.RosterSizeQuestion = rosterSizeQuestion;
        }
    }
}