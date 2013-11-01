﻿using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.View;
using Main.Core.View.Question;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WB.UI.Designer.Views.Questionnaire
{
    public class GroupView : ICompositeView
    {
        public GroupView(IQuestionnaireDocument doc, IGroup group)
        {
            this.PublicKey = group.PublicKey;
            this.Title = group.Title;
            this.Propagated = group.Propagated;
            this.ConditionExpression = group.ConditionExpression;
            this.Description = group.Description;
            this.Children = this.ConvertChildrenFromGroupDocument(doc, @group);
        }

        public string ConditionExpression { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Propagate Propagated { get; set; }

        public string Description { get; set; }

        public Guid PublicKey { get; set; }

        public string Title { get; set; }

        public List<ICompositeView> Children { get; set; }

        public bool IsGroup = true;

        private List<ICompositeView> ConvertChildrenFromGroupDocument(IQuestionnaireDocument doc, IComposite @group)
        {
            var compositeViews = new List<ICompositeView>();
            foreach (IComposite composite in @group.Children)
            {
                if ((composite as IQuestion) != null)
                {
                    var q = composite as IQuestion;
                    compositeViews.Add(new QuestionView(q));
                }
                else
                {
                    var g = composite as IGroup;
                    compositeViews.Add(new GroupView(doc, g));
                }
            }
            return compositeViews;
        }

        public List<QuestionView> GetAllQuestions()
        {
            var questions = new List<QuestionView>();
            foreach (ICompositeView composite in Children)
            {
                if ((composite as QuestionView) != null)
                {
                    var q = composite as QuestionView;
                    questions.Add(q);
                }
                else
                {
                    var g = composite as GroupView;
                    questions.AddRange(g.GetAllQuestions());
                }
            }
            return questions;
        }
    }
}