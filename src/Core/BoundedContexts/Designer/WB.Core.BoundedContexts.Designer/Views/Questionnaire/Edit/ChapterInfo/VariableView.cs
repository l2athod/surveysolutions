﻿using System;
using System.Collections.Generic;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireInfo;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo
{
    public class VariableView : INameable, IQuestionnaireItem
    {
        public VariableView(Guid id, string itemId, VariableData variableData, Breadcrumb[]? breadcrumbs = null,
            QuestionnaireInfoFactory.SelectOption[]? typeOptions = null)
        {
            this.Id = id;
            ItemId = itemId;
            VariableData = variableData;

            Breadcrumbs = breadcrumbs ?? new Breadcrumb[0];
            TypeOptions = typeOptions ?? new QuestionnaireInfoFactory.SelectOption[0];
        }

        public Guid Id { get; set; }

        public string ItemId { get; set; }

        public VariableData VariableData { get; set; }

        public ChapterItemType ItemType => ChapterItemType.Variable;

        public List<IQuestionnaireItem> Items
        {
            get { return new List<IQuestionnaireItem>(); }
            set { }
        }

        public bool HasCondition
        {
            get => false;
            set { }
        }

        public bool HideIfDisabled
        {
            get => false;
            set { }
        }

        public Breadcrumb[] Breadcrumbs { get; set; }
        public QuestionnaireInfoFactory.SelectOption[] TypeOptions { get; set; }

        public string Variable => this.VariableData.Name;
    }
}
