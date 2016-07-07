﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.Questionnaire.Documents;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.SharedKernels.Questionnaire.Translator
{
    internal class QuestionnaireTranslator : IQuestionnaireTranslator
    {
        public QuestionnaireDocument Translate(QuestionnaireDocument originalDocument, IQuestionnaireTranslation translation)
        {
            var translatedDocument = originalDocument.Clone();

            TranslateTitles(translatedDocument, translation);

            return translatedDocument;
        }

        private static void TranslateTitles(QuestionnaireDocument questionnaireDocument, IQuestionnaireTranslation translation)
        {
            foreach (var entity in questionnaireDocument.Find<IQuestionnaireEntity>())
            {
                TranslateTitle(entity, translation);
            }

            foreach (var question in questionnaireDocument.Find<IQuestion>())
            {
                TranslateInstruction(question, translation);

                foreach (var answerOption in question.Answers)
                {
                    TranslateAnswerOption(question.PublicKey, answerOption, translation);
                }
            }

            foreach (var validatableEntity in questionnaireDocument.Find<IValidatable>())
            {
                for (int index = 0; index < validatableEntity.ValidationConditions.Count; index++)
                {
                    var validationCondition = validatableEntity.ValidationConditions[index];
                    TranslateValidationMessage(validatableEntity.PublicKey, index + 1, validationCondition, translation);
                }
            }

            foreach (var roster in questionnaireDocument.Find<IGroup>(group => group.IsRoster))
            {
                foreach (var fixedRosterTitle in roster.FixedRosterTitles)
                {
                    TranslateFixedRosterTitle(roster.PublicKey, fixedRosterTitle, translation);
                }
            }
        }

        private static void TranslateTitle(IQuestionnaireEntity entity, IQuestionnaireTranslation translation)
        {
            entity.SetTitle(Translate(
                original: entity.GetTitle(),
                translated: translation.GetTitle(entity.PublicKey)));
        }

        private static void TranslateInstruction(IQuestion question, IQuestionnaireTranslation translation)
        {
            question.Instructions = Translate(
                original: question.Instructions,
                translated: translation.GetInstruction(question.PublicKey));
        }

        private static void TranslateAnswerOption(Guid questionId, Answer answerOption, IQuestionnaireTranslation translation)
        {
            answerOption.AnswerText = Translate(
                original: answerOption.AnswerText,
                translated: translation.GetAnswerOption(questionId, answerOption.AnswerValue));
        }

        private static void TranslateValidationMessage(Guid validatableEntityId, int validationOneBasedIndex,
            ValidationCondition validation, IQuestionnaireTranslation translation)
        {
            validation.Message = Translate(
                original: validation.Message,
                translated: translation.GetValidationMessage(validatableEntityId, validationOneBasedIndex));
        }

        private static void TranslateFixedRosterTitle(Guid rosterId, FixedRosterTitle fixedRosterTitle, IQuestionnaireTranslation translation)
        {
            fixedRosterTitle.Title = Translate(
                original: fixedRosterTitle.Title,
                translated: translation.GetFixedRosterTitle(rosterId, fixedRosterTitle.Value));
        }

        private static string Translate(string original, string translated)
            => string.IsNullOrWhiteSpace(translated) ? translated : original;
    }
}
