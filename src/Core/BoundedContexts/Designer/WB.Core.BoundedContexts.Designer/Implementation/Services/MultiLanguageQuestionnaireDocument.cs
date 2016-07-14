using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.BoundedContexts.Designer.Implementation.Services
{
    public class MultiLanguageQuestionnaireDocument
    {
        public ReadOnlyQuestionnaireDocument Questionnaire { get; private set; }
        public ReadOnlyCollection<ReadOnlyQuestionnaireDocument> TranslatedQuestionnaires { get; private set; }

        public MultiLanguageQuestionnaireDocument(ReadOnlyQuestionnaireDocument originalQuestionnaireDocument,
            params ReadOnlyQuestionnaireDocument[] translatedQuestionnaireDocuments)
        {
            this.Questionnaire = originalQuestionnaireDocument;
            this.TranslatedQuestionnaires = translatedQuestionnaireDocuments.ToReadOnlyCollection();
        }

        public Dictionary<Guid, Macro> Macros => this.Questionnaire.Macros;
        public Dictionary<Guid, LookupTable> LookupTables => this.Questionnaire.LookupTables;
        public List<Attachment> Attachments => this.Questionnaire.Attachments;
        public List<Translation> Translations => this.Questionnaire.Translations;
        public string Title => this.Questionnaire.Title;
        public Guid PublicKey => this.Questionnaire.PublicKey;

        public T Find<T>(Guid publicKey) where T : class, IComposite
            => this.Questionnaire.Find<T>(publicKey);

        public IEnumerable<T> Find<T>() where T : class
            => this.Questionnaire.Find<T>();

        public IEnumerable<T> Find<T>(Func<T, bool> condition) where T : class
            => this.Questionnaire.Find<T>(condition);

        public IEnumerable<T> FindWithTranslations<T>(Func<T, bool> condition) where T : class
        {
            var allQuestionnaires = this.Questionnaire.ToEnumerable().Union(this.TranslatedQuestionnaires);
            foreach (var questionnaire in allQuestionnaires)
            {
                var findResult = questionnaire.Find<T>(condition);
                foreach (var entity in findResult)
                {
                    yield return entity;
                }
            }
        }

        public T FirstOrDefault<T>(Func<T, bool> condition) where T : class
            => this.Questionnaire.FirstOrDefault(condition);

        public IEnumerable<ReadOnlyQuestionnaireDocument.QuestionnaireItemTypeReference> GetAllEntitiesIdAndTypePairsInQuestionnaireFlowOrder()
            => Questionnaire.GetAllEntitiesIdAndTypePairsInQuestionnaireFlowOrder();
    }
}