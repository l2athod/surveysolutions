using System;
using System.Globalization;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Core.BoundedContexts.Interviewer.Implementation.Services
{
    public class AnswerToStringConverter : IAnswerToStringConverter
    {
        public string Convert(object answer, Guid questionId, IQuestionnaire questionnaire)
        {
            Func<decimal, string> getCategoricalOptionText = null;

            var questionType = questionnaire.GetQuestionType(questionId);

            if (answer != null)
            {
                switch (questionType)
                {
                    case QuestionType.DateTime:
                        DateTime dateTimeAnswer = answer is string
                            ? DateTime.Parse((string) answer)
                            : (DateTime) answer;
                        var isTimestamp = questionnaire.IsTimestampQuestion(questionId);
                        answer = isTimestamp
                            ? dateTimeAnswer.ToString(DateTimeFormat.DateWithTimeFormat)
                            : dateTimeAnswer.ToString(DateTimeFormat.DateFormat);
                        break;

                    case QuestionType.MultyOption:
                        if (answer.GetType().IsArray)
                        {
                            answer = (answer as object[])
                                .Select(x => System.Convert.ToDecimal((object) x, CultureInfo.InvariantCulture))
                                .ToArray();
                        }
                        else 
                        {
                            var multiAnswer = ((string) answer).Split(',');
                            answer = multiAnswer
                                .Select(x => System.Convert.ToDecimal(x, CultureInfo.InvariantCulture))
                                .ToArray();
                        }

                        getCategoricalOptionText = (option) => questionnaire.GetAnswerOptionTitle(questionId, option);
                        break;

                    case QuestionType.SingleOption:
                        answer = System.Convert.ToDecimal(answer, CultureInfo.InvariantCulture);
                        getCategoricalOptionText = (option) => questionnaire.GetAnswerOptionTitle(questionId, option);
                        break;

                    case QuestionType.Numeric:
                        decimal answerTyped = answer is string
                            ? decimal.Parse((string) answer, CultureInfo.InvariantCulture)
                            : System.Convert.ToDecimal(answer);
                        answer = questionnaire.ShouldUseFormatting(questionId)
                            ? answerTyped.FormatDecimal()
                            : answerTyped.ToString(CultureInfo.CurrentCulture);
                        break;

                    case QuestionType.GpsCoordinates:
                        var gps = answer is string
                            ? GeoPosition.FromString((string) answer)
                            : (GeoPosition) answer;
                        answer = $"{gps.Latitude}, {gps.Longitude}";
                        break;
                }
            }

            return answer == null ? null : AnswerUtils.AnswerToString(answer, getCategoricalOptionText);
        }
    }
}