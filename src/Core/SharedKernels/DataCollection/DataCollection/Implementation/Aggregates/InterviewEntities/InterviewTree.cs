﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.GenericSubdomains.Portable;

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
                .Single(node => node.Identity == questionIdentity);

        public IReadOnlyCollection<InterviewTreeQuestion> FindQuestions(Guid questionId)
            => this
                .GetNodes<InterviewTreeQuestion>()
                .Where(node => node.Identity.Id == questionId)
                .ToReadOnlyCollection();

        private IEnumerable<TNode> GetNodes<TNode>() => this.GetNodes().OfType<TNode>();

        private IEnumerable<IInterviewTreeNode> GetNodes()
            => this.Sections.Cast<IInterviewTreeNode>().TreeToEnumerable(node => node.Children);

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
    }

    public interface IInternalInterviewTreeNode
    {
        void SetTree(InterviewTree tree);
        void SetParent(IInterviewTreeNode parent);
    }

    public abstract class InterviewTreeLeafNode : IInterviewTreeNode, IInternalInterviewTreeNode
    {
        private readonly bool isDisabled;

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
    }

    public abstract class InterviewTreeGroup : IInterviewTreeNode, IInternalInterviewTreeNode
    {
        private readonly bool isDisabled;

        protected InterviewTreeGroup(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
        {
            this.Identity = identity;
            this.Children = children.ToList();
            this.isDisabled = isDisabled;

            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetParent(this);
            }
        }

        public Identity Identity { get; }
        public InterviewTree Tree { get; private set; }
        public IInterviewTreeNode Parent { get; private set; }
        public IReadOnlyCollection<IInterviewTreeNode> Children { get; }

        void IInternalInterviewTreeNode.SetTree(InterviewTree tree)
        {
            this.Tree = tree;

            foreach (var child in this.Children)
            {
                ((IInternalInterviewTreeNode)child).SetTree(tree);
            }
        }

        void IInternalInterviewTreeNode.SetParent(IInterviewTreeNode parent) => this.Parent = parent;

        public bool IsDisabled() => this.isDisabled || (this.Parent?.IsDisabled() ?? false);
    }

    public class InterviewTreeQuestion : InterviewTreeLeafNode
    {
        public InterviewTreeQuestion(Identity identity, bool isDisabled, string title, string variableName,
            QuestionType questionType, object answer,
            IEnumerable<RosterVector> linkedOptions, Identity cascadingParentQuestionIdentity)
            : base(identity, isDisabled)
        {
            this.Title = title;
            this.VariableName = variableName;
            this.Answer = answer;

            if (questionType == QuestionType.SingleOption)
                this.AsSingleOption = new InterviewTreeSingleOptionQuestion(answer);

            if (linkedOptions != null)
                this.AsLinked = new InterviewTreeLinkedQuestion(linkedOptions);

            if (cascadingParentQuestionIdentity != null)
                this.AsCascading = new InterviewTreeCascadingQuestion(this, cascadingParentQuestionIdentity);
        }

        public string Title { get; }
        public string VariableName { get; }

        public object Answer { get; }

        public InterviewTreeSingleOptionQuestion AsSingleOption { get; }
        public bool IsSingleOption => this.AsSingleOption != null;

        public InterviewTreeLinkedQuestion AsLinked { get; }
        public bool IsLinked => this.AsLinked != null;

        public InterviewTreeCascadingQuestion AsCascading { get; }
        public bool IsCascading => this.AsCascading != null;

        public string FormatForException() => $"'{this.Title} [{this.VariableName}] ({this.Identity})'";

        public override string ToString() => $"Question ({this.Identity}) '{this.Title}'";
    }

    public class InterviewTreeLinkedQuestion
    {
        public InterviewTreeLinkedQuestion(IEnumerable<RosterVector> linkedOptions)
        {
            if (linkedOptions == null) throw new ArgumentNullException(nameof(linkedOptions));

            this.Options = linkedOptions.ToReadOnlyCollection();
        }

        public IReadOnlyCollection<RosterVector> Options { get; }
    }

    public class InterviewTreeSingleOptionQuestion
    {
        private readonly object answer;

        public InterviewTreeSingleOptionQuestion(object answer)
        {
            this.answer = answer;
        }

        public bool IsAnswered => this.answer != null;

        public int GetAnswer() => Convert.ToInt32(this.answer);
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

    public class InterviewTreeStaticText : InterviewTreeLeafNode
    {
        public InterviewTreeStaticText(Identity identity)
            : base(identity, false) {}

        public override string ToString() => $"Text ({this.Identity})";
    }

    public class InterviewTreeSubSection : InterviewTreeGroup
    {
        public InterviewTreeSubSection(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
            : base(identity, children, isDisabled) {}

        public override string ToString()
            => $"SubSection ({this.Identity})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }

    public class InterviewTreeSection : InterviewTreeGroup
    {
        public InterviewTreeSection(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
            : base(identity, children, isDisabled) {}

        public override string ToString()
            => $"Section ({this.Identity})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }

    public class InterviewTreeRoster : InterviewTreeGroup
    {
        public string RosterTitle { get; set; }

        public InterviewTreeRoster(Identity identity, IEnumerable<IInterviewTreeNode> children, bool isDisabled)
            : base(identity, children, isDisabled) {}

        public override string ToString()
            => $"Roster ({this.Identity})" + Environment.NewLine
            + string.Join(Environment.NewLine, this.Children.Select(child => child.ToString().PrefixEachLine("  ")));
    }
}