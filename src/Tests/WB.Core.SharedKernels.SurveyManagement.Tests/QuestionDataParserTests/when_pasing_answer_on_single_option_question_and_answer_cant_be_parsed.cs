﻿using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.SurveyManagement.ValueObjects;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.QuestionDataParserTests
{
    internal class when_pasing_answer_on_single_option_question_and_answer_cant_be_parsed : QuestionDataParserTestContext
    {
        private Establish context = () =>
        {
            answer = "unparsed";
            questionDataParser = CreateQuestionDataParser();
        };

        private Because of =
            () =>
                parsingResult =
                    questionDataParser.TryParse(answer, new SingleQuestion()
                    {
                        PublicKey = questionId,
                        QuestionType = QuestionType.SingleOption,
                        StataExportCaption = questionVarName
                    }, out parcedValue);

        private It should_result_be_AnswerAsDecimalWasNotParsed = () =>
            parsingResult.ShouldEqual(ValueParsingResult.AnswerAsDecimalWasNotParsed);
    }
}