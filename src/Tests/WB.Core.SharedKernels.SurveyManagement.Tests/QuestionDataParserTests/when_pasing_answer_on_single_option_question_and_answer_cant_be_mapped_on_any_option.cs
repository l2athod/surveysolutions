﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using WB.Core.SharedKernels.SurveyManagement.ValueObjects;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.QuestionDataParserTests
{
    internal class when_pasing_answer_on_single_option_question_and_answer_cant_be_mapped_on_any_option : QuestionDataParserTestContext
    {
        private Establish context = () =>
        {
            answer = "1";
            questionDataParser = CreateQuestionDataParser();
        };

        private Because of =
            () =>
                parsingResult =
                    questionDataParser.TryParse(answer, questionVarName,
                        CreateQuestionnaireDocumentWithOneChapter(new SingleQuestion()
                        {
                            PublicKey = questionId,
                            QuestionType = QuestionType.SingleOption,
                            StataExportCaption = questionVarName
                        }), out parcedValue);

        private It should_result_be_ParsedValueIsNotAllowed = () =>
            parsingResult.ShouldEqual(ValueParsingResult.ParsedValueIsNotAllowed);
        
    }
}
