using System;
using System.Linq;

namespace WB.Core.SharedKernels.Questionnaire.Categories
{
    public interface ICategoriesService
    {
        void CloneCategories(Guid questionnaireId, Guid categoriesId, Guid clonedQuestionnaireId,
            Guid clonedCategoriesId);

        void Store(Guid questionnaireId, Guid categoriesId, byte[] fileBytes);
        byte[] GetTemplateAsExcelFile();
        IQueryable<CategoriesItem> GetCategoriesById(Guid questionnaireId, Guid id);
        CategoriesFile GetAsExcelFile(Guid questionnaireId, Guid categoriesId);
    }
}
