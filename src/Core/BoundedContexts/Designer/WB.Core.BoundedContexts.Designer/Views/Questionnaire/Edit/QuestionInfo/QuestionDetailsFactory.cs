using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;

namespace WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionInfo
{
    internal class QuestionDetailsFactory : IQuestionDetailsFactory
    {
        public QuestionDetailsView CreateQuestion(IQuestion question, Guid parentGroupId)
        {
            var questionView = CreateQuestionByType(question.QuestionType);
            if (questionView == null)
                return null;
            questionView.Id = question.PublicKey;
            questionView.ParentGroupId = parentGroupId;
            questionView.QuestionScope = question.QuestionScope;
            questionView.Title = question.QuestionText;
            questionView.VariableName = question.StataExportCaption;
            questionView.ConditionExpression = question.ConditionExpression;
            questionView.ValidationExpression = question.ValidationExpression;
            questionView.ValidationMessage = question.ValidationMessage;
            questionView.IsPreFilled = question.Featured;
            questionView.IsMandatory = question.Mandatory;
            questionView.Instructions = question.Instructions;

            var numericQuestion = question as INumericQuestion;
            if (numericQuestion != null)
            {
                var numericQuestionView = ((NumericDetailsView)questionView);
                numericQuestionView.IsInteger = numericQuestion.IsInteger;
                numericQuestionView.CountOfDecimalPlaces = numericQuestion.CountOfDecimalPlaces;
                numericQuestionView.MaxValue = numericQuestion.MaxValue;
                return numericQuestionView;
            }

            var multioptionQuestion = question as IMultyOptionsQuestion;
            if (multioptionQuestion != null)
            {
                var multioptionQuestionView = ((MultiOptionDetailsView)questionView);
                multioptionQuestionView.LinkedToQuestionId = multioptionQuestion.LinkedToQuestionId;
                multioptionQuestionView.AreAnswersOrdered = multioptionQuestion.AreAnswersOrdered;
                multioptionQuestionView.MaxAllowedAnswers = multioptionQuestion.MaxAllowedAnswers;
                multioptionQuestionView.Options = this.CreateCategoricalOptions(multioptionQuestion.Answers);
                return multioptionQuestionView;
            }

            var singleoptionQuestion = question as SingleQuestion;
            if (singleoptionQuestion != null)
            {
                var singleoptionQuestionView = ((SingleOptionDetailsView)questionView);
                singleoptionQuestionView.LinkedToQuestionId = singleoptionQuestion.LinkedToQuestionId;
                singleoptionQuestionView.Options = this.CreateCategoricalOptions(singleoptionQuestion.Answers);
                return singleoptionQuestionView;
            }

            var listQuestion = question as ITextListQuestion;
            if (listQuestion != null)
            {
                var listQuestionView = ((TextListDetailsView)questionView);
                listQuestionView.MaxAnswerCount = listQuestion.MaxAnswerCount;
                return listQuestionView;
            }

            return questionView;
        }

        private CategoricalOption[] CreateCategoricalOptions(List<Answer> answers)
        {
            return EventConverter.GetValidAnswersCollection(answers.ToArray()).Select(x => new CategoricalOption
            {
                Title = x.AnswerText,
                Value = decimal.Parse(x.AnswerValue)
            }).ToArray();
        }

        private static QuestionDetailsView CreateQuestionByType(QuestionType type)
        {
            switch (type)
            {
                case QuestionType.MultyOption:
                    return new MultiOptionDetailsView();

                case QuestionType.SingleOption:
                    return new SingleOptionDetailsView();

                case QuestionType.Text:
                    return new TextDetailsView();

                case QuestionType.DateTime:
                    return new DateTimeDetailsView();

                case QuestionType.Numeric:
                    return new NumericDetailsView();

                case QuestionType.GpsCoordinates:
                    return new GeoLocationDetailsView();

                case QuestionType.TextList:
                    return new TextListDetailsView();

                case QuestionType.QRBarcode:
                    return new QrBarcodeDetailsView();
            }

            return null;
        }
    }
}