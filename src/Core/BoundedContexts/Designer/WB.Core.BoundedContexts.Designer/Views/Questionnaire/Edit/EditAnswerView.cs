using System;
using Main.Core.Entities.SubEntities;
using Main.Core.View;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit
{
    public class EditAnswerView : ICompositeView
    {
        public EditAnswerView()
        {
        }

        public EditAnswerView(IAnswer answer)
        {
            this.Id = answer.PublicKey;
            this.Title = answer.AnswerText;
            this.AnswerValue = answer.AnswerValue;
        }


        public string AnswerValue { get; set; }

        
        public Guid Id { get; set; }

        public string Title { get; set; }
    }
}